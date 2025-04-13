using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UnityEngine.UI {
    internal static class MultipleDisplayUtilities {
        /// <summary>
        /// Converts the current drag position into a relative position for the display.
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="position"></param>
        /// <returns>Returns true except when the drag operation is not on the same display as it originated</returns>
        public static bool GetRelativeMousePositionForDrag(PointerEventData eventData, ref Vector2 position) {
#if UNITY_EDITOR
            position = eventData.position;
#else
            int pressDisplayIndex = eventData.pointerPressRaycast.displayIndex;
            var relativePosition = Display.RelativeMouseAt(eventData.position);
            int currentDisplayIndex = (int)relativePosition.z;

            // Discard events on a different display.
            if (currentDisplayIndex != pressDisplayIndex)
                return false;

            // If we are not on the main display then we must use the relative position.
            position = pressDisplayIndex != 0 ? (Vector2)relativePosition : eventData.position;
#endif
            return true;
        }

        /// <summary>
        /// Adjusts the position when the main display has a different rendering resolution to the system resolution.
        /// By default, the mouse position is relative to the main render area, we need to adjust this so it is relative to the system resolution
        /// in order to correctly determine the position on other displays.
        /// </summary>
        /// <returns></returns>
        public static Vector2 GetMousePositionRelativeToMainDisplayResolution() {
            var position = Input.mousePosition;
#if !UNITY_EDITOR && !UNITY_WSA
            if (Display.main.renderingHeight != Display.main.systemHeight)
            {
                // The position is relative to the main render area, we need to adjust this so
                // it is relative to the system resolution in order to correctly determine the position on other displays.

                // Correct the y position if we are outside the main display.
                if ((position.y < 0 || position.y > Display.main.renderingHeight ||
                     position.x < 0 || position.x > Display.main.renderingWidth) && (Screen.fullScreenMode != FullScreenMode.Windowed))
                {
                    position.y += Display.main.systemHeight - Display.main.renderingHeight;
                }
            }
#endif
            return position;
        }
    }
}

namespace UnityEngine.UI {
    internal static class SetPropertyUtility {
        public static bool SetColor(ref Color currentValue, Color newValue) {
            if (currentValue.r == newValue.r && currentValue.g == newValue.g && currentValue.b == newValue.b && currentValue.a == newValue.a)
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct {
            if (currentValue.Equals(newValue))
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetClass<T>(ref T currentValue, T newValue) where T : class {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return false;

            currentValue = newValue;
            return true;
        }
    }
}

namespace UnityEngine.UI {
    [AddComponentMenu("UI/Slider2D", 50)]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// A standard slider that can be moved between a minimum and maximum value.
    /// </summary>
    /// <remarks>
    /// The slider component is a Selectable that controls a handle. The handle follow the current value.
    /// The anchors of the handle RectTransform is driven by the Slider. The handle can be direct children of the GameObject with the Slider2D, or intermediary RectTransforms can be placed in between for additional control.
    /// When a change to the slider value occurs, a callback is sent to any registered listeners of UI.Slider2D.onValueChanged.
    /// </remarks>
    public class Slider2D : Selectable, IDragHandler, IInitializePotentialDragHandler, ICanvasElement {

        [Serializable]
        /// <summary>
        /// Event type used by the UI.Slider.
        /// </summary>
        public class Slider2DEvent : UnityEvent<Vector2> {
        }

        [SerializeField]
        private RectTransform m_HandleRect;

        /// <summary>
        /// Optional RectTransform to use as a handle for the slider.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Slider mainSlider;
        ///     //Reference to new "RectTransform" (Child of "Handle Slide Area").
        ///     public RectTransform handleHighlighted;
        ///
        ///     //Deactivates the old Handle, then assigns and enables the new one.
        ///     void Start()
        ///     {
        ///         mainSlider.handleRect.gameObject.SetActive(false);
        ///         mainSlider.handleRect = handleHighlighted;
        ///         mainSlider.handleRect.gameObject.SetActive(true);
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public RectTransform handleRect {
            get {
                return m_HandleRect;
            }
            set {
                if (SetPropertyUtility.SetClass(ref m_HandleRect, value)) {
                    UpdateCachedReferences();
                    UpdateVisuals();
                }
            }
        }

        [Space]

        [SerializeField]
        private bool m_HorizontalReversed = false;
        [SerializeField]
        private bool m_VerticalReversed = false;
        public bool reverseValueX {
            get {
                return m_HorizontalReversed;
            }
            set {
                if (SetPropertyUtility.SetStruct(ref m_HorizontalReversed, value)) {
                    UpdateVisuals();
                }
            }
        }

        public bool reverseValueY {
            get {
                return m_VerticalReversed;
            }
            set {
                if (SetPropertyUtility.SetStruct(ref m_VerticalReversed, value)) {
                    UpdateVisuals();
                }
            }
        }

        [SerializeField]
        private float m_MinValueX = 0;
        /// <summary>
        /// The minimum allowed value of the slider on X axis.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Slider mainSlider;
        ///
        ///     void Start()
        ///     {
        ///         // Changes the minimum value of the slider to 10;
        ///         mainSlider.minValueX = 10;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public float minValueX {
            get {
                return m_MinValueX;
            }
            set {
                if (SetPropertyUtility.SetStruct(ref m_MinValueX, value)) {
                    Set(m_Value);
                    UpdateVisuals();
                }
            }
        }

        [SerializeField]
        private float m_MinValueY = 0;
        /// <summary>
        /// The minimum allowed value of the slider on X axis.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Slider mainSlider;
        ///
        ///     void Start()
        ///     {
        ///         // Changes the minimum value of the slider to 10;
        ///         mainSlider.minValueX = 10;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public float minValueY {
            get {
                return m_MinValueY;
            }
            set {
                if (SetPropertyUtility.SetStruct(ref m_MinValueY, value)) {
                    Set(m_Value);
                    UpdateVisuals();
                }
            }
        }

        [SerializeField]
        private float m_MaxValueX = 1;
        /// <summary>
        /// The maximum allowed value of the slider.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Slider mainSlider;
        ///
        ///     void Start()
        ///     {
        ///         // Changes the max value of the slider to 20;
        ///         mainSlider.maxValue = 20;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public float maxValueX {
            get {
                return m_MaxValueX;
            }
            set {
                if (SetPropertyUtility.SetStruct(ref m_MaxValueX, value)) {
                    Set(m_Value);
                    UpdateVisuals();
                }
            }
        }

        [SerializeField]
        private float m_MaxValueY = 1;
        /// <summary>
        /// The maximum allowed value of the slider.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Slider mainSlider;
        ///
        ///     void Start()
        ///     {
        ///         // Changes the max value of the slider to 20;
        ///         mainSlider.maxValue = 20;
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public float maxValueY {
            get {
                return m_MaxValueY;
            }
            set {
                if (SetPropertyUtility.SetStruct(ref m_MaxValueY, value)) {
                    Set(m_Value);
                    UpdateVisuals();
                }
            }
        }

        [SerializeField]
        protected Vector2 m_Value;

        /// <summary>
        /// The current value of the slider.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Slider mainSlider;
        ///
        ///     //Invoked when a submit button is clicked.
        ///     public void SubmitSliderSetting()
        ///     {
        ///         //Displays the value of the slider in the console.
        ///         Debug.Log(mainSlider.value);
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public virtual Vector2 value {
            get {
                return m_Value;
            }
            set {
                Set(value);
            }
        }

        /// <summary>
        /// Set the value of the slider X without invoking onValueChanged callback.
        /// </summary>
        /// <param name="input">The new value for the slider.</param>
        public virtual void SetValueWithoutNotify(Vector2 input) {
            Set(input, false);
        }

        /// <summary>
        /// The current value of the slider normalized into a value between 0 and 1.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Slider mainSlider;
        ///
        ///     //Set to invoke when "OnValueChanged" method is called.
        ///     void CheckNormalisedValue()
        ///     {
        ///         //Displays the normalised value of the slider everytime the value changes.
        ///         Debug.Log(mainSlider.normalizedValue);
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Vector2 normalizedValue {
            get {
                float x, y;
                if (Mathf.Approximately(minValueX, maxValueX))
                    x = 0;
                else
                    x = Mathf.InverseLerp(minValueX, maxValueX, value.x);
                if (Mathf.Approximately(minValueY, maxValueY))
                    y = 0;
                else
                    y = Mathf.InverseLerp(minValueY, maxValueY, value.y);
                return new Vector2(x, y);
            }
            set {
                this.value = new Vector2(
                    Mathf.Lerp(minValueX, maxValueX, value.x),
                    Mathf.Lerp(minValueY, maxValueY, value.y)
                );
            }
        }

        [Space]

        [SerializeField]
        private Slider2DEvent m_OnValueChanged = new Slider2DEvent();

        /// <summary>
        /// Callback executed when the value of the slider is changed.
        /// </summary>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// using UnityEngine;
        /// using System.Collections;
        /// using UnityEngine.UI; // Required when Using UI elements.
        ///
        /// public class Example : MonoBehaviour
        /// {
        ///     public Slider mainSlider;
        ///
        ///     public void Start()
        ///     {
        ///         //Adds a listener to the main slider and invokes a method when the value changes.
        ///         mainSlider.onValueChanged.AddListener(delegate {ValueChangeCheck(); });
        ///     }
        ///
        ///     // Invoked when the value of the slider changes.
        ///     public void ValueChangeCheck()
        ///     {
        ///         Debug.Log(mainSlider.value);
        ///     }
        /// }
        /// ]]>
        ///</code>
        /// </example>
        public Slider2DEvent onValueChanged {
            get {
                return m_OnValueChanged;
            }
            set {
                m_OnValueChanged = value;
            }
        }

        // Private fields

        private Transform m_HandleTransform;
        private RectTransform m_HandleContainerRect;

        // The offset from handle position to mouse down position
        private Vector2 m_Offset = Vector2.zero;

        // field is never assigned warning
#pragma warning disable 649
        private DrivenRectTransformTracker m_Tracker;
#pragma warning restore 649

        // This "delayed" mechanism is required for case 1037681.
        private bool m_DelayedUpdateVisuals = false;

        // Size of each step.
        float stepSizeX {
            get {
                return (maxValueX - minValueX) * 0.1f;
            }
        }
        float stepSizeY {
            get {
                return (maxValueY - minValueY) * 0.1f;
            }
        }

        protected Slider2D() {
        }

#if UNITY_EDITOR
        protected override void OnValidate() {
            base.OnValidate();

            //Onvalidate is called before OnEnabled. We need to make sure not to touch any other objects before OnEnable is run.
            if (IsActive()) {
                UpdateCachedReferences();
                // Update rects in next update since other things might affect them even if value didn't change.
                m_DelayedUpdateVisuals = true;
            }

            if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this) && !Application.isPlaying)
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }

#endif // if UNITY_EDITOR

        public virtual void Rebuild(CanvasUpdate executing) {
#if UNITY_EDITOR
            if (executing == CanvasUpdate.Prelayout)
                onValueChanged.Invoke(value);
#endif
        }

        /// <summary>
        /// See ICanvasElement.LayoutComplete
        /// </summary>
        public virtual void LayoutComplete() {
        }

        /// <summary>
        /// See ICanvasElement.GraphicUpdateComplete
        /// </summary>
        public virtual void GraphicUpdateComplete() {
        }

        protected override void OnEnable() {
            base.OnEnable();
            UpdateCachedReferences();
            Set(m_Value, false);
            // Update rects since they need to be initialized correctly.
            UpdateVisuals();
        }

        protected override void OnDisable() {
            m_Tracker.Clear();
            base.OnDisable();
        }

        /// <summary>
        /// Update the rect based on the delayed update visuals.
        /// Got around issue of calling sendMessage from onValidate.
        /// </summary>
        protected virtual void Update() {
            if (m_DelayedUpdateVisuals) {
                m_DelayedUpdateVisuals = false;
                Set(m_Value, false);
                UpdateVisuals();
            }
        }

        protected override void OnDidApplyAnimationProperties() {
            // Has value changed? Various elements of the slider have the old normalisedValue assigned, we can use this to perform a comparison.
            // We also need to ensure the value stays within min/max.
            m_Value = ClampValue(m_Value);
            float oldNormalizedValueX = normalizedValue.x;
            float oldNormalizedValueY = normalizedValue.y;
            if (m_HandleContainerRect != null) {
                oldNormalizedValueX = (reverseValueX ? 1 - m_HandleRect.anchorMin.x : m_HandleRect.anchorMin.x);
                oldNormalizedValueY = (reverseValueY ? 1 - m_HandleRect.anchorMin.y : m_HandleRect.anchorMin.y);
            }

            UpdateVisuals();

            if (oldNormalizedValueX != normalizedValue.x || oldNormalizedValueY != normalizedValue.y) {
                UISystemProfilerApi.AddMarker("Slider2D.value", this);
                onValueChanged.Invoke(m_Value);
            }
        }

        void UpdateCachedReferences() {
            if (m_HandleRect && m_HandleRect != (RectTransform)transform) {
                m_HandleTransform = m_HandleRect.transform;
                if (m_HandleTransform.parent != null)
                    m_HandleContainerRect = m_HandleTransform.parent.GetComponent<RectTransform>();
            }
            else {
                m_HandleRect = null;
                m_HandleContainerRect = null;
            }
        }

        Vector2 ClampValue(Vector2 input) {
            float newValueX = Mathf.Clamp(input.x, minValueX, maxValueX);
            float newValueY = Mathf.Clamp(input.y, minValueY, maxValueY);
            return new Vector2(newValueX, newValueY);
        }

        /// <summary>
        /// Set the value of the slider.
        /// </summary>
        /// <param name="input">The new value for the slider.</param>
        /// <param name="sendCallback">If the OnValueChanged callback should be invoked.</param>
        /// <remarks>
        /// Process the input to ensure the value is between min and max value. If the input is different set the value and send the callback is required.
        /// </remarks>
        protected virtual void Set(Vector2 input, bool sendCallback = true) {
            // Clamp the input
            Vector2 newValue = ClampValue(input);

            // If the stepped value doesn't match the last one, it's time to update
            if (m_Value == newValue)
                return;

            m_Value = newValue;
            UpdateVisuals();
            if (sendCallback) {
                UISystemProfilerApi.AddMarker("Slider2D.value", this);
                m_OnValueChanged.Invoke(newValue);
            }
        }

        protected override void OnRectTransformDimensionsChange() {
            base.OnRectTransformDimensionsChange();

            //This can be invoked before OnEnabled is called. So we shouldn't be accessing other objects, before OnEnable is called.
            if (!IsActive())
                return;

            UpdateVisuals();
        }

        // Force-update the slider. Useful if you've changed the properties and want it to update visually.
        private void UpdateVisuals() {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateCachedReferences();
#endif

            m_Tracker.Clear();

            if (m_HandleContainerRect != null) {
                m_Tracker.Add(this, m_HandleRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;
                anchorMin.x = anchorMax.x = (reverseValueX ? (1 - normalizedValue.x) : normalizedValue.x);
                anchorMin.y = anchorMax.y = (reverseValueY ? (1 - normalizedValue.y) : normalizedValue.y);
                m_HandleRect.anchorMin = anchorMin;
                m_HandleRect.anchorMax = anchorMax;
            }
        }

        // Update the slider's position based on the mouse.
        void UpdateDrag(PointerEventData eventData, Camera cam) {
            RectTransform clickRect = m_HandleContainerRect;
            if (clickRect != null && clickRect.rect.size.x > 0 && clickRect.rect.size.y > 0) {
                Vector2 position = Vector2.zero;
                if (!MultipleDisplayUtilities.GetRelativeMousePositionForDrag(eventData, ref position))
                    return;

                Vector2 localCursor;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(clickRect, position, cam, out localCursor))
                    return;

                localCursor -= clickRect.rect.position;

                float valX = Mathf.Clamp01((localCursor - m_Offset).x / clickRect.rect.size.x);
                float valY = Mathf.Clamp01((localCursor - m_Offset).y / clickRect.rect.size.y);

                normalizedValue = new Vector2((reverseValueX ? 1f - valX : valX), (reverseValueY ? 1f - valY : valY));
            }
        }

        private bool MayDrag(PointerEventData eventData) {
            return IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left;
        }

        public override void OnPointerDown(PointerEventData eventData) {
            if (!MayDrag(eventData))
                return;

            base.OnPointerDown(eventData);

            m_Offset = Vector2.zero;
            if (m_HandleContainerRect != null && RectTransformUtility.RectangleContainsScreenPoint(m_HandleRect, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera)) {
                Vector2 localMousePos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_HandleRect, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out localMousePos))
                    m_Offset = localMousePos;
            }
            else {
                // Outside the slider handle - jump to this point instead
                UpdateDrag(eventData, eventData.pressEventCamera);
            }
        }

        public virtual void OnDrag(PointerEventData eventData) {
            if (!MayDrag(eventData))
                return;
            UpdateDrag(eventData, eventData.pressEventCamera);
        }

        public override void OnMove(AxisEventData eventData) {
            if (!IsActive() || !IsInteractable()) {
                base.OnMove(eventData);
                return;
            }

            Vector2 moveDelta = Vector2.zero;

            switch (eventData.moveDir) {
                case MoveDirection.Left:
                    moveDelta.x += stepSizeX;
                    break;
                case MoveDirection.Right:
                    moveDelta.x -=stepSizeX;
                    break;
                case MoveDirection.Up:
                    moveDelta.y -= stepSizeY;
                    break;
                case MoveDirection.Down:
                    moveDelta.y += stepSizeY;
                    break;
            }
            Set(value + moveDelta);
        }

        /// <summary>
        /// See Selectable.FindSelectableOnLeft
        /// </summary>
        public override Selectable FindSelectableOnLeft() {
            return null;
        }

        /// <summary>
        /// See Selectable.FindSelectableOnRight
        /// </summary>
        public override Selectable FindSelectableOnRight() {
            return null;
        }

        /// <summary>
        /// See Selectable.FindSelectableOnUp
        /// </summary>
        public override Selectable FindSelectableOnUp() {
            return null;
        }

        /// <summary>
        /// See Selectable.FindSelectableOnDown
        /// </summary>
        public override Selectable FindSelectableOnDown() {
            return null;
        }

        public virtual void OnInitializePotentialDrag(PointerEventData eventData) {
            eventData.useDragThreshold = false;
        }
    }
}
