using UnityEngine;
using UnityEngine.Animations.Rigging;

public class GeistLookAt : MonoBehaviour
{
    [Header("Vision Cone")]
    public Transform coneOrigin;
    public Transform target;
    [Range(1f, 180f)] public float coneHalfAngle = 60f;
    public float detectionRange = 10f;

    [Header("IK Target")]
    [SerializeField] private Transform ikTarget;
    [SerializeField] private ChainIKConstraint ikConstraint;
    public Transform ikRestAnchor;
    public float neckBendOffset = 0.3f;
    public float ikFollowSpeed = 5f;
    public Vector3 lookRotationOffset = new Vector3(-90f, 0f, 180f);

    [Header("Center Deadzone")]
    public float centerDeadzoneRadius = 0.1f;
    public Vector3 defaultLateralDirection = Vector3.down;

    public bool TargetVisible { get; private set; }

    private Vector3 lastLateral;
    private Vector3 ikVelocity;

    private void Start()
    {
        target = FindFirstObjectByType<ZeroGravity>().transform;
        lastLateral = defaultLateralDirection.normalized;
    }

    private void LateUpdate()
    {
        TargetVisible = CheckVisibility();
        ikConstraint.weight = TargetVisible ? 1f : 0f;
        SetIKTargetPosition();
    }

    private bool CheckVisibility()
    {
        if (target == null || coneOrigin == null) return false;
        Vector3 toTarget = target.position - coneOrigin.position;
        return toTarget.magnitude <= detectionRange &&
               Vector3.Angle(coneOrigin.up, toTarget) <= coneHalfAngle;
    }

    private void SetIKTargetPosition()
    {
        if (ikTarget == null || ikRestAnchor == null) return;

        if (!TargetVisible)
        {
            ikTarget.position = Vector3.SmoothDamp(ikTarget.position, ikRestAnchor.position, ref ikVelocity, 1f / ikFollowSpeed);
            ikTarget.rotation = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(lookRotationOffset);
            return;
        }

        Vector3 toTarget = target.position - coneOrigin.position;
        Vector3 direction = toTarget.normalized;
        Vector3 toTargetFromRoot = target.position - ikRestAnchor.position;
        Vector3 lateralOffset = Vector3.ProjectOnPlane(toTargetFromRoot, ikRestAnchor.up);

        if (lateralOffset.magnitude > centerDeadzoneRadius)
            lastLateral = lateralOffset.normalized;

        Vector3 desiredPos = (ikRestAnchor.position - ikRestAnchor.up * neckBendOffset) + lastLateral * neckBendOffset;

        ikTarget.position = Vector3.SmoothDamp(ikTarget.position, desiredPos, ref ikVelocity, 1f / ikFollowSpeed);
        ikTarget.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(lookRotationOffset);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (coneOrigin == null) return;

        DrawWireCone(coneOrigin.position, coneOrigin.up, coneHalfAngle, detectionRange,
                     TargetVisible ? Color.green : Color.yellow);

        if (ikTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(ikTarget.position, 0.05f);
            Gizmos.DrawLine(coneOrigin.position, ikTarget.position);
        }

        if (ikRestAnchor != null)
        {
            Gizmos.color = Color.red;
            Vector3 center = ikRestAnchor.position;
            Vector3 up = ikRestAnchor.up;

            Vector3 perp = Mathf.Abs(Vector3.Dot(up, Vector3.right)) > 0.99f ? Vector3.forward : Vector3.right;
            perp = Vector3.Cross(up, perp).normalized * centerDeadzoneRadius;

            const int segments = 32;
            Vector3 prev = center + perp;
            for (int i = 1; i <= segments; i++)
            {
                Vector3 next = center + Quaternion.AngleAxis((float)i / segments * 360f, up) * perp;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }

            Vector3 arm = Quaternion.AngleAxis(0f, up) * perp;
            Gizmos.DrawLine(center - arm, center + arm);
            arm = Quaternion.AngleAxis(90f, up) * perp;
            Gizmos.DrawLine(center - arm, center + arm);
        }
    }

    private static void DrawWireCone(Vector3 origin, Vector3 forward,
                                     float halfAngleDeg, float range, Color color)
    {
        Gizmos.color = color;
        const int segments = 20;

        Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
        Vector3 perpStart = Vector3.Cross(forward, up).normalized;
        Vector3 prevRim = Vector3.zero;

        float cosA = Mathf.Cos(halfAngleDeg * Mathf.Deg2Rad);
        float sinA = Mathf.Sin(halfAngleDeg * Mathf.Deg2Rad);

        for (int i = 0; i <= segments; i++)
        {
            Vector3 perp = Quaternion.AngleAxis((float)i / segments * 360f, forward) * perpStart;
            Vector3 rimPoint = origin + (forward * cosA + perp * sinA).normalized * range;

            if (i > 0) Gizmos.DrawLine(prevRim, rimPoint);
            if (i % (segments / 4) == 0) Gizmos.DrawLine(origin, rimPoint);

            prevRim = rimPoint;
        }
    }
#endif
}