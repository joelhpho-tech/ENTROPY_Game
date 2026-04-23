using UnityEngine;

public class GeistAmbientMotion : MonoBehaviour
{
    // Cone of rotation
    [Header("Cone of Rotation")]
    [SerializeField, Range(0f, 45f)] private float maxTiltAngle = 14f;
    [SerializeField, Range(0f, 20f)] private float minTiltAngle = 1f;

    // Precession
    [Header("Precession")]
    [SerializeField] private float maxPrecessionSpeed = 6f;

    [SerializeField] private float precessionLerpSpeed = 0.15f;

    [SerializeField] private float minPrecessionHoldTime = 2.0f;
    [SerializeField] private float maxPrecessionHoldTime = 6.0f;

    // Tilt transitions
    [Header("Tilt Transitions")]
    [Tooltip("How long each full tilt transition takes.")]
    [SerializeField] private float minTransitionTime = 3.0f;
    [SerializeField] private float maxTransitionTime = 7.0f;

    [Tooltip("Higher values = longer linger at each end before moving.")]
    [SerializeField, Range(1f, 4f)] private float easePower = 2f;

    // Axis config
    [Header("Axis Configuration")]
    [SerializeField] private Vector3 forwardAxis = Vector3.forward;
    [SerializeField] private Vector3 tiltAxis = Vector3.right;

    // Runtime state
    private Quaternion restRotation;
    private Quaternion transitionStart;    
    private Quaternion currentTarget;

    private float journeyDuration;         
    private float journeyProgress;         

    private float azimuth;
    private float currentPrecessionSpeed;
    private float targetPrecessionSpeed;
    private float precessionHoldTimer;

    void Start()
    {
        restRotation = transform.localRotation;
        transitionStart = restRotation;
        currentTarget = restRotation;
        azimuth = Random.Range(0f, 360f);
        journeyProgress = 1f;     

        currentPrecessionSpeed = Random.Range(-maxPrecessionSpeed, maxPrecessionSpeed);
        PickNewPrecessionTarget();
    }

    void Update()
    {
        // Precession speed wandering
        precessionHoldTimer -= Time.deltaTime;
        if (precessionHoldTimer <= 0f)
            PickNewPrecessionTarget();

        currentPrecessionSpeed = Mathf.Lerp(
            currentPrecessionSpeed,
            targetPrecessionSpeed,
            Time.deltaTime * precessionLerpSpeed
        );

        azimuth += currentPrecessionSpeed * Time.deltaTime;

        // Tilt transition 
        journeyProgress += Time.deltaTime / journeyDuration;

        if (journeyProgress >= 1f)
        {
            // Snap exactly to the target and immediately begin the next journey
            transform.localRotation = currentTarget;
            PickNewTiltTarget();
        }
        else
        {
            // Simulated momentum through easing
            float t = journeyProgress * journeyProgress * (3f - 2f * journeyProgress);
            for (int i = 1; i < (int)easePower; i++)
                t = t * t * (3f - 2f * t);

            transform.localRotation = Quaternion.Slerp(transitionStart, currentTarget, t);
        }
    }

    private void PickNewTiltTarget()
    {
        // Store wherever we actually are right now as the new start point
        // so there is never a discontinuity regardless of when this is called
        transitionStart = transform.localRotation;

        float tiltAngle = Random.Range(minTiltAngle, maxTiltAngle);
        float azimuthOffset = Random.Range(-70f, 70f);

        Quaternion tilt = Quaternion.AngleAxis(tiltAngle, tiltAxis);
        Quaternion spin = Quaternion.AngleAxis(azimuth + azimuthOffset, forwardAxis);

        currentTarget = restRotation * spin * tilt;
        journeyDuration = Random.Range(minTransitionTime, maxTransitionTime);
        journeyProgress = 0f;
    }

    private void PickNewPrecessionTarget()
    {
        // 20% chance to pause rather than pick a new rotation
        targetPrecessionSpeed = Random.value < 0.2f
            ? Random.Range(-4f, 4f)
            : Random.Range(-maxPrecessionSpeed, maxPrecessionSpeed);

        precessionHoldTimer = Random.Range(minPrecessionHoldTime, maxPrecessionHoldTime);
    }

    // Call this if the Geists rest pose changes at runtime
    public void ResetRestRotation()
    {
        restRotation = transform.localRotation;
        transitionStart = restRotation;
        currentTarget = restRotation;
        journeyProgress = 1f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;
        Vector3 fwd = transform.TransformDirection(forwardAxis);
        Vector3 tilt = transform.TransformDirection(tiltAxis);
        int segs = 48;
        float radius = 0.5f;

        UnityEditor.Handles.color = new Color(0.85f, 0.2f, 0.7f, 0.2f);
        Vector3 prev = Vector3.zero;
        for (int i = 0; i <= segs; i++)
        {
            float a = i / (float)segs * 360f * Mathf.Deg2Rad;
            Quaternion rot = Quaternion.AngleAxis(maxTiltAngle, tilt)
                           * Quaternion.AngleAxis(a * Mathf.Rad2Deg, fwd);
            Vector3 dir = transform.rotation * rot * forwardAxis;
            Vector3 tip = origin + dir * radius;
            if (i > 0) UnityEditor.Handles.DrawLine(prev, tip);
            if (i % 12 == 0) UnityEditor.Handles.DrawLine(origin, tip);
            prev = tip;
        }

        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.DrawLine(origin,
            origin + transform.TransformDirection(currentTarget * forwardAxis) * radius);
    }
#endif
}