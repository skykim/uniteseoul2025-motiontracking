using UnityEngine;
using TMPro;

public class MarkerController : MonoBehaviour
{
    //private TextMeshProUGUI _textMesh;
    private TMP_Text _textMesh;
    public float lastUpdateTime;
    private Camera _camera;

    private void Awake()
    {
        _camera = Camera.main;
        //_textMesh = GetComponentInChildren<TextMeshProUGUI>();
        _textMesh = GetComponentInChildren<TMP_Text>();
        if (_textMesh == null)
        {
            Debug.LogError("No TextMeshProUGUI found on marker prefab!");
        }
    }

    /// <summary>
    /// Updates the marker’s transform and text, and records the update time.
    /// </summary>
    public void UpdateMarker(Vector3 position, Quaternion rotation, Vector3 scale, string text)
    {
        transform.SetPositionAndRotation(position, rotation);
        transform.localScale = scale;
        if (_textMesh)
        {
            _textMesh.text = text;
        }
        
        lastUpdateTime = Time.time;
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (_textMesh)
        {
            _textMesh.transform.rotation = Quaternion.LookRotation(_textMesh.transform.position - _camera.transform.position);
        }
    
        if (gameObject.activeSelf && Time.time - lastUpdateTime > 2f)
        {
            gameObject.SetActive(false);
        }
    }
}
