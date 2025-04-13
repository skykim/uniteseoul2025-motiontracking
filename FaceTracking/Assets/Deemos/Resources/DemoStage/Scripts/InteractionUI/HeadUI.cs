using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using FacialController;
using Unity.LiveCapture.ARKitFaceCapture;
using Unity.LiveCapture;

namespace InteractionUI
{
    public enum MainState
    {
        Preset,
        Livelink,
        Custom
    }

    [RequireComponent(typeof(InteractionUIScripts))]
    public class HeadUI : MonoBehaviour
    {
        [Header("Head Options")]
        [SerializeField] ToggleGroup m_HeadOptions;
        [SerializeField] Toggle m_TogglePreset, m_ToggleLivelink, m_ToggleCustom;
        [SerializeField] Dictionary<string, Toggle> m_HeadToggles;

        [Space]
        [Header("Blend Shape Controllers")]
        [SerializeField] public FaceActor m_FaceActor;
        [SerializeField] public HeadGroup m_HeadGroup;
        [SerializeField] public PlayableDirector m_PresetPlayer;

        [Space]
        [Header("Preset")]
        [SerializeField] PlayableAsset m_PresetAnimation;

        [Space]
        [Header("Livelink")]
        //[SerializeField] public TakeRecorder m_TakeRecorder; //disable TakeRecorder
        
        [SerializeField] public FaceDevice m_FaceDevice;

        [Space]
        [Header("Rig Panel")]
        [SerializeField] FacialControllers m_FacialController;
        GameObject m_RigPanelObject;
        [SerializeField] Toggle m_ToggleRigPanelVisible;

        [Space]
        [Header("Other UI controller")]
        [SerializeField] ViewUI m_ViewUIController;
        [SerializeField] LightUI m_lightUIController;
        public float presetCloseupLightNotChangingTime;
        [SerializeField] CharacterAnimationUI m_CharacterAnimationUI;


        // Start is called before the first frame update
        void Start()
        {
            m_HeadToggles = InteractionUIScripts.GetToggles(m_HeadOptions);
            if (//!m_HeadToggles.TryGetValue("Preset", out m_TogglePreset)||
              !m_HeadToggles.TryGetValue("Live", out m_ToggleLivelink)||
              !m_HeadToggles.TryGetValue("Custom", out m_ToggleCustom)||
               m_ToggleRigPanelVisible == null
              )
            {
                enabled = false;
                return;
            }
            //m_TogglePreset.onValueChanged.AddListener(HandlePreset);
            m_ToggleLivelink.onValueChanged.AddListener(HandleLivelink);
            m_ToggleCustom.onValueChanged.AddListener(HandleCustom);
            m_ToggleRigPanelVisible.onValueChanged.AddListener(HandleRig);
            m_RigPanelObject = m_FacialController.gameObject;
            m_RigPanelObject.SetActive(false);
            /*
            if (m_TakeRecorder)
            {
                m_TakeRecorder.enabled = false;
            }
            */
            HandleLivelink(true);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha8)) // Livelink
            {
                m_ToggleLivelink.isOn = true;
                HandleLivelink(true);
            }
            if (Input.GetKeyDown(KeyCode.Alpha9)) // Custom
            {
                m_ToggleCustom.isOn = true;
                HandleCustom(true);
            }
        }

        void HandlePreset(bool x)
        {
            if (x)
            {
                EnterPreset();
            }
            else
            {
                ExitPreset();
            }
        }
        void HandleLivelink(bool x)
        {
            if (x)
            {
                EnterLivelink();
            }
            else
            {
                ExitLivelink();
            }
        }
        void HandleCustom(bool x)
        {
            if (x)
            {
                EnterCustom();
            }
            else
            {
                ExitCustom();
            }
        }
        void HandleRig(bool x)
        {
            if (x)
            {
                RigPanelOn();
            }
            else
            {
                RigPanelOff();
            }
        }

        void EnterPreset()
        {
            m_FaceActor.enabled = true;
            m_PresetPlayer.extrapolationMode = DirectorWrapMode.Loop;
            m_PresetPlayer.initialTime = 0;
            m_PresetPlayer.enabled = true;
            m_PresetPlayer.Play(m_PresetAnimation);
        }

        void ExitPreset()
        {
            m_FaceActor.enabled = false;
            m_PresetPlayer.Pause();
            m_PresetPlayer.enabled = false;
        }
        void EnterCustom()
        {
            m_ToggleRigPanelVisible.gameObject.SetActive(true);
            //m_CharacterAnimationUI.SetState(CharacterState.Pose);
            m_ViewUIController.SetToggle("Close-up");
            m_ViewUIController.SetCameraState(AutoCameraState.CloseUp, false);
            m_HeadGroup.ResetBlendShape();
        }
        void ExitCustom()
        {
            RigPanelOff();
            m_FacialController.ResetPanel();
            m_ToggleRigPanelVisible.isOn = false;
            m_ToggleRigPanelVisible.gameObject.SetActive(false);
            m_ViewUIController.SetCameraState(AutoCameraState.CloseUp, true);
        }
        void EnterLivelink()
        {
            if (m_FaceActor)
            {
                m_FaceActor.enabled = true;
                //m_TakeRecorder.enabled = true;
            }
            
            //m_FaceDevice.enabled = true;
            //m_FaceDevice.m_Pose = FacePose.Identity;
        }

        void ExitLivelink()
        {
            if (m_FaceActor)
            {
                m_FaceActor.enabled = false;
                //m_TakeRecorder.enabled = false;
            }
            //m_FaceDevice.m_Pose = FacePose.Identity;
            //m_FaceDevice.enabled = false;
        }

        void RigPanelOn()
        {
            m_RigPanelObject.SetActive(true);
            m_ViewUIController.SwitchPanel(true);
        }

        void RigPanelOff()
        {
            m_RigPanelObject.SetActive(false);
            m_ViewUIController.SwitchPanel(false);
        }
    }
}