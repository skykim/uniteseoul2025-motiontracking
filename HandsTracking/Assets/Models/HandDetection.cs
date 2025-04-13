using System;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.Sentis;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HandDetection : MonoBehaviour
{
    public HandPreview[] handPreviews; //multiple hand previews
    public ImagePreview imagePreview;
    public ModelAsset handDetector;
    public ModelAsset handLandmarker;

    public ModelAsset gestureEmbedder;
    public ModelAsset gestureClassifier;
    public TextAsset anchorsCSV;

    public BackendType backendType = BackendType.CPU;

    public ComputeShader rotateFlipShader;
    private WebCamTexture webcamTexture; 

    public float iouThreshold = 0.3f; //iou threshold for NMSFiltering
    public float scoreThreshold = 0.5f;

    const int k_NumAnchors = 2016;
    float[,] m_Anchors;

    const int k_NumKeypoints = 21;
    const int detectorInputSize = 192;
    const int landmarkerInputSize = 224;

    Worker m_HandDetectorWorker;
    Worker m_HandLandmarkerWorker;
    Worker m_GestureEmbedderWorker;
    Worker m_GestureClassifierWorker;
    Tensor<float> m_DetectorInput;
    Tensor<float> m_LandmarkerInput;
    Awaitable m_DetectAwaitable;

    public GameObject[] effectObjects;

    static string[] gestures_name = {"None", "Closed Fist", "Open Palm", "Pointing Up", "Thumb Down", "Thumb Up", "Victory", "ILoveYou"};

    public TMP_Text gestureText;

    float m_TextureWidth;
    float m_TextureHeight;

    public void Start()
    {
        m_Anchors = BlazeUtils.LoadAnchors(anchorsCSV.text, k_NumAnchors);

        var handDetectorModel = ModelLoader.Load(handDetector);

        // post process the model to filter scores + argmax select the best hand
        var graph = new FunctionalGraph();
        var input = graph.AddInput(handDetectorModel, 0);
        var outputs = Functional.Forward(handDetectorModel, input);
        var boxes = outputs[0]; // (1, 2016, 18)
        var scores = outputs[1]; // (1, 2016, 1)

        //NMSFiltering
        var anchorsData = new float[k_NumAnchors * 4];
        Buffer.BlockCopy(m_Anchors, 0, anchorsData, 0, anchorsData.Length * sizeof(float));
        var anchors = Functional.Constant(new TensorShape(k_NumAnchors, 4), anchorsData);

        var idx_scores_boxes = BlazeUtils.NMSFiltering(boxes, scores, anchors, detectorInputSize, iouThreshold, scoreThreshold);
        handDetectorModel = graph.Compile(idx_scores_boxes.Item1, idx_scores_boxes.Item2, idx_scores_boxes.Item3);

        m_HandDetectorWorker = new Worker(handDetectorModel, backendType);

        var handLandmarkerModel = ModelLoader.Load(handLandmarker);
        m_HandLandmarkerWorker = new Worker(handLandmarkerModel, backendType);

        m_DetectorInput = new Tensor<float>(new TensorShape(1, detectorInputSize, detectorInputSize, 3));
        m_LandmarkerInput = new Tensor<float>(new TensorShape(1, landmarkerInputSize, landmarkerInputSize, 3));

        var gestureEmbedderModel = ModelLoader.Load(gestureEmbedder);
        m_GestureEmbedderWorker = new Worker(gestureEmbedderModel, backendType);

        var gestureClassifierModel = ModelLoader.Load(gestureClassifier);
        var graph2 = new FunctionalGraph();
        var input2 = graph2.AddInput(gestureClassifierModel, 0);
        var outputs2 = Functional.Forward(gestureClassifierModel, input2);
        var maxIndex = Functional.ArgMax(outputs2[0], 1);
        gestureClassifierModel = graph2.Compile(outputs2[0], maxIndex);

        m_GestureClassifierWorker = new Worker(gestureClassifierModel, backendType);

        //Webcam
        WebCamDevice[] devices = WebCamTexture.devices;
        for (int i = 0; i < devices.Length; i++)
        {
            if (devices[i].isFrontFacing)
            {
                webcamTexture = new WebCamTexture(devices[i].name, 360, 640);
                break;
            }
        }
        webcamTexture.Play();
    }

    int effectActiveIndex = 0;
    int previousEffectActiveIndex = 0;

    async void LateUpdate()
    {
        if (isProcessing)
            return;

        Texture rotateImage;

        if (webcamTexture.videoRotationAngle == 270 || webcamTexture.videoRotationAngle == 90)
            rotateImage = RotateFlipTexture(webcamTexture, rotate90:true, flipHorizontal: true, flipVertical: true);
        else
            rotateImage = RotateFlipTexture(webcamTexture, flipHorizontal: true);

        imagePreview.SetTexture(rotateImage);

        m_DetectAwaitable = Detect(rotateImage);
        await m_DetectAwaitable;
    }

    private RenderTexture resultRT;

    public Texture RotateFlipTexture(WebCamTexture webcamTexture, bool rotate90 = false, bool flipHorizontal = false, bool flipVertical = false)
    {
        int kernel = rotateFlipShader.FindKernel("CSMain");

        int width = webcamTexture.width;
        int height = webcamTexture.height;

        int outputWidth = rotate90 ? height : width;
        int outputHeight = rotate90 ? width : height;

        RenderTexture tempRT = RenderTexture.GetTemporary(
            outputWidth,
            outputHeight,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear);

        tempRT.enableRandomWrite = true;
        tempRT.Create();

        rotateFlipShader.SetBool("Rotate90", rotate90);
        rotateFlipShader.SetBool("FlipHorizontal", flipHorizontal);
        rotateFlipShader.SetBool("FlipVertical", flipVertical);

        rotateFlipShader.SetTexture(kernel, "Input", webcamTexture);
        rotateFlipShader.SetTexture(kernel, "Result", tempRT);
        rotateFlipShader.SetInts("InputDimensions", width, height);

        int threadGroupX = Mathf.CeilToInt(width / 16f);
        int threadGroupY = Mathf.CeilToInt(height / 16f);

        rotateFlipShader.Dispatch(kernel, threadGroupX, threadGroupY, 1);

        if (resultRT == null || resultRT.width != outputWidth || resultRT.height != outputHeight)
        {
            if (resultRT != null)
            {
                resultRT.Release();
                Destroy(resultRT);
            }
            resultRT = new RenderTexture(outputWidth, outputHeight, 0, RenderTextureFormat.ARGB32);
            resultRT.enableRandomWrite = true;
            resultRT.Create();
        }

        Graphics.Blit(tempRT, resultRT);
        RenderTexture.ReleaseTemporary(tempRT);

        return resultRT;
    }

    Vector3 ImageToWorld(Vector2 position)
    {
        return (position - 0.5f * new Vector2(m_TextureWidth, m_TextureHeight)) / m_TextureHeight;
    }

    bool isProcessing = false;
    async Awaitable Detect(Texture texture)
    {
        isProcessing = true;

        m_TextureWidth = texture.width;
        m_TextureHeight = texture.height;

        var size = Mathf.Max(texture.width, texture.height);

        // The affine transformation matrix to go from tensor coordinates to image coordinates
        var scale = size / (float)detectorInputSize;
        var M = BlazeUtils.mul(BlazeUtils.TranslationMatrix(0.5f * (new Vector2(texture.width, texture.height) + new Vector2(-size, size))), BlazeUtils.ScaleMatrix(new Vector2(scale, -scale)));
        BlazeUtils.SampleImageAffine(texture, m_DetectorInput, M);

        m_HandDetectorWorker.Schedule(m_DetectorInput);

        var outputIdxAwaitable = (m_HandDetectorWorker.PeekOutput(0) as Tensor<int>).ReadbackAndCloneAsync();
        var outputScoreAwaitable = (m_HandDetectorWorker.PeekOutput(1) as Tensor<float>).ReadbackAndCloneAsync();
        var outputBoxAwaitable = (m_HandDetectorWorker.PeekOutput(2) as Tensor<float>).ReadbackAndCloneAsync();

        using var outputIndices = await outputIdxAwaitable;
        using var outputScores = await outputScoreAwaitable;
        using var outputBoxes = await outputBoxAwaitable;

        int numHands = outputIndices.shape.length;
        Debug.Log("# of hands: " + numHands);

        for (var i = 0; i < handPreviews.Length; i++)
        {
            var active = i < numHands;
            handPreviews[i].SetActive(active);
            if (!active)
                continue;

            var idx = outputIndices[i];

            var anchorPosition = detectorInputSize * new float2(m_Anchors[idx, 0], m_Anchors[idx, 1]);

            var boxCentre_TensorSpace = anchorPosition + new float2(outputBoxes[0, i, 0], outputBoxes[0, i, 1]);
            var boxSize_TensorSpace = math.max(outputBoxes[0, i, 2], outputBoxes[0, i, 3]);

            var kp0_TensorSpace = anchorPosition + new float2(outputBoxes[0, i, 4 + 2 * 0 + 0], outputBoxes[0, i, 4 + 2 * 0 + 1]);
            var kp2_TensorSpace = anchorPosition + new float2(outputBoxes[0, i, 4 + 2 * 2 + 0], outputBoxes[0, i, 4 + 2 * 2 + 1]);
            var delta_TensorSpace = kp2_TensorSpace - kp0_TensorSpace;
            var up_TensorSpace = delta_TensorSpace / math.length(delta_TensorSpace);
            var theta = math.atan2(delta_TensorSpace.y, delta_TensorSpace.x);
            var rotation = 0.5f * Mathf.PI - theta;
            boxCentre_TensorSpace += 0.5f * boxSize_TensorSpace * up_TensorSpace;
            boxSize_TensorSpace *= 2.6f;

            var origin2 = new float2(0.5f * landmarkerInputSize, 0.5f * landmarkerInputSize);
            var scale2 = boxSize_TensorSpace / landmarkerInputSize;
            var M2 = BlazeUtils.mul(M, BlazeUtils.mul(BlazeUtils.mul(BlazeUtils.mul(BlazeUtils.TranslationMatrix(boxCentre_TensorSpace), BlazeUtils.ScaleMatrix(new float2(scale2, -scale2))), BlazeUtils.RotationMatrix(rotation)), BlazeUtils.TranslationMatrix(-origin2)));
            BlazeUtils.SampleImageAffine(texture, m_LandmarkerInput, M2);

            m_HandLandmarkerWorker.Schedule(m_LandmarkerInput);

            using var landmarksTensor = m_HandLandmarkerWorker.PeekOutput("Identity") as Tensor<float>;
            using var probabilityTensor = m_HandLandmarkerWorker.PeekOutput("Identity_1") as Tensor<float>;
            using var handnessTensor = m_HandLandmarkerWorker.PeekOutput("Identity_2") as Tensor<float>;
            using var worldLandmarksTensor = m_HandLandmarkerWorker.PeekOutput("Identity_3") as Tensor<float>;

            var landmarksAwaitable = landmarksTensor.ReadbackAndCloneAsync();
            var probabilityAwaitable = probabilityTensor.ReadbackAndCloneAsync();
            var handnessAwaitable = handnessTensor.ReadbackAndCloneAsync();
            var worldLandmarkAwaitable = worldLandmarksTensor.ReadbackAndCloneAsync();
            
            using var landmarks = await landmarksAwaitable;            
            using var probability = await probabilityAwaitable;
            using var handness = await handnessAwaitable;
            using var worldLandmarks = await worldLandmarkAwaitable;

            //gesture embedder
            landmarksTensor.Reshape(new TensorShape(1, 21, 3));
            worldLandmarksTensor.Reshape(new TensorShape(1, 21, 3));
            
            m_GestureEmbedderWorker.SetInput("hand", landmarksTensor);
            m_GestureEmbedderWorker.SetInput("handedness", handnessTensor);
            m_GestureEmbedderWorker.SetInput("world_hand", worldLandmarksTensor);

            m_GestureEmbedderWorker.Schedule();
            using var gestureEmbedderTensor = m_GestureEmbedderWorker.PeekOutput("Identity") as Tensor<float>;

            m_GestureClassifierWorker.SetInput(0, gestureEmbedderTensor);
            m_GestureClassifierWorker.Schedule();
            var gestureProbabilityAwaitable = (m_GestureClassifierWorker.PeekOutput(0) as Tensor<float>).ReadbackAndCloneAsync();
            var gestureMaxIndexAwaitable = (m_GestureClassifierWorker.PeekOutput(1) as Tensor<int>).ReadbackAndCloneAsync();
            using var gestureClassifierResult = await gestureProbabilityAwaitable;
            using var gestureMaxIndex = await gestureMaxIndexAwaitable;

            string handnessText = handness[0, 0] > 0.5f ? "Right" : "Left";
            gestureText.text = handnessText + "\n" + gestures_name[gestureMaxIndex[0]] + "\n" + gestureClassifierResult[0, gestureMaxIndex[0]];

            for (var j = 0; j < k_NumKeypoints; j++)
            {
                var position_ImageSpace = BlazeUtils.mul(M2, new float2(landmarks[3 * j + 0], landmarks[3 * j + 1]));
                Vector3 position_WorldSpace = ImageToWorld(position_ImageSpace) + new Vector3(0, 0, landmarks[3 * j + 2] / m_TextureHeight);
                handPreviews[i].SetKeypoint(j, true, position_WorldSpace);
            }

            effectActiveIndex = gestureMaxIndex[0];

            if (effectActiveIndex != previousEffectActiveIndex)
            {
                effectObjects[previousEffectActiveIndex].SetActive(false);
                if (effectObjects[previousEffectActiveIndex].GetComponent<ParticleSystem>() != null)
                    effectObjects[previousEffectActiveIndex].GetComponent<ParticleSystem>().Stop();
                effectObjects[effectActiveIndex].SetActive(true);
                if (effectObjects[effectActiveIndex].GetComponent<ParticleSystem>() != null)
                    effectObjects[effectActiveIndex].GetComponent<ParticleSystem>().Play();
            }

            landmarksTensor.Dispose();
            worldLandmarksTensor.Dispose();

            previousEffectActiveIndex = effectActiveIndex;          
        }

        isProcessing = false;
    }

    void OnDestroy()
    {
        m_HandDetectorWorker?.Dispose();
        m_HandLandmarkerWorker?.Dispose();
        m_GestureEmbedderWorker?.Dispose();
        m_GestureClassifierWorker?.Dispose();

        m_DetectorInput?.Dispose();
        m_LandmarkerInput?.Dispose();

        m_DetectAwaitable.Cancel();

        resultRT?.Release();
        Destroy(resultRT);
    }
}
