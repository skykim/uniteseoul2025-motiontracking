using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Video;
using UnityEngine.UI;

public class VNectBlazePoseRunner : MonoBehaviour
{
    [Header("VNect Model")]
    public VNectModel[] VNectModels;

    private WebCamTexture webcamTexture;
    private VideoPlayer videoPlayer;

    public RawImage displayImage;
    public PoseDetection poseDetection;

    [Header("Kalman Filter Settings")]
    public bool UseKalmanFilter = true;
    public float KalmanParamQ = 0.001f;
    public float KalmanParamR = 0.01f;

    void Start()
    {
        //set frameRate
        Application.targetFrameRate = 60;
                
        foreach(var model in VNectModels)
        {
            model.Init();
        }

        //webcamTexture = new WebCamTexture(640, 640);
        //webcamTexture.Play();
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.Play();
    }

    async void Update()
    {
        //Texture inputTexture = webcamTexture;
        Texture inputTexture = videoPlayer.texture;
        displayImage.texture = inputTexture;
        if (inputTexture == null) return;

        List<Vector3> landmarks = await poseDetection.Detect(inputTexture);
        if (landmarks != null)
            UpdateJoints(landmarks);
    }

    public void UpdateJoints(List<Vector3> landmarks)
    {
        if (landmarks.Count != 33) return;

        //jointPointsList
        foreach(var model in VNectModels)
        {
            UpdateJointPointsFromBlazePose(model.JointPoints, landmarks);
            PredictPose(model.JointPoints);
            model.PoseUpdate();
        }
    }

    private void UpdateJointPointsFromBlazePose(VNectModel.JointPoint[] jointPoints, List<Vector3> blazeLandmarks)
    {
        jointPoints[PositionIndex.Nose.Int()].Now3D = blazeLandmarks[0];
        jointPoints[PositionIndex.lShldrBend.Int()].Now3D = blazeLandmarks[11];
        jointPoints[PositionIndex.rShldrBend.Int()].Now3D = blazeLandmarks[12];
        jointPoints[PositionIndex.lForearmBend.Int()].Now3D = blazeLandmarks[13];
        jointPoints[PositionIndex.rForearmBend.Int()].Now3D = blazeLandmarks[14];
        jointPoints[PositionIndex.lHand.Int()].Now3D = blazeLandmarks[15];
        jointPoints[PositionIndex.rHand.Int()].Now3D = blazeLandmarks[16];
        jointPoints[PositionIndex.lThighBend.Int()].Now3D = blazeLandmarks[23];
        jointPoints[PositionIndex.rThighBend.Int()].Now3D = blazeLandmarks[24];
        jointPoints[PositionIndex.lShin.Int()].Now3D = blazeLandmarks[25];
        jointPoints[PositionIndex.rShin.Int()].Now3D = blazeLandmarks[26];
        jointPoints[PositionIndex.lFoot.Int()].Now3D = blazeLandmarks[27];
        jointPoints[PositionIndex.rFoot.Int()].Now3D = blazeLandmarks[28];

        jointPoints[PositionIndex.lThumb2.Int()].Now3D = blazeLandmarks[21];
        jointPoints[PositionIndex.lMid1.Int()].Now3D = blazeLandmarks[19];
        jointPoints[PositionIndex.rThumb2.Int()].Now3D = blazeLandmarks[22];
        jointPoints[PositionIndex.rMid1.Int()].Now3D = blazeLandmarks[20];
        jointPoints[PositionIndex.lToe.Int()].Now3D = blazeLandmarks[31];
        jointPoints[PositionIndex.rToe.Int()].Now3D = blazeLandmarks[32];
        jointPoints[PositionIndex.lEar.Int()].Now3D = blazeLandmarks[7];
        jointPoints[PositionIndex.rEar.Int()].Now3D = blazeLandmarks[8];

        // Derived joint calculations
        Vector3 leftShoulder  = jointPoints[PositionIndex.lShldrBend.Int()].Now3D;
        Vector3 rightShoulder = jointPoints[PositionIndex.rShldrBend.Int()].Now3D;
        Vector3 neckPos       = (leftShoulder + rightShoulder) / 2f;
        jointPoints[PositionIndex.neck.Int()].Now3D = neckPos;

        Vector3 leftThigh  = jointPoints[PositionIndex.lThighBend.Int()].Now3D;
        Vector3 rightThigh = jointPoints[PositionIndex.rThighBend.Int()].Now3D;
        Vector3 hipCandidate = (leftThigh + rightThigh) / 2f;

        Vector3 leftEar  = jointPoints[PositionIndex.lEar.Int()].Now3D;
        Vector3 rightEar = jointPoints[PositionIndex.rEar.Int()].Now3D;
        Vector3 cEar     = (leftEar + rightEar) / 2f;
        Vector3 nose = jointPoints[PositionIndex.Nose.Int()].Now3D;
        jointPoints[PositionIndex.head.Int()].Now3D = (cEar + nose) / 2f;

        jointPoints[PositionIndex.spine.Int()].Now3D = Vector3.Lerp(neckPos, hipCandidate, 0.8f);

        Vector3 spine = jointPoints[PositionIndex.spine.Int()].Now3D;
        jointPoints[PositionIndex.abdomenUpper.Int()].Now3D = (neckPos + spine) / 2f;
        jointPoints[PositionIndex.hip.Int()].Now3D = Vector3.Lerp(spine, hipCandidate, 0.8f);        
    }
    private void PredictPose(VNectModel.JointPoint[] jointPoints)
    {
        if (UseKalmanFilter)
        {
            foreach (var jp in jointPoints)
                KalmanUpdate(jp);
        }
    }
    private void KalmanUpdate(VNectModel.JointPoint measurement)
    {
        measurement.K.x = (measurement.P.x + KalmanParamQ) / (measurement.P.x + KalmanParamQ + KalmanParamR);
        measurement.K.y = (measurement.P.y + KalmanParamQ) / (measurement.P.y + KalmanParamQ + KalmanParamR);
        measurement.K.z = (measurement.P.z + KalmanParamQ) / (measurement.P.z + KalmanParamQ + KalmanParamR);

        measurement.Pos3D.x = measurement.X.x + (measurement.Now3D.x - measurement.X.x) * measurement.K.x;
        measurement.Pos3D.y = measurement.X.y + (measurement.Now3D.y - measurement.X.y) * measurement.K.y;
        measurement.Pos3D.z = measurement.X.z + (measurement.Now3D.z - measurement.X.z) * measurement.K.z;

        measurement.P.x = KalmanParamR * (measurement.P.x + KalmanParamQ) / (KalmanParamR + measurement.P.x + KalmanParamQ);
        measurement.P.y = KalmanParamR * (measurement.P.y + KalmanParamQ) / (KalmanParamR + measurement.P.y + KalmanParamQ);
        measurement.P.z = KalmanParamR * (measurement.P.z + KalmanParamQ) / (KalmanParamR + measurement.P.z + KalmanParamQ);

        measurement.X = measurement.Pos3D;
    }

    void OnDestroy()
    {
        if (webcamTexture != null)
        {
            webcamTexture.Stop();
            webcamTexture = null;
        }
    }  
}