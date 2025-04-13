#pragma warning disable 0649

using UnityEngine;
using UnityEngine.UI;

public class InteractionUI_old : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] Toggle toggleFace;
    [SerializeField] Camera cameraFace;
    [SerializeField] Toggle toggleMidshot;
    [SerializeField] Camera cameraMidshot;
    [SerializeField] Toggle toggleBody;
    [SerializeField] Camera cameraBody;

    [Space]
    [SerializeField] Animator cameraAnimator;
    //[SerializeField] float cameraFaceSpeedMultiplier = 0.5f;

    [Space]
    [SerializeField] Toggle toggleFreePivot;
    [SerializeField] CameraControl freePivotController;

    [Header("Head Control Mode")]
    [SerializeField] Toggle toggleCustom;
    [SerializeField] Toggle toggleLive;
    [SerializeField] Toggle toggleRigPanel;
    [SerializeField] Camera cameraRig;
    [SerializeField] GameObject rigPanel;
    [System.NonSerialized] FacialController.FacialControllers facialControllers;

    [System.NonSerialized] bool initialized; // Not serialized to detect domain reload as UI event references are not serialized


    void OnEnable()
    {
        if (!initialized)
            Initialize();
    }

    void Start()
    {
        SetDefaults();
    }

    void Initialize()
    {
        // Facial action source
        toggleCustom.onValueChanged.AddListener(HandleToggleCustom);

        toggleRigPanel.onValueChanged.AddListener(HandleToggleRigPanel);
        facialControllers = rigPanel.GetComponent<FacialController.FacialControllers>();

        toggleLive.onValueChanged.AddListener(HandleToggleLive);

        initialized = true;
    }

    void HandleToggleCustom(bool x)
    {
        Debug.Log("custom: " + x.ToString());
        switch (x){
            case true:
                break;
            case false:
                break;
        }
    }

    void HandleToggleRigPanel(bool x)
    {
        Debug.Log("rig: " + x.ToString());
        switch (x){
            case true:
                cameraAnimator.enabled = false;
                // Show panel
                rigPanel.SetActive(true);
                break;
            case false:
                rigPanel.SetActive(false);
                
                break;
        }
    }
    void HandleToggleLive(bool x)
    {
        Debug.Log("live: " + x.ToString());
        switch (x){
            case true:
                break;
            case false:
                break;
        }
    }

    void SetDefaults()
    {
        ForceToggleValue(toggleFace, true);
        ForceToggleValue(toggleCustom, true);
    }

    void ForceToggleValue(Toggle toggle, bool shouldBeOn)
    {
        if (toggle.isOn == shouldBeOn)
        {
            toggle.onValueChanged.Invoke(shouldBeOn);
        }
        else
        {
            toggle.isOn = shouldBeOn;
        }
    }
}