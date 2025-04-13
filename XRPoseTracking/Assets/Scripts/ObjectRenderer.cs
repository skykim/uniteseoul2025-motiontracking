using Meta.XR;
using UnityEngine;
using Unity.Sentis;
using PassthroughCameraSamples;
using System.Collections.Generic;
using UnityEngine.UI;

public class ObjectRenderer : MonoBehaviour
{
    [Header("Camera & Raycast Settings")]
    [SerializeField] private WebCamTextureManager webCamTextureManager;
    [SerializeField] private EnvironmentRaycastManager envRaycastManager;
    [SerializeField] private float mergeThreshold = 0.01f;
    
    private Camera _mainCamera;
    private const float YoloInputSize = 640f;
    private const int maxPeople = 50;
    private const int numJoints = 17;
   

    public GameObject markerPrefab;
    public GameObject keypointsPrefab;

    private readonly Dictionary<string, MarkerController> _activeMarkers = new();
    private readonly Dictionary<string, KeypointsController> _activeKeypoints = new();

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    public void RenderPoseDetections(Tensor<float> coords, Tensor<float> keypoints)
    {
        var numDetections = coords.shape[0];
        print($"[Detection3DRenderer] RenderDetections: {numDetections} detections received.");
        ClearPreviousMarkers();

        int numOfPeople = Mathf.Min(numDetections, maxPeople);

        var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(webCamTextureManager.Eye);
        var camRes = intrinsics.Resolution;
        
        var imageWidth = YoloInputSize;
        var imageHeight = YoloInputSize;
        var halfWidth = imageWidth * 0.5f;
        var halfHeight = imageHeight * 0.5f;

        for (var i = 0; i < numOfPeople; i++)
        {             
            var detectedCenterX = coords[i, 0];
            var detectedCenterY = coords[i, 1];
            var detectedWidth = coords[i, 2];
            var detectedHeight = coords[i, 3];

            var adjustedCenterX = detectedCenterX - halfWidth;
            var adjustedCenterY = detectedCenterY - halfHeight;

            var perX = (adjustedCenterX + halfWidth) / imageWidth;
            var perY = (adjustedCenterY + halfHeight) / imageHeight;

            var centerPixel = new Vector2(perX * camRes.x, (1.0f - perY) * camRes.y);
            print($"[Detection3DRenderer] Detection {i} Center Pixel: {centerPixel}");

            var centerRay = PassthroughCameraUtils.ScreenPointToRayInWorld(webCamTextureManager.Eye, new Vector2Int(Mathf.RoundToInt(centerPixel.x), Mathf.RoundToInt(centerPixel.y)));

            if (!envRaycastManager.Raycast(centerRay, out var centerHit))
            {
                Debug.LogWarning($"[Detection3DRenderer] Detection {i}: Environment raycast failed.");
                continue;
            }

            var markerWorldPos = centerHit.point;

            var u1 = (detectedCenterX - detectedWidth * 0.5f) / imageWidth;
            var v1 = (detectedCenterY - detectedHeight * 0.5f) / imageHeight;
            var u2 = (detectedCenterX + detectedWidth * 0.5f) / imageWidth;
            var v2 = (detectedCenterY + detectedHeight * 0.5f) / imageHeight;

            var tlPixel = new Vector2Int(
                Mathf.RoundToInt(u1 * camRes.x),
                Mathf.RoundToInt((1.0f - v1) * camRes.y)
            );
            
            var brPixel = new Vector2Int(
                Mathf.RoundToInt(u2 * camRes.x),
                Mathf.RoundToInt((1.0f - v2) * camRes.y)
            );

            var tlRay = PassthroughCameraUtils.ScreenPointToRayInWorld(webCamTextureManager.Eye, tlPixel);
            var brRay = PassthroughCameraUtils.ScreenPointToRayInWorld(webCamTextureManager.Eye, brPixel);

            var depth = Vector3.Distance(_mainCamera.transform.position, markerWorldPos);
            var worldTL = tlRay.GetPoint(depth);
            var worldBR = brRay.GetPoint(depth);

            var markerWidth = Mathf.Abs(worldBR.x - worldTL.x);
            var markerHeight = Mathf.Abs(worldBR.y - worldTL.y);
            var markerScale = new Vector3(markerWidth, markerHeight, 1f);

            var detectedLabel = YOLOv9Labels.person;

            var labelKey = detectedLabel.ToString();
            if (_activeMarkers.TryGetValue(labelKey, out MarkerController existingMarker))
            {
                if (Vector3.Distance(existingMarker.transform.position, markerWorldPos) < mergeThreshold)
                {
                    existingMarker.UpdateMarker(markerWorldPos, Quaternion.LookRotation(-centerHit.normal, Vector3.up), markerScale, labelKey);

                    if (_activeKeypoints.TryGetValue(labelKey, out KeypointsController existingKeypoints))
                    {
                        for(int kp=0; kp<numJoints; kp++)
                        {
                            existingKeypoints.DrawKeypoints(kp, keypoints[i, kp*3 + 0], keypoints[i, kp*3 + 1], keypoints[i, kp*3 + 2], detectedCenterX, detectedCenterY, imageWidth, imageHeight, markerWorldPos, depth, camRes, intrinsics.FocalLength, _mainCamera.transform);
                        }
                        existingKeypoints.DrawLines();
                    }
                    continue;
                }
                labelKey += $"_{i}";
            }


            var markerGo = Instantiate(markerPrefab);            
            var marker = markerGo.GetComponent<MarkerController>();

            if (!marker)
            {
                Debug.LogWarning($"[Detection3DRenderer] Detection {i}: Marker prefab is missing a MarkerController component.");
                continue;
            }

            marker.UpdateMarker(markerWorldPos, Quaternion.LookRotation(-centerHit.normal, Vector3.up), markerScale, labelKey);
            _activeMarkers[labelKey] = marker;
            
            var keypointsGo = Instantiate(keypointsPrefab);
            var keypointsController = keypointsGo.GetComponent<KeypointsController>();

            if (!keypointsController)
            {
                Debug.LogWarning($"[Detection3DRenderer] Detection {i}: Keypoints prefab is missing a KeypointsController component.");
                continue;
            }

            for(int kp=0; kp<numJoints; kp++)
            {
                keypointsController.DrawKeypoints(kp, keypoints[i, kp*3 + 0], keypoints[i, kp*3 + 1], keypoints[i, kp*3 + 2], detectedCenterX, detectedCenterY, imageWidth, imageHeight, markerWorldPos, depth, camRes, intrinsics.FocalLength, _mainCamera.transform);
            }           
            keypointsController.DrawLines();
            _activeKeypoints[labelKey] = keypointsController;
            
            print($"[Detection3DRenderer] Detection {i}: Marker placed with label: {labelKey}");
        }        
    }    

    private void ClearPreviousMarkers()
    {
        foreach (var marker in _activeMarkers.Values)
        {
            if (marker && marker.gameObject)
            {
                Destroy(marker.gameObject);
            }
        }
        _activeMarkers.Clear();

        foreach (var keypoints in _activeKeypoints.Values)
        {
            if (keypoints && keypoints.gameObject)
            {
                Destroy(keypoints.gameObject);
            }
        }
        _activeKeypoints.Clear();
    }
}
