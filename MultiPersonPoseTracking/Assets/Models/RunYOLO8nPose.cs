using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Sentis;
using System.Threading.Tasks;
using TMPro;

public class RunYOLO8nPose : MonoBehaviour
{
    public ModelAsset asset;
    private RawImage displayImage;
    private Sprite borderSprite;
    private Sprite keypointSprite;
    public Texture2D keypointTexture;
    public Texture2D borderTexture;
    public Font font;

    private Transform displayLocation;
    private Worker engine;
    private RenderTexture targetRT;

    private const int imageWidth = 640;
    private const int imageHeight = 640;
    private const int numJoints = 17;
    private const int maxPeople = 50;
    private const int maxLines = 20;

    List<GameObject> boxPool = new();
    List<List<GameObject>> keypointPool = new();
    List<GameObject> lineHolderPool = new();
    List<List<GameObject>> lineRendererPool = new();

    [SerializeField, Range(0, 1)] float iouThreshold = 0.5f;
    [SerializeField, Range(0, 1)] float scoreThreshold = 0.5f;
    [SerializeField, Range(0, 1)] float jointThreshold = 0.5f;

    Tensor centersToCorners;

    public struct Keypoint { public float x, y, confidence; }
    public struct BoundingPoseBox { public float centerX, centerY, width, height; public Keypoint[] keypoints; }

    public void Initialize(BackendType backend, RawImage rawImage)
    {
        displayImage = rawImage;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        LoadModel(backend);
        targetRT = new RenderTexture(imageWidth, imageHeight, 0);
        displayLocation = displayImage.transform;

        if (borderSprite == null)
            borderSprite = Sprite.Create(borderTexture, new Rect(0, 0, borderTexture.width, borderTexture.height), new Vector2(0.5f, 0.5f));
        if (keypointSprite == null)
            keypointSprite = Sprite.Create(keypointTexture, new Rect(0, 0, keypointTexture.width, keypointTexture.height), new Vector2(0.5f, 0.5f));

        InitializePools();
    }

    void InitializePools()
    {
        for (int i = 0; i < maxPeople; i++)
        {
            List<GameObject> kpList = new();
            for (int j = 0; j < numJoints; j++)
            {
                var kpObj = new GameObject($"point_{i}_{j}");
                kpObj.transform.SetParent(displayLocation, false);
                var rt = kpObj.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(10, 10);
                var img = kpObj.AddComponent<Image>();
                img.sprite = keypointSprite;
                img.color = Color.green;
                kpObj.SetActive(false);
                kpList.Add(kpObj);
            }
            keypointPool.Add(kpList);

            var lineHolder = new GameObject($"lines_{i}");
            lineHolder.transform.SetParent(displayLocation, false);
            lineHolderPool.Add(lineHolder);

            List<GameObject> lines = new();
            for (int j = 0; j < maxLines; j++)
            {
                var lineObj = new GameObject($"line_{i}_{j}");
                lineObj.transform.SetParent(lineHolder.transform, false);
                var lr = lineObj.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.widthMultiplier = 5f;
                lr.sortingOrder = 5;
                lr.startColor = lr.endColor = Color.cyan;
                lr.useWorldSpace = false;
                lineObj.SetActive(false);
                lines.Add(lineObj);
            }
            lineRendererPool.Add(lines);
        }
    }

    void LoadModel(BackendType backend)
    {
        var model1 = ModelLoader.Load(asset);
        var centersToCornersData = new[] { 1, 0, 1, 0, 0, 1, 0, 1, -0.5f, 0, 0.5f, 0, 0, -0.5f, 0, 0.5f };

        var graph = new FunctionalGraph();
        var input = graph.AddInput(model1, 0);
        var modelOutput = Functional.Forward(model1, input)[0];

        var boxCoords = modelOutput[0, 0..4, ..].Transpose(0, 1);
        var scores = modelOutput[0, 4, ..];
        var keypointsData = modelOutput[0, 5.., ..].Transpose(0, 1);

        var boxCorners = Functional.MatMul(boxCoords, Functional.Constant(new TensorShape(4, 4), centersToCornersData));
        var indices = Functional.NMS(boxCorners, scores, iouThreshold, scoreThreshold);

        var indicesExpandedBox = indices.Unsqueeze(-1).BroadcastTo(new[] { 4 });
        var indicesExpandedKpts = indices.Unsqueeze(-1).BroadcastTo(new[] { 51 });

        var finalCoords = Functional.Gather(boxCoords, 0, indicesExpandedBox);
        var finalKeypointsData = Functional.Gather(keypointsData, 0, indicesExpandedKpts);

        var model2 = graph.Compile(finalCoords, finalKeypointsData);
        engine = new Worker(model2, backend);
    }

    bool isProcessing = false;
    public async Task<int> ExecuteModel(Texture inputTexture)
    {
        if (isProcessing) return -1;
        isProcessing = true;
        try
        {
            int numOfPeople = 0;
            float aspect = inputTexture.width * 1f / inputTexture.height;
            Graphics.Blit(inputTexture, targetRT, new Vector2(1f / aspect, 1), Vector2.zero);
            displayImage.texture = targetRT;

            using var input = TextureConverter.ToTensor(targetRT, imageWidth, imageHeight, 3);
            engine.Schedule(input);
            using var output_ = engine.PeekOutput(0) as Tensor<float>;
            using var ketPoints_ = engine.PeekOutput(1) as Tensor<float>;
            using var outputTensor = await output_.ReadbackAndCloneAsync();
            using var keyPointsTensor = await ketPoints_.ReadbackAndCloneAsync();

            ClearAnnotations();
            float displayWidth = displayImage.rectTransform.rect.width;
            float displayHeight = displayImage.rectTransform.rect.height;
            float scaleX = displayWidth / imageWidth;
            float scaleY = displayHeight / imageHeight;

            int boxesFound = outputTensor.shape[0];
            numOfPeople = Mathf.Min(boxesFound, maxPeople);

            for (int n = 0; n < numOfPeople; n++)
            {
                var poseBox = new BoundingPoseBox
                {
                    centerX = outputTensor[n, 0] * scaleX - displayWidth / 2,
                    centerY = outputTensor[n, 1] * scaleY - displayHeight / 2,
                    width = outputTensor[n, 2] * scaleX,
                    height = outputTensor[n, 3] * scaleY,
                    keypoints = new Keypoint[numJoints]
                };

                for (int kpIdx = 0; kpIdx < numJoints; kpIdx++)
                {
                    poseBox.keypoints[kpIdx] = new Keypoint
                    {
                        x = keyPointsTensor[n, kpIdx * 3] * scaleX - displayWidth / 2,
                        y = keyPointsTensor[n, kpIdx * 3 + 1] * scaleY - displayHeight / 2,
                        confidence = keyPointsTensor[n, kpIdx * 3 + 2]
                    };
                }

                DrawPoseBox(poseBox, n);
            }
            return numOfPeople;
        }
        finally { isProcessing = false; }
    }

    public void DrawPoseBox(BoundingPoseBox poseBox, int id)
    {
        GameObject panel = id < boxPool.Count ? boxPool[id] : CreateNewBox(Color.red);
        panel.SetActive(true);
        panel.transform.localPosition = new Vector3(poseBox.centerX, -poseBox.centerY);
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(poseBox.width, poseBox.height);

        var keypointObjects = keypointPool[id];
        for (int i = 0; i < numJoints; i++)
        {
            var kp = poseBox.keypoints[i];
            var kpObj = keypointObjects[i];
            kpObj.SetActive(kp.confidence > jointThreshold);
            if (kpObj.activeSelf)
                kpObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(kp.x, -kp.y);
        }

        var lineHolder = lineHolderPool[id];
        var lines = lineRendererPool[id];
        foreach (var line in lines) line.SetActive(false);

        int[,] connections = new int[,]
        {
            {0,1},{0,2},{1,3},{2,4},{5,6},{5,7},{5,11},{6,8},{6,12},{7,9},
            {8,10},{11,12},{11,13},{12,14},{13,15},{14,16}
        };

        int lineCount = 0;
        for (int i = 0; i < connections.GetLength(0) && lineCount < maxLines; i++)
        {
            int startIdx = connections[i, 0];
            int endIdx = connections[i, 1];
            var start = keypointObjects[startIdx];
            var end = keypointObjects[endIdx];
            if (start.activeSelf && end.activeSelf)
            {
                var lineObj = lines[lineCount++];
                lineObj.SetActive(true);
                var lr = lineObj.GetComponent<LineRenderer>();
                lr.SetPosition(0, start.GetComponent<RectTransform>().anchoredPosition);
                lr.SetPosition(1, end.GetComponent<RectTransform>().anchoredPosition);
            }
        }
    }

    public GameObject CreateNewBox(Color color)
    {
        var panel = new GameObject("ObjectBox");
        panel.AddComponent<CanvasRenderer>();
        var img = panel.AddComponent<Image>();
        img.color = color;
        img.sprite = borderSprite;
        img.type = Image.Type.Sliced;
        panel.transform.SetParent(displayLocation, false);
        boxPool.Add(panel);
        return panel;
    }

    public void ClearAnnotations()
    {
        foreach (var box in boxPool) box.SetActive(false);
        foreach (var kps in keypointPool)
            foreach (var kp in kps) kp.SetActive(false);
        foreach (var lines in lineRendererPool)
            foreach (var line in lines) line.SetActive(false);
    }

    private void OnDestroy()
    {
        centersToCorners?.Dispose();
        engine?.Dispose();
        if (targetRT != null)
        {
            targetRT.Release();
            Destroy(targetRT);
        }
    }
}
