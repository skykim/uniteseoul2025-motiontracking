using UnityEngine;

namespace RhythMo
{
    public class Retarget : MonoBehaviour
    {
        [SerializeField]
        private bool autoStart;
        
        [SerializeField]
        private Animator sourceAnimator;

        private Animator targetAnimator;

        private HumanPoseHandler sourceHandler;
        private HumanPoseHandler targetHandler;
        private HumanPose humanPose;

        public bool Started { get; private set; }

        private void Start()
        {
            if (!this.autoStart) return;
            StartRetarget();
        }

        public void StartRetarget()
        {
            this.targetAnimator = GetComponentInChildren<Animator>();
            this.sourceHandler = new HumanPoseHandler(this.sourceAnimator.avatar, this.sourceAnimator.transform);
            this.targetHandler = new HumanPoseHandler(this.targetAnimator.avatar, this.targetAnimator.transform);
            this.sourceHandler.GetHumanPose(ref this.humanPose);
            this.targetHandler.SetHumanPose(ref this.humanPose);
            Started = true;
        }
        
        public void StopRetarget()
        {
            this.sourceHandler = null;
            this.targetHandler = null;
            Started = false;
        }

        private void LateUpdate()
        {
            if (this.sourceHandler == null || this.targetHandler == null) return;
            this.sourceHandler.GetHumanPose(ref this.humanPose);
            this.targetHandler.SetHumanPose(ref this.humanPose);

            // Manual
            targetAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder).Rotate(new Vector3(0.0f, 0.0f, 18.0f), Space.Self);
            targetAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm).Rotate(new Vector3(0.0f, 0.0f, -18.0f), Space.Self);
            targetAnimator.GetBoneTransform(HumanBodyBones.RightShoulder).Rotate(new Vector3(0.0f, 0.0f, -18.0f), Space.Self);
            targetAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm).Rotate(new Vector3(0.0f, 0.0f, 18.0f), Space.Self);
        }
    }
}