using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InteractionUI
{
    public class InteractionUIScripts : MonoBehaviour
    {
        // Ignore mouse drag from GUI
        [SerializeField] EventSystem eventSystem;
        [SerializeField] GraphicRaycaster graphicRaycaster;
        public bool CheckGuiRaycastObjects()
        {
            PointerEventData eventData = new PointerEventData(eventSystem);
            eventData.pressPosition = Input.mousePosition;
            eventData.position = Input.mousePosition;

            List<RaycastResult> list = new List<RaycastResult>();
            graphicRaycaster.Raycast(eventData, list);
            return list.Count > 0;
        }

        void Start()
        {
            if (eventSystem == null)
            {
                eventSystem = FindAnyObjectByType<EventSystem>();
            }
            if (graphicRaycaster == null)
            {
                graphicRaycaster = FindAnyObjectByType<GraphicRaycaster>();
            }

            if (eventSystem == null || graphicRaycaster == null)
            {
                enabled = false;
            }
        }

        public static Dictionary<string, Toggle> GetToggles(ToggleGroup toggleGroup)
        {
            Dictionary<string, Toggle> result = new Dictionary<string, Toggle>();
            GameObject toggleGroupGameObject = toggleGroup.gameObject;
            foreach (Toggle toggle in toggleGroup.GetComponentsInChildren<Toggle>())
            {
                if (toggle.group == toggleGroup)
                {
                    string toggleName = toggle.gameObject.GetComponentInChildren<Text>().text;
                    result.Add(toggleName, toggle);
                }
            }
            return result;
        }

    }

}
