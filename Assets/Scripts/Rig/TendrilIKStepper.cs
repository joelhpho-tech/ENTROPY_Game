using UnityEngine;

public class TendrilIKStepper : MonoBehaviour
{
    [Header("References")]
    public Transform body;
    public Transform origin;
    public Transform ikTarget;

    [Header("Step Trigger")]
    public float maxBackDistance = 1.5f;

    [Header("Step Bias")]
    public Vector3 moveDirection = Vector3.zero;
    [Range(0f, 1f)]
    public float directionBias = 0.7f;

    [Header("Ground Detection")]
    public LayerMask groundMask;
    public float groundRayLength = 3f;
    public float groundOffset = 0.05f;

    [Header("Passive Mode")]
    public Transform defaultLocalPosition;
    public bool isPassive = true;

    [Header("Grab Detection")]
    public float grabRange = 1f;
    public float grabAngle = 120f;

    public float moveSpeed = 8f;
    public float passiveMoveSpeed = 20f;

    private Quaternion targetRotation = Quaternion.identity;

    public Vector3 CurrentTarget { get; private set; }
    public bool StepRequested { get; private set; }

    void Start()
    {
        StepRequested = false;

        if (!isPassive)
        {
            Vector3? target = PickStepTarget();
            if (target == null) { SetPassive(); return; }
            CurrentTarget = target.Value;
            ikTarget.position = CurrentTarget;
        }
    }

    void Update()
    {
        if (isPassive)
        {
            ikTarget.position = Vector3.Lerp(ikTarget.position, defaultLocalPosition.position, Time.deltaTime * passiveMoveSpeed);
            ikTarget.rotation = Quaternion.Lerp(ikTarget.rotation, defaultLocalPosition.rotation, Time.deltaTime * passiveMoveSpeed);

            Collider[] hits = Physics.OverlapSphere(origin.position, grabRange, groundMask);
            foreach (Collider hit in hits)
            {
                Vector3 toHit = hit.transform.position - origin.position;
                float angle = Vector3.Angle(origin.up, toHit);
                if (angle <= grabAngle * 0.5f)
                {
                    SetActive();
                    return;
                }
            }
            return;
        }

        float currentAngle = Vector3.Angle(origin.up, CurrentTarget - origin.position);
        if (currentAngle > grabAngle * 0.5f) { SetPassive(); return; }

        if ((CurrentTarget - origin.position).magnitude > grabRange * 1.1f) { SetPassive(); return; }

        if (StepRequested)
        {
            ikTarget.position = Vector3.Lerp(ikTarget.position, CurrentTarget, Time.deltaTime * moveSpeed);
            if (Vector3.Distance(ikTarget.position, CurrentTarget) < 0.05f)
            {
                ikTarget.position = CurrentTarget;
                StepRequested = false;
            }
            return;
        }

        if (ShouldStep())
        {
            Vector3? target = PickStepTarget();
            if (target == null) { SetPassive(); return; }
            CurrentTarget = target.Value;
            StepRequested = true;
        }
    }

    public void SetPassive()
    {
        isPassive = true;
    }

    public void SetActive()
    {
        Vector3? target = PickStepTarget();
        if (target == null) { SetPassive(); return; }

        isPassive = false;
        CurrentTarget = target.Value;
        StepRequested = true;
    }

    public void ConfirmStep()
    {
        StepRequested = false;
    }

    bool ShouldStep()
    {
        Vector3 toPlanted = CurrentTarget - origin.position;
        float backDot = Vector3.Dot(toPlanted, -origin.up);
        return backDot > maxBackDistance;
    }

    Vector3? PickStepTarget()
    {
        float halfGrab = grabAngle * 0.5f;
        float angle = Random.Range(0f, halfGrab);
        float roll = Random.Range(0f, 360f);

        Vector3 randomDir = Quaternion.AngleAxis(roll, origin.up)
                          * Quaternion.AngleAxis(angle, origin.right)
                          * origin.up;

        Vector3 biasDir = randomDir;
        if (moveDirection != Vector3.zero)
        {
            Vector3 flatMoveDir = body.TransformDirection(moveDirection).normalized;
            float moveAngle = Vector3.Angle(origin.up, flatMoveDir);
            Vector3 clampedMoveDir = moveAngle <= halfGrab
                ? flatMoveDir
                : Vector3.Slerp(origin.up, flatMoveDir, halfGrab / moveAngle);

            biasDir = Vector3.Slerp(randomDir, clampedMoveDir, directionBias);
        }

        if (Physics.Raycast(origin.position, biasDir, out RaycastHit hit, grabRange, groundMask))
        {
            targetRotation = Quaternion.FromToRotation(-Vector3.up, -biasDir.normalized);
            return hit.point + hit.normal * groundOffset;
        }
        
        return null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (origin == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(CurrentTarget, 0.1f);
        Gizmos.DrawLine(origin.position, CurrentTarget);

        float halfGrab = grabAngle * 0.5f;
        int segments = 32;

        Vector3 prevPoint = Vector3.zero;
        Gizmos.color = Color.green;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(CurrentTarget, targetRotation * Vector3.forward * 0.5f);

        for (int i = 0; i <= segments; i++)
        {
            float roll = (i / (float)segments) * 360f * Mathf.Deg2Rad;

            Vector3 rimPoint = origin.position
                + origin.up * (grabRange * Mathf.Cos(halfGrab * Mathf.Deg2Rad))
                + origin.right * (grabRange * Mathf.Sin(halfGrab * Mathf.Deg2Rad) * Mathf.Cos(roll))
                + origin.forward * (grabRange * Mathf.Sin(halfGrab * Mathf.Deg2Rad) * Mathf.Sin(roll));

            if (i > 0) Gizmos.DrawLine(prevPoint, rimPoint);
            prevPoint = rimPoint;

            if (i % 8 == 0) Gizmos.DrawLine(origin.position, rimPoint);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin.position, origin.up * grabRange);
    }
#endif
}