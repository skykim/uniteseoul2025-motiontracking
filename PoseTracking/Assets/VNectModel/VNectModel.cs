using UnityEngine;

//Reference: https://github.com/digital-standard/ThreeDPoseUnityBarracuda
public enum PositionIndex : int
{
    rShldrBend = 0, rForearmBend, rHand, rThumb2, rMid1, lShldrBend, lForearmBend,
    lHand, lThumb2, lMid1, lEar, lEye, rEar, rEye, Nose, rThighBend, rShin,
    rFoot, rToe, lThighBend, lShin, lFoot, lToe, abdomenUpper,
    //Calculated coordinates
    hip, head, neck, spine,
    Count, None
}

public static partial class EnumExtend
{
    public static int Int(this PositionIndex i)
    {
        return (int)i;
    }
}

public class VNectModel : MonoBehaviour
{
    public class JointPoint
    {
        public Vector2 Pos2D = new Vector2();
        public float score2D;

        public Vector3 Pos3D = new Vector3();
        public Vector3 Now3D = new Vector3();
        public Vector3[] PrevPos3D = new Vector3[6];

        // Bones
        public Transform Transform = null;
        public Quaternion InitRotation;
        public Quaternion InverseRotation;

        public JointPoint Child = null;

        // For Kalman filter
        public Vector3 P = new Vector3();
        public Vector3 X = new Vector3();
        public Vector3 K = new Vector3();
    }

    // Joint position and bone
    private JointPoint[] jointPoints;
    public JointPoint[] JointPoints { get { return jointPoints; } }

    private Vector3 initPosition; // Initial center position

    // Model
    public GameObject ModelObject;
    public GameObject Nose;
    private Animator anim;

    // Move in z direction
    private float centerTall = 256 * 0.75f;
    private float tall = 256 * 0.75f;
    private float prevTall = 256 * 0.75f;
    public float ZScale = 0.8f;

    private void Update()
    {
        if (jointPoints != null)
        {
            PoseUpdate();
        }
    }

    public JointPoint[] Init()
    {
        jointPoints = new JointPoint[PositionIndex.Count.Int()];
        for (int i = 0; i < jointPoints.Length; i++)
            jointPoints[i] = new JointPoint();

        anim = ModelObject.GetComponent<Animator>();

        // Arms
        MapJoint(PositionIndex.rShldrBend, HumanBodyBones.RightUpperArm);
        MapJoint(PositionIndex.rForearmBend, HumanBodyBones.RightLowerArm);
        MapJoint(PositionIndex.rHand, HumanBodyBones.RightHand);
        MapJoint(PositionIndex.rThumb2, HumanBodyBones.RightThumbIntermediate);
        MapJoint(PositionIndex.rMid1, HumanBodyBones.RightMiddleProximal);

        MapJoint(PositionIndex.lShldrBend, HumanBodyBones.LeftUpperArm);
        MapJoint(PositionIndex.lForearmBend, HumanBodyBones.LeftLowerArm);
        MapJoint(PositionIndex.lHand, HumanBodyBones.LeftHand);
        MapJoint(PositionIndex.lThumb2, HumanBodyBones.LeftThumbIntermediate);
        MapJoint(PositionIndex.lMid1, HumanBodyBones.LeftMiddleProximal);

        // Face
        MapJoint(PositionIndex.lEar, HumanBodyBones.Head);
        MapJoint(PositionIndex.rEar, HumanBodyBones.Head);
        MapJoint(PositionIndex.lEye, HumanBodyBones.LeftEye);
        MapJoint(PositionIndex.rEye, HumanBodyBones.RightEye);
        MapJoint(PositionIndex.Nose, Nose);

        // Legs
        MapJoint(PositionIndex.rThighBend, HumanBodyBones.RightUpperLeg);
        MapJoint(PositionIndex.rShin, HumanBodyBones.RightLowerLeg);
        MapJoint(PositionIndex.rFoot, HumanBodyBones.RightFoot);
        MapJoint(PositionIndex.rToe, HumanBodyBones.RightToes);

        MapJoint(PositionIndex.lThighBend, HumanBodyBones.LeftUpperLeg);
        MapJoint(PositionIndex.lShin, HumanBodyBones.LeftLowerLeg);
        MapJoint(PositionIndex.lFoot, HumanBodyBones.LeftFoot);
        MapJoint(PositionIndex.lToe, HumanBodyBones.LeftToes);

        // Spine & torso
        MapJoint(PositionIndex.abdomenUpper, HumanBodyBones.Spine);
        MapJoint(PositionIndex.spine, HumanBodyBones.Spine);
        MapJoint(PositionIndex.neck, HumanBodyBones.Neck);
        MapJoint(PositionIndex.head, HumanBodyBones.Head);
        MapJoint(PositionIndex.hip, HumanBodyBones.Hips);

        //Set Child
        SetChild(PositionIndex.rShldrBend, PositionIndex.rForearmBend);
        SetChild(PositionIndex.rForearmBend, PositionIndex.rHand);
        SetChild(PositionIndex.lShldrBend, PositionIndex.lForearmBend);
        SetChild(PositionIndex.lForearmBend, PositionIndex.lHand);
        SetChild(PositionIndex.rThighBend, PositionIndex.rShin);
        SetChild(PositionIndex.rShin, PositionIndex.rFoot);
        SetChild(PositionIndex.rFoot, PositionIndex.rToe);
        SetChild(PositionIndex.lThighBend, PositionIndex.lShin);
        SetChild(PositionIndex.lShin, PositionIndex.lFoot);
        SetChild(PositionIndex.lFoot, PositionIndex.lToe);
        SetChild(PositionIndex.spine, PositionIndex.neck);
        SetChild(PositionIndex.neck, PositionIndex.head);

        var forward = TriangleNormal(
            jointPoints[PositionIndex.hip.Int()].Transform.position,
            jointPoints[PositionIndex.lThighBend.Int()].Transform.position,
            jointPoints[PositionIndex.rThighBend.Int()].Transform.position);

        foreach (var jp in jointPoints)
        {
            if (jp.Transform != null)
                jp.InitRotation = jp.Transform.rotation;

            if (jp.Child != null)
                jp.InverseRotation = GetInverse(jp, jp.Child, forward) * jp.InitRotation;
        }

        var hip = jointPoints[PositionIndex.hip.Int()];
        initPosition = hip.Transform.position;
        hip.InverseRotation = Quaternion.Inverse(Quaternion.LookRotation(forward)) * hip.InitRotation;

        var head = jointPoints[PositionIndex.head.Int()];
        var gaze = jointPoints[PositionIndex.Nose.Int()].Transform.position - head.Transform.position;
        head.InverseRotation = Quaternion.Inverse(Quaternion.LookRotation(gaze)) * head.InitRotation;

        var lHand = jointPoints[PositionIndex.lHand.Int()];
        var lf = TriangleNormal(
            lHand.Pos3D,
            jointPoints[PositionIndex.lMid1.Int()].Pos3D,
            jointPoints[PositionIndex.lThumb2.Int()].Pos3D);
        lHand.InverseRotation = GetInverse(jointPoints[PositionIndex.lThumb2.Int()], jointPoints[PositionIndex.lMid1.Int()], lf) * lHand.Transform.rotation;

        var rHand = jointPoints[PositionIndex.rHand.Int()];
        var rf = TriangleNormal(
            rHand.Pos3D,
            jointPoints[PositionIndex.rThumb2.Int()].Pos3D,
            jointPoints[PositionIndex.rMid1.Int()].Pos3D);
        rHand.InverseRotation = GetInverse(jointPoints[PositionIndex.rThumb2.Int()], jointPoints[PositionIndex.rMid1.Int()], rf) * rHand.Transform.rotation;

        return jointPoints;
    }


    private void MapJoint(PositionIndex index, HumanBodyBones bone)
    {
        jointPoints[index.Int()].Transform = anim.GetBoneTransform(bone);
    }

    private void MapJoint(PositionIndex index, GameObject target)
    {
        jointPoints[index.Int()].Transform = target.transform;
    }

    private void SetChild(PositionIndex parent, PositionIndex child)
    {
        jointPoints[parent.Int()].Child = jointPoints[child.Int()];
    }    

    public void PoseUpdate()
    {
        float FastDistance(Vector3 a, Vector3 b) => (a - b).magnitude;

        var t1 = FastDistance(jointPoints[PositionIndex.head.Int()].Pos3D, jointPoints[PositionIndex.neck.Int()].Pos3D);
        var t2 = FastDistance(jointPoints[PositionIndex.neck.Int()].Pos3D, jointPoints[PositionIndex.spine.Int()].Pos3D);
        var pm = (jointPoints[PositionIndex.rThighBend.Int()].Pos3D + jointPoints[PositionIndex.lThighBend.Int()].Pos3D) / 2f;
        var t3 = FastDistance(jointPoints[PositionIndex.spine.Int()].Pos3D, pm);
        var t4r = FastDistance(jointPoints[PositionIndex.rThighBend.Int()].Pos3D, jointPoints[PositionIndex.rShin.Int()].Pos3D);
        var t4l = FastDistance(jointPoints[PositionIndex.lThighBend.Int()].Pos3D, jointPoints[PositionIndex.lShin.Int()].Pos3D);
        var t4 = (t4r + t4l) / 2f;
        var t5r = FastDistance(jointPoints[PositionIndex.rShin.Int()].Pos3D, jointPoints[PositionIndex.rFoot.Int()].Pos3D);
        var t5l = FastDistance(jointPoints[PositionIndex.lShin.Int()].Pos3D, jointPoints[PositionIndex.lFoot.Int()].Pos3D);
        var t5 = (t5r + t5l) / 2f;
        var t = t1 + t2 + t3 + t4 + t5;

        float lpf_coeff = 0.1f;

        tall = t * (1-lpf_coeff) + prevTall * lpf_coeff;
        prevTall = tall;
        if (tall == 0) tall = centerTall;
        var dz = (centerTall - tall) / centerTall * ZScale;

        var forward = TriangleNormal(
            jointPoints[PositionIndex.hip.Int()].Pos3D,
            jointPoints[PositionIndex.lThighBend.Int()].Pos3D,
            jointPoints[PositionIndex.rThighBend.Int()].Pos3D);

        jointPoints[PositionIndex.hip.Int()].Transform.position =
            jointPoints[PositionIndex.hip.Int()].Pos3D * 0.005f +
            new Vector3(initPosition.x, initPosition.y, initPosition.z + dz);

        jointPoints[PositionIndex.hip.Int()].Transform.rotation =
            Quaternion.LookRotation(forward) * jointPoints[PositionIndex.hip.Int()].InverseRotation;

        foreach (var jointPoint in jointPoints)
        {
            if (jointPoint.Child != null)
            {
                jointPoint.Transform.rotation =
                    Quaternion.LookRotation(jointPoint.Pos3D - jointPoint.Child.Pos3D, forward) *
                    jointPoint.InverseRotation;
            }
        }

        var gaze = jointPoints[PositionIndex.Nose.Int()].Pos3D - jointPoints[PositionIndex.head.Int()].Pos3D;
        var f = TriangleNormal(
            jointPoints[PositionIndex.Nose.Int()].Pos3D,
            jointPoints[PositionIndex.rEar.Int()].Pos3D,
            jointPoints[PositionIndex.lEar.Int()].Pos3D);

        jointPoints[PositionIndex.head.Int()].Transform.rotation =
            Quaternion.LookRotation(gaze, f) * jointPoints[PositionIndex.head.Int()].InverseRotation;

        var lHand = jointPoints[PositionIndex.lHand.Int()];
        var lf = TriangleNormal(lHand.Pos3D,
            jointPoints[PositionIndex.lMid1.Int()].Pos3D,
            jointPoints[PositionIndex.lThumb2.Int()].Pos3D);

        lHand.Transform.rotation = Quaternion.LookRotation(
            jointPoints[PositionIndex.lThumb2.Int()].Pos3D - jointPoints[PositionIndex.lMid1.Int()].Pos3D, lf) *
            lHand.InverseRotation;

        var rHand = jointPoints[PositionIndex.rHand.Int()];
        var rf = TriangleNormal(rHand.Pos3D,
            jointPoints[PositionIndex.rThumb2.Int()].Pos3D,
            jointPoints[PositionIndex.rMid1.Int()].Pos3D);

        rHand.Transform.rotation = Quaternion.LookRotation(
            jointPoints[PositionIndex.rThumb2.Int()].Pos3D - jointPoints[PositionIndex.rMid1.Int()].Pos3D, rf) *
            rHand.InverseRotation;
    }


    Vector3 TriangleNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 d1 = a - b;
        Vector3 d2 = a - c;

        Vector3 normal = Vector3.Cross(d1, d2);
        float mag = normal.sqrMagnitude;

        if (mag < 1e-6f)
            return Vector3.up;

        return normal.normalized;
    }

    private Quaternion GetInverse(JointPoint p1, JointPoint p2, Vector3 forward)
    {
        return Quaternion.Inverse(Quaternion.LookRotation(p1.Transform.position - p2.Transform.position, forward));
    }
}