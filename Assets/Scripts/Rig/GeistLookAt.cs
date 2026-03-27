using UnityEngine;

public class GeistLookAt : MonoBehaviour
{
    [Header("Vision Cone")]
    public Transform coneOrigin;
    public Transform target;

    [Range(1f, 180f)]
    public float coneHalfAngle = 60f;
    public float detectionRange = 10f;

    [Header("IK Target")]
    [SerializeField]
    private Transform ikTarget;
    public Transform ikRestAnchor;
    public float neckBendOffset = 0.3f;
    public float ikFollowSpeed = 5f;
    public Vector3 lookRotationOffset = new Vector3(-90f, 0f, 180f);

    public bool TargetVisible { get; private set; }

    private Vector3 lastLateral = Vector3.right;
    private Vector3 ikVelocity;

    void Start()
    {
        target = FindFirstObjectByType<ZeroGravity>().transform;
    }

    private void LateUpdate()
    {
        TargetVisible = CheckVisibility();
        SetIKTargetPosition();
    }

    private bool CheckVisibility()
    {
        if (target == null || coneOrigin == null) return false;

        Vector3 toTarget = target.position - coneOrigin.position;

        if (toTarget.magnitude > detectionRange) return false;
        if (Vector3.Angle(coneOrigin.up, toTarget) > coneHalfAngle) return false;

        return true;
    }

    private void SetIKTargetPosition()
    {
        if (ikTarget == null || ikRestAnchor == null) return;
        if (!TargetVisible)
        {
            Vector3 targetPos = ikRestAnchor.position;
            ikTarget.position = Vector3.SmoothDamp(ikTarget.position, targetPos, ref ikVelocity, 1f / ikFollowSpeed);
            ikTarget.rotation = Quaternion.LookRotation(transform.forward) * Quaternion.Euler(lookRotationOffset);
            return;
        }
        Vector3 direction = (target.position - coneOrigin.position).normalized;
        Vector3 lateral = Vector3.ProjectOnPlane(direction, ikRestAnchor.up);
        if (lateral.magnitude > 0.2f)
            lastLateral = lateral.normalized;
        Vector3 behindPoint = ikRestAnchor.position - ikRestAnchor.up * neckBendOffset;
        Vector3 desiredPos = behindPoint + lastLateral * neckBendOffset;
        ikTarget.position = Vector3.SmoothDamp(ikTarget.position, desiredPos, ref ikVelocity, 1f / ikFollowSpeed);
        ikTarget.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(lookRotationOffset);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (coneOrigin == null) return;

        DrawWireCone(coneOrigin.position, coneOrigin.up,
                     coneHalfAngle, detectionRange,
                     TargetVisible ? Color.green : Color.yellow);

        if (ikTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(ikTarget.position, 0.05f);
            Gizmos.DrawLine(coneOrigin.position, ikTarget.position);
        }
    }

    private static void DrawWireCone(Vector3 origin, Vector3 forward,
                                     float halfAngleDeg, float range, Color color)
    {
        Gizmos.color = color;
        int segments = 20;

        Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.99f
            ? Vector3.right : Vector3.up;
        Vector3 perpStart = Vector3.Cross(forward, up).normalized;

        Vector3 prevRim = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector3 perp = Quaternion.AngleAxis(t * 360f, forward) * perpStart;
            Vector3 rimDir = (forward * Mathf.Cos(halfAngleDeg * Mathf.Deg2Rad) +
                              perp * Mathf.Sin(halfAngleDeg * Mathf.Deg2Rad)).normalized;
            Vector3 rimPoint = origin + rimDir * range;

            if (i > 0) Gizmos.DrawLine(prevRim, rimPoint);
            if (i % (segments / 4) == 0) Gizmos.DrawLine(origin, rimPoint);

            prevRim = rimPoint;
        }
    }
#endif
}