using UnityEngine;

public class PassthroughHandler : MonoBehaviour
{
    private void Start()
    {
        // This is a property that determines whether premultiplied alpha blending is used for the eye field of view
        // layer, which can be adjusted to enhance the blending with underlays and potentially improve visual quality.
        OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;
    }
}
