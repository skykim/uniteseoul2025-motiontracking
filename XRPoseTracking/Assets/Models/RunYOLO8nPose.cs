using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Sentis;
using System.Threading.Tasks;
using PassthroughCameraSamples;

public class RunYOLO8nPose : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private WebCamTextureManager webCamTextureManager;
    [SerializeField] private ObjectRenderer objectRenderer;
    private WebCamTexture _webcamTexture;

    // Drag the yolov8_pose.onnx file here
    public ModelAsset asset;
    private Worker engine;
    private BackendType backend = BackendType.CPU;
    private Texture2D _cpuTexture;

    private const int numJoints = 17;
    private const int maxPeople = 50;    

    //Image size for the model
    private const int imageWidth = 640;
    private const int imageHeight = 640;

    [SerializeField, Range(0, 1)] float iouThreshold = 0.5f;
    [SerializeField, Range(0, 1)] float scoreThreshold = 0.5f;

    Tensor centersToCorners;
    public struct Keypoint
    {
        public float x;
        public float y;
        public float confidence;
    }       
    //bounding box data
    public struct BoundingBox
    {
        public float centerX;
        public float centerY;
        public float width;
        public float height;
        public string label;
    }

    public void Start()
    {
        print("[ObjectDetector] Starting up and acquiring webcam texture.");
        _webcamTexture = webCamTextureManager.WebCamTexture;
        if (_webcamTexture != null)
        {
            _cpuTexture = new Texture2D(_webcamTexture.width, _webcamTexture.height, TextureFormat.RGBA32, false);
            print($"[ObjectDetector] WebCamTexture dimensions: {_webcamTexture.width}x{_webcamTexture.height}");
        }
        else
        {
            Debug.LogError("[ObjectDetector] WebCamTexture is null at Start.");
        }

        LoadModel(backend);
    }
  
   void LoadModel(BackendType backend)
    {
        //Load model
        var model1 = ModelLoader.Load(asset);

        var centersToCornersData = new[]
            {
                        1,      0,      1,      0,
                        0,      1,      0,      1,
                        -0.5f,  0,      0.5f,   0,
                        0,      -0.5f,  0,      0.5f
            };

        var graph = new FunctionalGraph();
        var input = graph.AddInput(model1, 0);
        var modelOutput = Functional.Forward(model1, input)[0];

        var boxCoords = modelOutput[0, 0..4, ..].Transpose(0, 1);
        var scores = modelOutput[0, 4, ..];
        var keypointsData = modelOutput[0, 5.., ..].Transpose(0,1);

        var boxCorners = Functional.MatMul(boxCoords, Functional.Constant(new TensorShape(4, 4), centersToCornersData));

        var indices = Functional.NMS(boxCorners, scores, iouThreshold, scoreThreshold);

        var indicesExpandedBox = indices.Unsqueeze(-1).BroadcastTo(new[] {4});
        var indicesExpandedKpts = indices.Unsqueeze(-1).BroadcastTo(new[] {51});
        
        var finalCoords = Functional.Gather(boxCoords, 0, indicesExpandedBox);
        var finalKeypointsData = Functional.Gather(keypointsData, 0, indicesExpandedKpts);

        var model2 = graph.Compile(finalCoords, finalKeypointsData);

        //Create engine to run model
        engine = new Worker(model2, backend);
    }

    async void Update()
    {
        if (isProcessing)
            return;

        if (!_webcamTexture)
        {
            _webcamTexture = webCamTextureManager.WebCamTexture;
            
            if (_webcamTexture)
            {
                _cpuTexture = new Texture2D(_webcamTexture.width, _webcamTexture.height, TextureFormat.RGBA32, false);
                print("[ObjectDetector] WebCamTexture is now available; CPU texture created.");
            }
        }

        _cpuTexture.SetPixels(_webcamTexture.GetPixels());
        _cpuTexture.Apply();        
        
        await ExecuteModel(_cpuTexture);
    }

    bool isProcessing = false;

    async Task ExecuteModel(Texture inputTexture)
    {
        if (inputTexture == null)
            return;
        if (isProcessing)
            return;

        isProcessing = true;

        var inputTensor = TextureConverter.ToTensor(inputTexture, imageWidth, imageHeight, 3);

        engine.Schedule(inputTensor);

        using var output_ = engine.PeekOutput(0) as Tensor<float>;
        using var ketPoints_ = engine.PeekOutput(1) as Tensor<float>;

        using var outputTensor = await output_.ReadbackAndCloneAsync();
        using var keyPointsTensor = await ketPoints_.ReadbackAndCloneAsync();

        objectRenderer.RenderPoseDetections(outputTensor, keyPointsTensor);

        isProcessing = false;
    }

    private void OnDestroy()
    {
        centersToCorners?.Dispose();
        engine?.Dispose();

        if (_cpuTexture != null)
        {
            Destroy(_cpuTexture);
            _cpuTexture = null;
        }        
    }
}