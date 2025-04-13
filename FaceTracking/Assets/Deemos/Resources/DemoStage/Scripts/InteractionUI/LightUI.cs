using UnityEngine;
using UnityEngine.UI;

namespace InteractionUI
{ 
    [RequireComponent(typeof(InteractionUIScripts))]
    public class LightUI : MonoBehaviour
    {
        [SerializeField] Button m_buttonPreviousLighting;
        [SerializeField] Button m_buttonNextLighting;
        [SerializeField] Transform m_lightingSetupParent;
        int activeLightingSetupIndex;
        InteractionUIScripts m_UIHost;

        //float operationCooldown = 0f;

        // Start is called before the first frame update
        void Start()
        {
            m_UIHost = GetComponent<InteractionUIScripts>();
            m_buttonPreviousLighting.onClick.AddListener(HandleLightingPrevious);
            m_buttonNextLighting.onClick.AddListener(HandleLightingNext);
            ActivateLighting(0);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.RightBracket)) // Switch light
            {
                HandleLightingNext();
            }
            if (Input.GetKeyDown(KeyCode.LeftBracket))
            {
                HandleLightingPrevious();
            }
        }

        void HandleLightingPrevious()
        {
            ActivateLighting(activeLightingSetupIndex - 1);
        }

        void HandleLightingNext()
        {
            ActivateLighting(activeLightingSetupIndex + 1);
        }

        void ActivateLighting(int index)
        {
            m_lightingSetupParent.GetChild(activeLightingSetupIndex).gameObject.SetActive(false);
            activeLightingSetupIndex = (index + m_lightingSetupParent.childCount) % m_lightingSetupParent.childCount;

            var newLighting = m_lightingSetupParent.GetChild(activeLightingSetupIndex);
            newLighting.gameObject.SetActive(true);
        }
    }
}