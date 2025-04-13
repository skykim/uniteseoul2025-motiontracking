using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FacialController {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.UI.Slider))]
    public class FacialControllerHandle1D : MonoBehaviour, IFacialControllerHandle {
        [SerializeField]
        UnityEngine.UI.Slider m_slider;
        public UnityEngine.UI.Slider slider
        {
            get
            {
                return m_slider;
            }
        }

        public bool toBeAligned = true;
        public GameObject m_backgroundObject;
        public GameObject m_handleObject;

        public string m_name
        {
            get
            {
                return name;
            }
        }
        public float translateX => m_slider.value;
        public float maxTransXLimit => m_slider.maxValue;
        public float minTransXLimit => m_slider.minValue;
        public FacialControllers main_controller;

        [SerializeField]
        public List<FacialControllerCustomValue> m_customValue = new List<FacialControllerCustomValue> { new FacialControllerCustomValue("JointScale", 1) };

        public bool initialized
        {
            get
            {
                return m_initialized;
            }
        }

        private bool m_initialized = false;

        private void Awake() {
            Init();
        }

        public void Init()
        {
            m_slider = GetComponent<UnityEngine.UI.Slider>();
            main_controller = GetComponentInParent<FacialControllers>();
            if (main_controller == null)
            {
                Debug.LogError("No main controller found!");
            }
            m_backgroundObject = transform.Find("Background").gameObject;
            m_handleObject = transform.Find("Handle Slide Area").Find("Handle").gameObject;
            m_initialized = true;
        }

        void Start()
        {
            m_slider.onValueChanged.AddListener(OnSliderValueChange);
        }

        public void OnSliderValueChange(float value) {
            main_controller.ChangeValue(this);
        }
    }
}

