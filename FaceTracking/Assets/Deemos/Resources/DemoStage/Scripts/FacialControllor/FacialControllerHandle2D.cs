using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FacialController {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.UI.Slider2D))]
    public class FacialControllerHandle2D : MonoBehaviour, IFacialControllerHandle {
        [SerializeField]
        UnityEngine.UI.Slider2D m_slider;
        public UnityEngine.UI.Slider2D slider
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
        public float translateX => m_slider.value.x;
        public float maxTransXLimit => m_slider.maxValueX;
        public float minTransXLimit => m_slider.minValueX;
        public float translateY => m_slider.value.y;
        public float maxTransYLimit => m_slider.maxValueY;
        public float minTransYLimit => m_slider.minValueY;
        public FacialControllers main_controller;

        public bool initialized
        {
            get
            {
                return m_initialized;
            }
        }

        private bool m_initialized = false;

        [SerializeField]
        public List<FacialControllerCustomValue> m_customValue = new List<FacialControllerCustomValue> { new FacialControllerCustomValue("JointScale", 1) };


        private void Awake() {
            Init();
        }

        public void Init()
        {
            m_slider = GetComponent<UnityEngine.UI.Slider2D>();
            main_controller = GetComponentInParent<FacialControllers>();
            if (main_controller == null)
            {
                Debug.LogError("No main controller found!");
            }
            m_backgroundObject = transform.Find("Background").gameObject;
            m_handleObject = transform.Find("Handle Slide Area").Find("Handle").gameObject;
            m_initialized = true;
        }

        private void Start()
        {
            m_slider.onValueChanged.AddListener(OnSliderValueChange);
        }

        public void OnSliderValueChange(Vector2 value) {
            main_controller.ChangeValue(this);
        }
    }
}

