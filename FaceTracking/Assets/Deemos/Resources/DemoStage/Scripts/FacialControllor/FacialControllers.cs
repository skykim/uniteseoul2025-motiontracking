using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FacialController {
    #if UNITY_EDITOR
    [CustomEditor(typeof(FacialControllers))]
    public class FacialControllersEditor: Editor
    {
        FacialControllers facialControllers;
        private void OnEnable()
        {
            facialControllers = (FacialControllers)target;
        }

        public override void OnInspectorGUI()
        {

            DrawDefaultInspector();
            if (GUILayout.Button("Load All Controllers"))
            {
                facialControllers.Start();
            }
            if (GUILayout.Button("Mirror Right to Left"))
            {
                Dictionary<string, FacialControllerHandleListElem> waitingDict = new Dictionary<string, FacialControllerHandleListElem>();
                foreach (FacialControllerHandleListElem iter in facialControllers.m_controllerListReadOnly)
                {
                    if (!iter.toBeAligned) continue;
                    string pureName = iter.name.Replace("Left", "").Replace("Right", "");
                    if (pureName == iter.name)
                    {
                        // Rotation
                        iter.transform.localRotation = Quaternion.Euler(0, 0, iter.transform.localRotation.eulerAngles.z);
                        // Axis y can be safely ignored
                        // However x axis should be considered
                        float anchorXLength = iter.transform.anchorMax.x - iter.transform.anchorMin.x;
                        float anchorYMin = iter.transform.anchorMin.y, anchorYMax = iter.transform.anchorMax.y;
                        iter.transform.anchorMin = new Vector2(0.5f - anchorXLength / 2, anchorYMin);
                        iter.transform.anchorMax = new Vector2(0.5f + anchorXLength / 2, anchorYMax);
                        Vector3 newPosition = iter.transform.localPosition;
                        newPosition.x = 0;
                        iter.transform.localPosition = newPosition;
                    }
                    else if (!waitingDict.ContainsKey(pureName))
                    {
                        waitingDict.Add(pureName, iter);
                    }
                    else if (waitingDict.ContainsKey(pureName))
                    {
                        // Find which is which
                        FacialControllerHandleListElem leftElem, rightElem;
                        if (iter.name.Contains("Left"))
                        {
                            leftElem = iter;
                            rightElem = waitingDict[pureName];
                        } else
                        {
                            leftElem = waitingDict[pureName];
                            rightElem = iter;
                        }
                        // mirror from right to left
                        // scale
                        leftElem.transform.localScale = rightElem.transform.localScale;
                        // rotation
                        Vector3 rightRotationEuler = rightElem.transform.localRotation.eulerAngles;
                        rightRotationEuler.z = -rightRotationEuler.z;
                        leftElem.transform.localRotation = Quaternion.Euler(rightRotationEuler);
                        // anchor
                        leftElem.transform.anchorMin = new Vector2(1 - rightElem.transform.anchorMax.x, rightElem.transform.anchorMin.y);
                        leftElem.transform.anchorMax = new Vector2(1 - rightElem.transform.anchorMin.x, rightElem.transform.anchorMax.y);
                        // position
                        Vector3 leftPosition = rightElem.transform.localPosition;
                        leftPosition.x = -leftPosition.x;
                        leftElem.transform.localPosition = leftPosition;
                    }
                }
            }
            if (GUILayout.Button("Mirror Left to Right"))
            {
                Dictionary<string, FacialControllerHandleListElem> waitingDict = new Dictionary<string, FacialControllerHandleListElem>();
                foreach (FacialControllerHandleListElem iter in facialControllers.m_controllerListReadOnly)
                {
                    if (!iter.toBeAligned) continue;
                    string pureName = iter.name.Replace("Left", "").Replace("Right", "");
                    if (pureName == iter.name)
                    {
                        // Rotation
                        iter.transform.localRotation = Quaternion.Euler(0, 0, iter.transform.localRotation.eulerAngles.z);
                        // Axis y can be safely ignored
                        // However x axis should be considered
                        float anchorXLength = iter.transform.anchorMax.x - iter.transform.anchorMin.x;
                        float anchorYMin = iter.transform.anchorMin.y, anchorYMax = iter.transform.anchorMax.y;
                        iter.transform.anchorMin = new Vector2(0.5f - anchorXLength / 2, anchorYMin);
                        iter.transform.anchorMax = new Vector2(0.5f + anchorXLength / 2, anchorYMax);
                        Vector3 newPosition = iter.transform.localPosition;
                        newPosition.x = 0;
                        iter.transform.localPosition = newPosition;
                    }
                    else if (!waitingDict.ContainsKey(pureName))
                    {
                        waitingDict.Add(pureName, iter);
                    }
                    else if (waitingDict.ContainsKey(pureName))
                    {
                        // Find which is which
                        FacialControllerHandleListElem leftElem, rightElem;
                        if (iter.name.Contains("Left"))
                        {
                            leftElem = iter;
                            rightElem = waitingDict[pureName];
                        }
                        else
                        {
                            leftElem = waitingDict[pureName];
                            rightElem = iter;
                        }
                        // mirror from right to left
                        // scale
                        rightElem.transform.localScale = leftElem.transform.localScale;
                        // rotation
                        Vector3 leftRotationEuler = leftElem.transform.localRotation.eulerAngles;
                        leftRotationEuler.z = -leftRotationEuler.z;
                        rightElem.transform.localRotation = Quaternion.Euler(leftRotationEuler);
                        // anchor
                        rightElem.transform.anchorMin = new Vector2(1 - leftElem.transform.anchorMax.x, leftElem.transform.anchorMin.y);
                        rightElem.transform.anchorMax = new Vector2(1 - leftElem.transform.anchorMin.x, leftElem.transform.anchorMax.y);
                        // position
                        Vector3 rightPosition = leftElem.transform.localPosition;
                        rightPosition.x = -rightPosition.x;
                        rightElem.transform.localPosition = rightPosition;
                    }
                }
            }

        }
    }
    #endif
    public interface IFacialControllerHandle
    {
    }

    // handle operations
    [Serializable]
    public class FacialControllerHandleListElem
    {
        public string name = "";
        public FacialControllerHandle1D handle1D = null;
        public FacialControllerHandle2D handle2D = null;
        public bool is1D = false;
        public bool is2D = false;
        Image m_bg = null;
        public RectTransform handleTransform = null;
        public RectTransform backgroundTransform = null;
        public RectTransform transform = null;
        public float objectScale = 0;
        public bool toBeAligned = false;

        public FacialControllerHandleListElem(IFacialControllerHandle handle)
        {
            name = "";
            handle1D = null;
            handle2D = null;

            if (handle is FacialControllerHandle1D h1)
            {
                is1D = true;
                handle1D = h1;
                if (!handle1D.initialized)
                {
                    handle1D.Init();
                }
                toBeAligned = h1.toBeAligned;
                name = h1.name;
                transform = (RectTransform)handle1D.transform;
                m_bg = handle1D.m_backgroundObject.GetComponent<Image>();
                handleTransform = (RectTransform)handle1D.m_handleObject.transform;
                backgroundTransform = (RectTransform)handle1D.m_backgroundObject.transform;
                objectScale = handle1D.transform.localScale.x;
            }
            else if (handle is FacialControllerHandle2D h2)
            {
                is2D = true;
                handle2D = h2;
                if (!handle2D.initialized)
                {
                    handle2D.Init();
                }
                toBeAligned = h2.toBeAligned;
                name = h2.name;
                transform = (RectTransform)handle2D.transform;
                m_bg = handle2D.m_backgroundObject.GetComponent<Image>();
                handleTransform = (RectTransform)handle2D.m_handleObject.transform;
                backgroundTransform = (RectTransform)handle2D.m_backgroundObject.transform;
                objectScale = handle2D.transform.localScale.x;
            }

        }

        public Sprite sliderBGSprite
        {
            get
            {
                if (m_bg)
                {
                    return m_bg.sprite;
                }
                return null;
            }
            set
            {
                if (m_bg)
                {
                    m_bg.sprite = value;
                }
            }
        }
    }

    public class FacialControllers : MonoBehaviour {
        // Blendshape control

        [SerializeField]
        float m_BlendShapeScale = 100f;

        [SerializeField]
        public HeadGroup m_HeadGroup;

        [SerializeField]
        float handleSize = 40f;

        Dictionary<string, FacialControllerHandleListElem> m_controllers = new Dictionary<string, FacialControllerHandleListElem>();
        [SerializeField]
        public List<FacialControllerHandleListElem> m_controllerListReadOnly = new List<FacialControllerHandleListElem>();

        // Start is called before the first frame update
        public void Start() {
            m_controllers.Clear();
            m_controllerListReadOnly.Clear();
            foreach (IFacialControllerHandle handle in GetComponentsInChildren<IFacialControllerHandle>()) {
                var h = new FacialControllerHandleListElem(handle);
                m_controllers.Add(h.name, h);
                m_controllerListReadOnly.Add(h);
            }
            ResizeHandle(((RectTransform)Canvas.FindAnyObjectByType<Canvas>().transform).rect.width);
        }

        private void OnRectTransformDimensionsChange() {
            // The RectTransform has changed!
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            ResizeHandle(((RectTransform)canvas.transform).rect.width);
        }

        void ResizeHandle(float width) {
            float actual_size = handleSize * width / 1920;
            foreach (var iter in m_controllers) {
                string name = iter.Key;
                FacialControllerHandleListElem handle = iter.Value;
                // Reshape the width to match the height.
                if (handle.is1D)
                {
                    var sliderGameObject = handle.handle1D.m_handleObject;
                    var fitter = sliderGameObject.GetComponentInChildren<AspectRatioFitter>();
                    var transformSlidingArea = sliderGameObject.transform.parent;
                    var transformSlider = transformSlidingArea.parent;
                    fitter.aspectRatio = 
                        (transformSlider.localScale.y / transformSlider.localScale.x)
                      * (transformSlidingArea.localScale.y / transformSlidingArea.localScale.x)
                        ;
                }
                Rect originalRect = handle.handleTransform.rect;
                handle.handleTransform.sizeDelta = new Vector2(actual_size / handle.objectScale, 0);
            }
        }

        // Update Head BS (and joints)
        public void ChangeValue(FacialControllerHandle1D handler) {
            switch (handler.name) {
                case "InnerBrowLeft":
                    UpdateHeadBlendShape("browInnerUp", Mathf.Max(0, handler.translateX));
                    UpdateHeadBlendShape("browDownLeft", Mathf.Max(
                        -m_controllers["OuterBrowLeft"].handle1D.translateX,
                        -m_controllers["InnerBrowLeft"].handle1D.translateX,
                        0
                    ));
                    break;
                case "InnerBrowRight":
                    UpdateHeadBlendShape("browInnerUp", Mathf.Max(0, handler.translateX));
                    UpdateHeadBlendShape("browDownRight", Mathf.Max(
                        -m_controllers["OuterBrowRight"].handle1D.translateX,
                        -m_controllers["InnerBrowRight"].handle1D.translateX,
                        0
                    ));
                    break;
                case "OuterBrowLeft":
                    UpdateHeadBlendShape("browOuterUpLeft", Mathf.Max(0, handler.translateX));
                    UpdateHeadBlendShape("browDownLeft", Mathf.Max(
                        -m_controllers["OuterBrowLeft"].handle1D.translateX,
                        -m_controllers["InnerBrowLeft"].handle1D.translateX,
                        0
                    ));
                    break;
                case "OuterBrowRight":
                    UpdateHeadBlendShape("browOuterUpRight", Mathf.Max(0, handler.translateX));
                    UpdateHeadBlendShape("browDownRight", Mathf.Max(
                        -m_controllers["OuterBrowRight"].handle1D.translateX,
                        -m_controllers["InnerBrowRight"].handle1D.translateX,
                        0
                    ));
                    break;
                case "CheekLeft":
                    UpdateHeadBlendShape("cheekPuff", handler.translateX);
                    break;
                case "CheekRight":
                    UpdateHeadBlendShape("cheekPuff", handler.translateX);
                    break;
                case "LowerEyelidLeft":
                    UpdateHeadBlendShape("cheekSquintLeft", handler.translateX);
                    break;
                case "LowerEyelidRight":
                    UpdateHeadBlendShape("cheekSquintRight", handler.translateX);
                    break;
                case "JawForward":
                    UpdateHeadBlendShape("jawForward", handler.translateX);
                    break;
                case "UpperMouthLip2Left":
                    UpdateHeadBlendShape("mouthUpperUpLeft", handler.translateX);
                    break;
                case "UpperMouthLip2Right":
                    UpdateHeadBlendShape("mouthUpperUpRight", handler.translateX);
                    break;
                case "LowerMouthLip2Left":
                    UpdateHeadBlendShape("mouthLowerDownLeft", handler.translateX);
                    break;
                case "LowerMouthLip2Right":
                    UpdateHeadBlendShape("mouthLowerDownRight", handler.translateX);
                    break;
                case "NoseWingsLeft":
                    UpdateHeadBlendShape("noseSneerLeft", handler.translateX);
                    break;
                case "NoseWingsRight":
                    UpdateHeadBlendShape("noseSneerRight", handler.translateX);
                    break;
                case "UpperEyeLidLeft":
                    var eyeLeftHandler = m_controllers["EyeLeft"].handle2D;
                    UpdateHeadBlendShape("eyeWideLeft", Mathf.Max(0, Mathf.Lerp(0, handler.WideLimit(), handler.translateX)));

                    UpdateHeadBlendShape("eyeLookDownLeft", Mathf.Max(0, -eyeLeftHandler.translateY));
                    float eyeBlinkLeftMax = handler.BlinkLimit() - Mathf.Max(0, -eyeLeftHandler.translateY);
                    UpdateHeadBlendShape("eyeBlinkLeft", Mathf.Lerp(0, eyeBlinkLeftMax, -handler.translateX));
                    break;
                case "UpperEyeLidRight":
                    var eyeRightHandler = m_controllers["EyeRight"].handle2D;
                    UpdateHeadBlendShape("eyeWideRight", Mathf.Max(0, Mathf.Lerp(0, handler.WideLimit(), handler.translateX)));
                    
                    UpdateHeadBlendShape("eyeLookDownRight", Mathf.Max(0, -eyeRightHandler.translateY));
                    float eyeBlinkRightMax = handler.BlinkLimit() - Mathf.Max(0, -eyeRightHandler.translateY);
                    UpdateHeadBlendShape("eyeBlinkRight", Mathf.Lerp(0, eyeBlinkRightMax, -handler.translateX));
                    break;
                case "MouthRollFunnel":
                    UpdateHeadBlendShape("mouthFunnel", Mathf.Max(0, handler.translateX));
                    UpdateHeadBlendShape("mouthRollUpper", Mathf.Max(0, -handler.translateX));
                    UpdateHeadBlendShape("mouthRollLower", Mathf.Max(0, -handler.translateX));
                    break;
                case "MouthClosePress":
                    var jawOpenHandler = m_controllers["Jaw"].handle2D;
                    UpdateHeadBlendShape("mouthClose",
                        Mathf.Clamp01(handler.translateX)
                      * jawOpenHandler.translateY
                    );
                    UpdateHeadBlendShape("mouthPressLeft", Mathf.Max(handler.translateX - 1, 0));
                    UpdateHeadBlendShape("mouthPressRight", Mathf.Max(handler.translateX - 1, 0));
                    break;
                case "MouthPucker":
                    UpdateHeadBlendShape("mouthPucker", handler.translateX);
                    break;
                    //UpdateHeadBlendShape("mouthClose", (handler.MouthClose() * 0.1f));
            }
        }
        public void ChangeValue(FacialControllerHandle2D handler) {
            switch (handler.name) {
                case "Mouth":
                    UpdateHeadBlendShape("mouthLeft", Mathf.Max(0, handler.translateX));
                    UpdateHeadBlendShape("mouthRight", Mathf.Max(0, -handler.translateX));
                    UpdateHeadBlendShape("mouthShrugUpper", Mathf.Max(0, -handler.translateY));
                    UpdateHeadBlendShape("mouthShrugLower", Mathf.Max(0, -handler.translateY));
                    break;
                case "Jaw":
                    UpdateHeadBlendShape("jawLeft", Mathf.Max(0, handler.translateX));
                    UpdateHeadBlendShape("jawRight", Mathf.Max(0, -handler.translateX));
                    UpdateHeadBlendShape("jawOpen", handler.translateY);

                    var mouthClosePressHandler = m_controllers["MouthClosePress"].handle1D;
                    UpdateHeadBlendShape("mouthClose",
                        Mathf.Clamp01(mouthClosePressHandler.translateX)
                      * handler.translateY
                    ); ;
                    break;
                case "MouthLeft":
                    //UpdateHeadBlendShape("mouthDimpleLeft", handler.Dimple() * 0.1f);
                    UpdateHeadBlendShape("mouthFrownLeft", Mathf.Max(0, -handler.translateY));
                    UpdateHeadBlendShape("mouthSmileLeft", Mathf.Max(0, handler.translateY));
                    UpdateHeadBlendShape("mouthStretchLeft", Mathf.Max(0, handler.translateX));
                    break;
                case "MouthRight":
                    //UpdateHeadBlendShape("mouthDimpleRight", handler.Dimple() * 0.1f);
                    UpdateHeadBlendShape("mouthFrownRight", Mathf.Max(0, -handler.translateY));
                    UpdateHeadBlendShape("mouthSmileRight", Mathf.Max(0, handler.translateY));
                    UpdateHeadBlendShape("mouthStretchRight", Mathf.Max(0, handler.translateX));
                    break;
                case "EyeLeft":
                    var eyeLidLeftHandler = m_controllers["UpperEyeLidLeft"].handle1D;
                    UpdateHeadBlendShape("eyeLookDownLeft", Mathf.Max(0, -handler.translateY));
                    float eyeBlinkLeftMax = eyeLidLeftHandler.BlinkLimit() - Mathf.Max(0, -handler.translateY);
                    UpdateHeadBlendShape("eyeBlinkLeft", Mathf.Lerp(0, eyeBlinkLeftMax, -eyeLidLeftHandler.translateX));

                    UpdateHeadBlendShape("eyeLookInLeft", Mathf.Max(0, -handler.translateX));
                    UpdateHeadBlendShape("eyeLookOutLeft", Mathf.Max(0, handler.translateX));
                    UpdateHeadBlendShape("eyeLookUpLeft", Mathf.Max(0, handler.translateY));
                    break;
                case "EyeRight":
                    var eyeLidRightHandler = m_controllers["UpperEyeLidRight"].handle1D;
                    UpdateHeadBlendShape("eyeLookDownRight", Mathf.Max(0, -handler.translateY));
                    float eyeBlinkRightMax = eyeLidRightHandler.BlinkLimit() - Mathf.Max(0, -handler.translateY);
                    UpdateHeadBlendShape("eyeBlinkRight", Mathf.Lerp(0, eyeBlinkRightMax, -eyeLidRightHandler.translateX));

                    UpdateHeadBlendShape("eyeLookOutRight", Mathf.Max(0, -handler.translateX));
                    UpdateHeadBlendShape("eyeLookInRight", Mathf.Max(0, handler.translateX));
                    UpdateHeadBlendShape("eyeLookUpRight", Mathf.Max(0, handler.translateY));
                    break;
            }
        }

        // Update from head to other parts.
        void UpdateHeadBlendShape(string headBlendShapeName, float value) {
            m_HeadGroup.SetBlendShapeWeight(headBlendShapeName, value * m_BlendShapeScale);
        }

        public void ResetPanel()
        {
            foreach (var iter in m_controllers)
            {
                var handle = iter.Value;
                if (handle.is1D)
                {
                    handle.handle1D.slider.value = 0;
                } else if (handle.is2D)
                {
                    handle.handle2D.slider.value = Vector2.zero;
                }
            }
            m_HeadGroup.ResetBlendShape();
        }
    }
}

