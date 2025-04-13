using System;
using TMPro;
using Unity.LiveCapture;
using Unity.Mathematics;
using Unity.Sentis;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace FacialController
{
public class FaceDetection : MonoBehaviour
{
    private WebCamTexture webcamTexture; 
    public ModelAsset faceDetector;
    public ModelAsset faceMesh;
    public ModelAsset faceBlendshape;
    public TextAsset anchorsCSV;
    public RawImage display;
    public ComputeShader rotateFlipShader;

    public BackendType backendType = BackendType.CPU;

    public ComputeShader cropResizeShader;
    public float iouThreshold = 0.3f;
    public float scoreThreshold = 0.5f;

    const int k_NumAnchors = 896;
    float[,] m_Anchors;
    const int detectorInputSize = 128;
    const int meshInputSize = 256;

    Worker m_FaceDetectorWorker;
    Worker m_FaceMeshWorker;
    Worker m_FaceBlendshapeWorker;
    Tensor<float> m_DetectorInput;
    Awaitable m_DetectAwaitable;

    int[] LandmarkIndices = new[]
    {
            0, 1, 4, 5, 6, 7, 8, 10, 13, 14, 17, 21, 33, 37, 39, 40, 46, 52, 53, 54, 55,
            58, 61, 63, 65, 66, 67, 70, 78, 80, 81, 82, 84, 87, 88, 91, 93, 95, 103, 105,
            107, 109, 127, 132, 133, 136, 144, 145, 146, 148, 149, 150, 152, 153, 154,
            155, 157, 158, 159, 160, 161, 162, 163, 168, 172, 173, 176, 178, 181, 185,
            191, 195, 197, 234, 246, 249, 251, 263, 267, 269, 270, 276, 282, 283, 284,
            285, 288, 291, 293, 295, 296, 297, 300, 308, 310, 311, 312, 314, 317, 318,
            321, 323, 324, 332, 334, 336, 338, 356, 361, 362, 365, 373, 374, 375, 377,
            378, 379, 380, 381, 382, 384, 385, 386, 387, 388, 389, 390, 397, 398, 400,
            402, 405, 409, 415, 454, 466, 468, 469, 470, 471, 472, 473, 474, 475, 476, 477
    };    

    [SerializeField] public SkinnedMeshRenderer headMesh;    

    public void Start()
    {
        m_Anchors = BlazeUtils.LoadAnchors(anchorsCSV.text, k_NumAnchors);

        //Face Detector
        var faceDetectorModel = ModelLoader.Load(faceDetector);

        // post process the model to filter scores + nms select the best faces
        var graph = new FunctionalGraph();
        var input = graph.AddInput(faceDetectorModel, 0);
        var outputs = Functional.Forward(faceDetectorModel, 2 * input - 1);
        var boxes = outputs[0]; // (1, 896, 16)
        var scores = outputs[1]; // (1, 896, 1)
        var anchorsData = new float[k_NumAnchors * 4];
        Buffer.BlockCopy(m_Anchors, 0, anchorsData, 0, anchorsData.Length * sizeof(float));
        var anchors = Functional.Constant(new TensorShape(k_NumAnchors, 4), anchorsData);
        var idx_scores_boxes = BlazeUtils.NMSFiltering(boxes, scores, anchors, detectorInputSize, iouThreshold, scoreThreshold);
        faceDetectorModel = graph.Compile(idx_scores_boxes.Item1, idx_scores_boxes.Item2, idx_scores_boxes.Item3);

        m_FaceDetectorWorker = new Worker(faceDetectorModel, backendType);

        //Face Mesh
        var faceMeshModel = ModelLoader.Load(faceMesh);

        var graph2 = new FunctionalGraph();
        var input2 = graph2.AddInput(faceMeshModel, 0);
        var modelOutput2 = Functional.Forward(faceMeshModel, input2)[0];
        var reshapeTensor = modelOutput2.Reshape(new [] {478, 3});
        var twoDTensor = reshapeTensor[.., 0..2];
        var indiceLandmark = Functional.Constant(new TensorShape(146), LandmarkIndices);
        var indicesLandmarkExpand = indiceLandmark.Unsqueeze(-1).BroadcastTo(new[] {2});
        var selectedLandmarks = Functional.Gather(twoDTensor, 0, indicesLandmarkExpand);
        var reshapeTensor2 = selectedLandmarks.Reshape(new [] {1, 146, 2});
        var faceMeshModel2 = graph2.Compile(reshapeTensor2);

        m_FaceMeshWorker = new Worker(faceMeshModel2, backendType);

        //Face Blendshape
        var faceBlendshapeModel = ModelLoader.Load(faceBlendshape);
        m_FaceBlendshapeWorker = new Worker(faceBlendshapeModel, backendType);        

        m_DetectorInput = new Tensor<float>(new TensorShape(1, detectorInputSize, detectorInputSize, 3));

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

    bool isProcessing = false;

    async void Update()
    {
        Texture rotateImage;

        if (webcamTexture.videoRotationAngle == 270 || webcamTexture.videoRotationAngle == 90)
            rotateImage = RotateFlipTexture(webcamTexture, rotate90:true, flipHorizontal: true);
        else
            rotateImage = RotateFlipTexture(webcamTexture, rotate90:false, flipHorizontal: true);
        
        display.texture = rotateImage;

        if (isProcessing)
            return;

        m_DetectAwaitable = Detect(rotateImage);
        await m_DetectAwaitable;
    }

    private RenderTexture cachedResultRT;

    public Texture RotateFlipTexture(WebCamTexture webcamTexture, bool rotate90 = false, bool flipHorizontal = false)
    {
        if (webcamTexture == null || webcamTexture.width == 0 || webcamTexture.height == 0)
        {
            Debug.LogWarning("WebCamTexture is not ready.");
            return cachedResultRT;
        }

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
        rotateFlipShader.SetTexture(kernel, "Input", webcamTexture);
        rotateFlipShader.SetTexture(kernel, "Result", tempRT);
        rotateFlipShader.SetInts("InputDimensions", width, height);

        int threadGroupX = Mathf.CeilToInt(width / 16f);
        int threadGroupY = Mathf.CeilToInt(height / 16f);

        rotateFlipShader.Dispatch(kernel, threadGroupX, threadGroupY, 1);

        if (cachedResultRT == null || cachedResultRT.width != outputWidth || cachedResultRT.height != outputHeight)
        {
            if (cachedResultRT != null)
            {
                cachedResultRT.Release();
                Destroy(cachedResultRT);
            }

            cachedResultRT = new RenderTexture(outputWidth, outputHeight, 0, tempRT.format);
            cachedResultRT.enableRandomWrite = true;
            cachedResultRT.Create();
        }

        Graphics.Blit(tempRT, cachedResultRT);
        RenderTexture.ReleaseTemporary(tempRT);

        return cachedResultRT;
    }

    
    async Awaitable Detect(Texture texture)
    {
        isProcessing = true;

        var size = Mathf.Max(texture.width, texture.height);

        var scale = size / (float)detectorInputSize;
        var M = BlazeUtils.mul(BlazeUtils.TranslationMatrix(0.5f * (new Vector2(texture.width, texture.height) + new Vector2(-size, size))), BlazeUtils.ScaleMatrix(new Vector2(scale, -scale)));
        BlazeUtils.SampleImageAffine(texture, m_DetectorInput, M);

        Profiler.BeginSample("Sentis.FaceDetectorWorker");

        m_FaceDetectorWorker.Schedule(m_DetectorInput);

        var outputIndicesAwaitable = (m_FaceDetectorWorker.PeekOutput(0) as Tensor<int>).ReadbackAndCloneAsync();
        var outputScoresAwaitable = (m_FaceDetectorWorker.PeekOutput(1) as Tensor<float>).ReadbackAndCloneAsync();
        var outputBoxesAwaitable = (m_FaceDetectorWorker.PeekOutput(2) as Tensor<float>).ReadbackAndCloneAsync();

        using var outputIndices = await outputIndicesAwaitable;
        using var outputScores = await outputScoresAwaitable;
        using var outputBoxes = await outputBoxesAwaitable;

        Profiler.EndSample();

        

        var numFaces = outputIndices.shape.length;
        //Debug.Log("# of faces: " + numFaces);

        for (var i = 0; i < numFaces; i++)
        {
            var idx = outputIndices[i];

            var anchorPosition = detectorInputSize * new float2(m_Anchors[idx, 0], m_Anchors[idx, 1]);

            var box_ImageSpace = BlazeUtils.mul(M, anchorPosition + new float2(outputBoxes[0, i, 0], outputBoxes[0, i, 1]));
            var boxTopRight_ImageSpace = BlazeUtils.mul(M, anchorPosition + new float2(outputBoxes[0, i, 0] + 0.5f * outputBoxes[0, i, 2], outputBoxes[0, i, 1] + 0.5f * outputBoxes[0, i, 3]));

            var boxSize = 2f * (boxTopRight_ImageSpace - box_ImageSpace);

            Vector2 faceCenter = box_ImageSpace;
            float maxLength = 1.8f * Math.Max(Math.Abs(boxSize.x), Math.Abs(boxSize.y));
            
            RenderTexture cropTexture = CropResize(texture, box_ImageSpace, new Vector2(maxLength, maxLength), meshInputSize ,meshInputSize);
            TextureTransform faceBoxTranform = new TextureTransform().SetDimensions(meshInputSize, meshInputSize, 3).SetTensorLayout(TensorLayout.NHWC);
            using var faceBoxTensor = new Tensor<float>(new TensorShape(1, meshInputSize, meshInputSize, 3));
            TextureConverter.ToTensor(cropTexture, faceBoxTensor, faceBoxTranform);
            RenderTexture.ReleaseTemporary(cropTexture);

            //inference faceMesh
            Profiler.BeginSample("Sentis.FaceMesh");
            m_FaceMeshWorker.Schedule(faceBoxTensor);
            var outputMeshesAwaitable = (m_FaceMeshWorker.PeekOutput(0) as Tensor<float>).ReadbackAndCloneAsync();
            using var outputMeshes = await outputMeshesAwaitable;
            Profiler.EndSample();

            //inference blendshape
            Profiler.BeginSample("Sentis.FaceBlendshape");
            m_FaceBlendshapeWorker.Schedule(outputMeshes);
            var outputBlenshapesAwaitable = (m_FaceBlendshapeWorker.PeekOutput(0) as Tensor<float>).ReadbackAndCloneAsync();
            using var outputBlendshapes = await outputBlenshapesAwaitable;
            Profiler.EndSample();

            string[] blendshapeNames = new string[]
            {
                "_neutral", "browDownLeft", "browDownRight", "browInnerUp", "browOuterUpLeft", "browOuterUpRight",
                "cheekPuff", "cheekSquintLeft", "cheekSquintRight", "eyeBlinkLeft", "eyeBlinkRight",
                "eyeLookDownLeft", "eyeLookDownRight", "eyeLookInLeft", "eyeLookInRight", "eyeLookOutLeft",
                "eyeLookOutRight", "eyeLookUpLeft", "eyeLookUpRight", "eyeSquintLeft", "eyeSquintRight",
                "eyeWideLeft", "eyeWideRight", "jawForward", "jawLeft", "jawOpen", "jawRight", "mouthClose",
                "mouthDimpleLeft", "mouthDimpleRight", "mouthFrownLeft", "mouthFrownRight", "mouthFunnel", "mouthLeft",
                "mouthLowerDownLeft", "mouthLowerDownRight", "mouthPressLeft", "mouthPressRight", "mouthPucker","mouthRight",
                "mouthRollLower", "mouthRollUpper", "mouthShrugLower", "mouthShrugUpper", "mouthSmileLeft", "mouthSmileRight",
                "mouthStretchLeft", "mouthStretchRight", "mouthUpperUpLeft", "mouthUpperUpRight", "noseSneerLeft", "noseSneerRight"
            };

            for (int j = 0; j < blendshapeNames.Length; j++)
            {
                UpdateBlendShape(blendshapeNames[j], outputBlendshapes[j] * 100f);
            }            
        }

        isProcessing = false;
    }
    void UpdateBlendShape(string blendshapeName, float value)
    {
        Mesh mesh = headMesh.sharedMesh;

        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            string meshBlendShapeName = mesh.GetBlendShapeName(i);
            float boundedValue = Math.Min(100, Math.Max(0, value));
            
            if (meshBlendShapeName.Contains(blendshapeName))
            {                
                headMesh.SetBlendShapeWeight(i, boundedValue);
                break;
            }
        }
    }

    public RenderTexture CropResize(Texture inputTexture, Vector2 boxCenter, Vector2 boxSize, int outputWidth, int outputHeight)
    {
        RenderTexture output = RenderTexture.GetTemporary(outputWidth, outputHeight, 0, RenderTextureFormat.ARGB32);
        output.enableRandomWrite = true;
        output.Create();

        int kernel = cropResizeShader.FindKernel("CropResize");

        cropResizeShader.SetTexture(kernel, "_InputTex", inputTexture);
        cropResizeShader.SetTexture(kernel, "_Result", output);

        cropResizeShader.SetVector("_InputSize", new Vector2(inputTexture.width, inputTexture.height));
        cropResizeShader.SetVector("_OutputSize", new Vector2(outputWidth, outputHeight));
        cropResizeShader.SetVector("_BoxCenter", boxCenter);
        cropResizeShader.SetVector("_BoxSize", boxSize);

        int threadX = Mathf.CeilToInt(outputWidth / 8f);
        int threadY = Mathf.CeilToInt(outputHeight / 8f);

        cropResizeShader.Dispatch(kernel, threadX, threadY, 1);

        return output;
    }

    void OnDestroy()
    {
        webcamTexture.Stop();

        m_FaceDetectorWorker.Dispose();
        m_FaceMeshWorker.Dispose();
        m_FaceBlendshapeWorker.Dispose();

        m_DetectorInput.Dispose();
        m_DetectAwaitable.Cancel();

        if (cachedResultRT != null)
        {
            cachedResultRT.Release();
            Destroy(cachedResultRT);
        }        
    }
}
}