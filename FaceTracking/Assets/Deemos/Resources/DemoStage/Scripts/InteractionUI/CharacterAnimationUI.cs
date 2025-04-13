using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InteractionUI
{
    public enum CharacterState
    {
        Pose,
        Walk
    }

    [RequireComponent(typeof(InteractionUIScripts))]
    public class CharacterAnimationUI : MonoBehaviour
    {
        [Space]
        [Header("Character Animation")]
        [SerializeField] ToggleGroup m_AnimationOptions;
        [SerializeField] Dictionary<string, Toggle> m_AnimationToggles;
        [SerializeField] CharacterState m_CurrentState;
        [SerializeField] Animator m_CharacterAnimator;
        InteractionUIScripts m_UIHost;

        [SerializeField] Toggle m_toggleWalk, m_togglePose;

        // Start is called before the first frame update
        void Start()
        {
            m_UIHost = GetComponent<InteractionUIScripts>();
            m_AnimationToggles = InteractionUIScripts.GetToggles(m_AnimationOptions);

            if (m_CharacterAnimator == null
                || !m_AnimationToggles.TryGetValue("Walk", out m_toggleWalk)
                || !m_AnimationToggles.TryGetValue("Pose", out m_togglePose))
            {
                enabled = false;
                return;
            }

            // Initialization
            m_CurrentState = CharacterState.Pose;

            // Character Action Listener
            m_toggleWalk.onValueChanged.AddListener((x) => m_CharacterAnimator.SetBool("Walk", x));
            m_toggleWalk.onValueChanged.AddListener((x) => m_CurrentState = x ? CharacterState.Walk : CharacterState.Pose);

            // Toggle state and character animator state
            m_CharacterAnimator.SetBool("Walk", false);
            m_togglePose.isOn = true;
        }

        public void SetState(CharacterState newState)
        {
            if (newState == m_CurrentState)
            {
                return;
            }
            switch (newState)
            {
                case CharacterState.Pose:
                    m_togglePose.isOn = true;
                    m_toggleWalk.isOn = false;
                    m_CharacterAnimator.SetBool("Walk", false);
                    break;
                case CharacterState.Walk:
                    m_toggleWalk.isOn = true;
                    m_togglePose.isOn = false;
                    m_CharacterAnimator.SetBool("Walk", true);
                    break;
            }
            m_CurrentState = newState;
        }
    }
}