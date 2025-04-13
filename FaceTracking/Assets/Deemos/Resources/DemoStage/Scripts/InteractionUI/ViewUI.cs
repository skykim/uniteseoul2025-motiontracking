using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InteractionUI
{
    public enum AutoCameraState
    {
        CloseUp,
        MidShot,
        FullShot
    }

    [RequireComponent(typeof(InteractionUIScripts))]
    public class ViewUI : MonoBehaviour
    {
        [Header("Camera Control")]
        [SerializeField] ToggleGroup m_CameraOptions;
        [SerializeField] Toggle m_ToggleCloseUp, m_ToggleMidShot, m_ToggleFullShot, m_ToggleFree;
        [SerializeField] Dictionary<string, Toggle> m_CameraToggles;
        [SerializeField] CameraControl m_FreePivotController;
        [SerializeField] RigCameraSwitch m_RigCameraSwitch;
        [SerializeField] Animator m_CameraAnimator;
        [Header("Cameras")]
        [SerializeField] Camera m_CameraCloseUp;
        [SerializeField] Camera m_CameraMidShot;
        [SerializeField] Camera m_CameraFullShot;
        public AutoCameraState currentState;

        InteractionUIScripts m_MainUI;

        public void SetToggle(string toggleName)
        {
            m_CameraToggles[toggleName].isOn = true;
        }

        public void SetToggleInteractable(bool value)
        {
            foreach (var keyValuePair in m_CameraToggles)
            {
                keyValuePair.Value.interactable = value;
            }
        }

        public void SwitchPanel(bool newState)
        {
            m_RigCameraSwitch.inRigPanel = newState;
            SetToggleInteractable(!newState);
        }

        public void SetCameraState(AutoCameraState autoCameraState, bool cameraAnimatorState)
        {
            switch (autoCameraState)
            {
                case AutoCameraState.CloseUp:
                    HandleCameraToggle(true, m_CameraCloseUp.transform, true, cameraAnimatorState);
                    break;
                case AutoCameraState.MidShot:
                    HandleCameraToggle(true, m_CameraMidShot.transform, false, cameraAnimatorState);
                    break;
                case AutoCameraState.FullShot:
                    HandleCameraToggle(true, m_CameraFullShot.transform, false, cameraAnimatorState);
                    break;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            m_MainUI = GetComponent<InteractionUIScripts>();
            m_CameraToggles = InteractionUIScripts.GetToggles(m_CameraOptions);
            if (!m_CameraToggles.TryGetValue("Close-up", out m_ToggleCloseUp)
              ||!m_CameraToggles.TryGetValue("Mid Shot", out m_ToggleMidShot)
              ||!m_CameraToggles.TryGetValue("Full Shot", out m_ToggleFullShot)
              ||!m_CameraToggles.TryGetValue("Free", out m_ToggleFree)
            )
            {
                enabled = false;
                return;
            }
            SetupCameraToggle(m_CameraCloseUp, m_ToggleCloseUp, true);
            SetupCameraToggle(m_CameraMidShot, m_ToggleMidShot, false);
            SetupCameraToggle(m_CameraFullShot, m_ToggleFullShot, false);
            m_ToggleFree.onValueChanged.AddListener(HandleFreePivotModeToggle);

            m_ToggleCloseUp.isOn = true;
            HandleCameraToggle(true, m_CameraCloseUp.transform, true);
        }

        private void Update()
        {
            if (
                // Button
                (
                    (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) &&
                    !m_MainUI.CheckGuiRaycastObjects()
                ) ||
                // scroll
                (Input.mouseScrollDelta.y != 0)
            )
            {
                m_ToggleFree.isOn = true;
                HandleFreePivotModeToggle(true);
            }

            if (Input.GetKeyDown(KeyCode.Alpha1)) // Switch to CloseUp Camera
            {
                m_ToggleCloseUp.isOn = true;
                HandleCameraToggle(true, m_CameraCloseUp.transform, true);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2)) // MidShot
            {
                m_ToggleMidShot.isOn = true;
                HandleCameraToggle(true, m_CameraMidShot.transform, true);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3)) // FullShot
            {
                m_ToggleFullShot.isOn = true;
                HandleCameraToggle(true, m_CameraFullShot.transform, true);
            }
        }

        void SetupCameraToggle(Camera camera, Toggle toggle, bool focusing)
        {
            toggle.onValueChanged.AddListener((x) => HandleCameraToggle(x, camera.transform, focusing));
            camera.gameObject.SetActive(false);
        }

        void HandleCameraToggle(bool enable, Transform targetCamera, bool focusing, bool animationState=true)
        {
            if (enable)
            {
                m_CameraAnimator.enabled = animationState;
                m_FreePivotController.AttachToTarget(targetCamera);
                //if (m_FreePivotController.focusHandler != null)
                //{
                //    if (focusing)
                //        m_FreePivotController.focusHandler.StartFocus();
                //    else
                //        m_FreePivotController.focusHandler.CancelFocus(instantly: true);
                //}
            }
        }

        void HandleFreePivotModeToggle(bool enable)
        {
            m_CameraAnimator.enabled = !enable;
            if (enable)
            {
                m_FreePivotController.ControlEnable();
            }
        }
    }
}
