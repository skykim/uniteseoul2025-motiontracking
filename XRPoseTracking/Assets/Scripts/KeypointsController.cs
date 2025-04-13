using UnityEngine;
using UnityEngine.InputSystem;

public class KeypointsController : MonoBehaviour
{
    private const int numJoints = 17;
    private Keypoint[] _keypointsWorldPose = new Keypoint[numJoints];
    public struct Keypoint { public Vector3 position; public float confidence; }

    const float threhsoldConfidence = 0.5f;

    Transform GetPointTransform(int index)
    {
        Transform childTransform = transform.Find("point_" + index.ToString());
        return childTransform;
    }

    LineRenderer GetLineRenderer(int index)
    {
        Transform lineTransform = transform.Find("line_" + index.ToString());
        if (lineTransform != null)
        {
            return lineTransform.GetComponent<LineRenderer>();
        }
        return null;
    }

    public void DrawKeypoints(int index, float x, float y, float confidence, float detectionCenterX, float detectionCenterY,
        float imageWidth, float imageHeight,
        Vector3 markerWorldPos,
        float depth,
        Vector2 camRes,
        Vector2 focalLength,
        Transform cameraTransform)
    {

        if (confidence > threhsoldConfidence)
        {
            _keypointsWorldPose[index].position = ComputeKeypointsWorldPosition(
                x, y,
                detectionCenterX, detectionCenterY,
                imageWidth, imageHeight,
                markerWorldPos,
                depth,
                camRes,
                focalLength,
                cameraTransform
            );
            _keypointsWorldPose[index].confidence = confidence;            
            Transform pointTransform = GetPointTransform(index);
            if (pointTransform != null)
            {
                pointTransform.position = _keypointsWorldPose[index].position;
                pointTransform.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"[KeypointsController] Transform for point {index} not found.");
            }
        }
        else
        {
            _keypointsWorldPose[index].confidence = 0f;
            Transform pointTransform = GetPointTransform(index);
            if (pointTransform != null)
            {
                pointTransform.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"[KeypointsController] Transform for point {index} not found.");
            }
        }
    }

    public void DrawLines()
    {
        // Define the connections between keypoints
        int[,] connections = new int[,]
        {
            { 0, 1 }, { 0, 2 }, { 1, 3 }, { 2, 4 },
            { 5, 6 }, { 5, 7 }, { 5, 11},
            { 6, 8 }, { 6 ,12}, { 7, 9 }, { 8, 10},
            {11, 12}, {11, 13}, {12, 14},
            {13, 15}, {14, 16}
        };

        for (int i = 0; i < connections.GetLength(0); i++)
        {
            int startIndex = connections[i, 0];
            int endIndex = connections[i, 1];

            if (_keypointsWorldPose[startIndex].confidence > threhsoldConfidence && _keypointsWorldPose[endIndex].confidence > threhsoldConfidence)
            {
                //line renderer
                LineRenderer lineRenderer = GetLineRenderer(i);
                if (lineRenderer != null)
                {
                    lineRenderer.SetPosition(0, _keypointsWorldPose[startIndex].position);
                    lineRenderer.SetPosition(1, _keypointsWorldPose[endIndex].position);
                    lineRenderer.startWidth = 0.003f;
                    lineRenderer.endWidth = 0.003f;
                    lineRenderer.startColor = lineRenderer.endColor = Color.cyan;
                    lineRenderer.useWorldSpace = true;
                    lineRenderer.material = new Material(Shader.Find("Sprites/Default"));                    
                    lineRenderer.gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogWarning($"[KeypointsController] Line renderer for connection {i} not found.");
                }
            }
        }

    }

    public Vector3 ComputeKeypointsWorldPosition(
        float keypointX, float keypointY,
        float detectionCenterX, float detectionCenterY,
        float imageWidth, float imageHeight,
        Vector3 markerWorldPos,
        float depth,
        Vector2 camRes,
        Vector2 focalLength,
        Transform cameraTransform)
    {
        float deltaX_pixels = keypointX - detectionCenterX;
        float deltaY_pixels = keypointY - detectionCenterY;

        float deltaX_norm = deltaX_pixels / imageWidth;
        float deltaY_norm = deltaY_pixels / imageHeight;
        
        Vector2 pixelOffset = new Vector2(deltaX_norm * camRes.x, -deltaY_norm * camRes.y);

        float offsetX_world = pixelOffset.x * depth / focalLength.x;
        float offsetY_world = pixelOffset.y * depth / focalLength.y;

        Vector3 worldOffset = cameraTransform.right * offsetX_world + cameraTransform.up * offsetY_world;

        return markerWorldPos + worldOffset;
    }    
}
