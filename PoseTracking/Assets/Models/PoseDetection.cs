using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.Sentis;
using UnityEngine;

public class PoseDetection : MonoBehaviour
{
    public ModelAsset poseDetector;
    public ModelAsset poseLandmarker;
    public TextAsset anchorsCSV;

    public float scoreThreshold = 0.75f;

    const int k_NumAnchors = 2254;
    float[,] m_Anchors;

    const int k_NumKeypoints = 33;
    const int detectorInputSize = 224;
    const int landmarkerInputSize = 256;

    Worker m_PoseDetectorWorker;
    Worker m_PoseLandmarkerWorker;
    Tensor<float> m_DetectorInput;
    Tensor<float> m_LandmarkerInput;
    Awaitable m_DetectAwaitable;

    public BackendType backendType = BackendType.CPU;    

    public void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        m_Anchors = BlazeUtils.LoadAnchors(anchorsCSV.text, k_NumAnchors);

        var poseDetectorModel = ModelLoader.Load(poseDetector);
        // post process the model to filter scores + argmax select the best pose
        var graph = new FunctionalGraph();
        var input = graph.AddInput(poseDetectorModel, 0);
        var outputs = Functional.Forward(poseDetectorModel, input);
        var boxes = outputs[0]; // (1, 2254, 12)
        var scores = outputs[1]; // (1, 2254, 1)
        var idx_scores_boxes = BlazeUtils.ArgMaxFiltering(boxes, scores);
        poseDetectorModel = graph.Compile(idx_scores_boxes.Item1, idx_scores_boxes.Item2, idx_scores_boxes.Item3);

        m_PoseDetectorWorker = new Worker(poseDetectorModel, backendType);

        var poseLandmarkerModel = ModelLoader.Load(poseLandmarker);
        m_PoseLandmarkerWorker = new Worker(poseLandmarkerModel, backendType);

        m_DetectorInput = new Tensor<float>(new TensorShape(1, detectorInputSize, detectorInputSize, 3));
        m_LandmarkerInput = new Tensor<float>(new TensorShape(1, landmarkerInputSize, landmarkerInputSize, 3));
    }

    bool isProcessing = false;

    public async Task<List<Vector3>> Detect(Texture texture)
    {
        if (isProcessing)
            return null;

        isProcessing = true;

        var size = Mathf.Max(texture.width, texture.height);

        // The affine transformation matrix to go from tensor coordinates to image coordinates
        var scale = size / (float)detectorInputSize;
        var M = BlazeUtils.mul(BlazeUtils.TranslationMatrix(0.5f * (new Vector2(texture.width, texture.height) + new Vector2(-size, size))), BlazeUtils.ScaleMatrix(new Vector2(scale, -scale)));
        BlazeUtils.SampleImageAffine(texture, m_DetectorInput, M);

        m_PoseDetectorWorker.Schedule(m_DetectorInput);

        var outputIdxAwaitable = (m_PoseDetectorWorker.PeekOutput(0) as Tensor<int>).ReadbackAndCloneAsync();
        var outputScoreAwaitable = (m_PoseDetectorWorker.PeekOutput(1) as Tensor<float>).ReadbackAndCloneAsync();
        var outputBoxAwaitable = (m_PoseDetectorWorker.PeekOutput(2) as Tensor<float>).ReadbackAndCloneAsync();

        using var outputIdx = await outputIdxAwaitable;
        using var outputScore = await outputScoreAwaitable;
        using var outputBox = await outputBoxAwaitable;

        var scorePassesThreshold = outputScore[0] >= scoreThreshold;

        if (!scorePassesThreshold)
        {
            isProcessing = false;            
            return null;
        }

        var idx = outputIdx[0];

        var anchorPosition = detectorInputSize * new float2(m_Anchors[idx, 0], m_Anchors[idx, 1]);

        var face_ImageSpace = BlazeUtils.mul(M, anchorPosition + new float2(outputBox[0, 0, 0], outputBox[0, 0, 1]));
        var faceTopRight_ImageSpace = BlazeUtils.mul(M, anchorPosition + new float2(outputBox[0, 0, 0] + 0.5f * outputBox[0, 0, 2], outputBox[0, 0, 1] + 0.5f * outputBox[0, 0, 3]));

        var kp1_ImageSpace = BlazeUtils.mul(M, anchorPosition + new float2(outputBox[0, 0, 4 + 2 * 0 + 0], outputBox[0, 0, 4 + 2 * 0 + 1]));
        var kp2_ImageSpace = BlazeUtils.mul(M, anchorPosition + new float2(outputBox[0, 0, 4 + 2 * 1 + 0], outputBox[0, 0, 4 + 2 * 1 + 1]));
        var delta_ImageSpace = kp2_ImageSpace - kp1_ImageSpace;
        var dscale = 1.25f;
        var radius = dscale * math.length(delta_ImageSpace);
        var theta = math.atan2(delta_ImageSpace.y, delta_ImageSpace.x);
        var origin2 = new float2(0.5f * landmarkerInputSize, 0.5f * landmarkerInputSize);
        var scale2 = radius / (0.5f * landmarkerInputSize);
        var M2 = BlazeUtils.mul(BlazeUtils.mul(BlazeUtils.mul(BlazeUtils.TranslationMatrix(kp1_ImageSpace), BlazeUtils.ScaleMatrix(new float2(scale2, -scale2))), BlazeUtils.RotationMatrix(0.5f * Mathf.PI - theta)), BlazeUtils.TranslationMatrix(-origin2));
        BlazeUtils.SampleImageAffine(texture, m_LandmarkerInput, M2);

        m_PoseLandmarkerWorker.Schedule(m_LandmarkerInput);

        var landmarksAwaitable = (m_PoseLandmarkerWorker.PeekOutput("Identity") as Tensor<float>).ReadbackAndCloneAsync();
        using var landmarks = await landmarksAwaitable; // (1,195)

        Landmarks.Clear();

        for (var i = 0; i < k_NumKeypoints; i++)
        {
            var visibility = landmarks[5 * i + 3];
            var presence = landmarks[5 * i + 4];

            Vector3 pos_3d = new Vector3(landmarks[5 * i + 0], 1.0f - landmarks[5 * i + 1], landmarks[5 * i + 2]* 0.2f); 

            Landmarks.Add(pos_3d);
        }

        isProcessing = false;

        return Landmarks;
    }
    List<Vector3> Landmarks = new List<Vector3>();

    void OnDestroy()
    {
        m_PoseDetectorWorker.Dispose();
        m_PoseLandmarkerWorker.Dispose();
        m_DetectorInput.Dispose();
        m_LandmarkerInput.Dispose();

        m_DetectAwaitable.Cancel();
    }  
}
