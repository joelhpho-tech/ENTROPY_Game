using UnityEngine;

public class TendrilBehavior : MonoBehaviour
{
    public TendrilOrigin origin;
    public EnemySimpleAI ownerEnemy;
    public float maxLength = 6f;
    public float extendSpeed = 5f;
    public float retractSpeed = 5f;
    public float holdDuration = 2f;

    private LineRenderer lineRenderer;
    public bool manualRetract = false;
    public bool retractCalled = false;

    private Vector3 targetPoint;
    private float progress = 0f;
    public bool retracting = false;
    private float holdTimer = 0f;
    private bool initialized = false;

    public bool autoInitialize = false;

    // === CURVE SETTINGS ===
    public int curveResolution = 20;
    public float curveAmount = 2f;

    private Vector3 curveDirectionOffset;
    private Vector3[] localCurvePoints;

    // === REPEATING MODE ===
    public bool repeatingMode = false;

    [Header("Repeat Delay Range")]
    public float repeatDelayMin = 0.3f;
    public float repeatDelayMax = 1.0f;

    private float currentRepeatDelay = 0f;
    private float repeatDelayTimer = 0f;
    private bool isWaitingToRepeat = false;
    private bool shouldStopRepeating = false;

    [Header("Initial Delay Before First Extension")]
    public float initialStartDelay = 0f;
    private float initialDelayTimer = 0f;
    private bool waitingForInitialDelay = false;

    public void Initialize(TendrilOrigin originPoint, EnemySimpleAI owner = null, bool retractsManually = false)
    {
        origin = originPoint;
        origin.activeTendril = this;
        ownerEnemy = owner;
        manualRetract = retractsManually;
        initialized = true;

        lineRenderer = GetComponentInChildren<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError("LineRenderer not found in children of " + gameObject.name);
            return;
        }

        lineRenderer.positionCount = 2;
        Vector3 endPoint = origin.transform.position + origin.transform.forward * 0.1f;

        float angle = Random.Range(0f, 360f);
        curveDirectionOffset = Quaternion.AngleAxis(angle, origin.transform.forward) * Vector3.up;

        lineRenderer.SetPosition(0, origin.transform.position);
        lineRenderer.SetPosition(1, endPoint);

        CalculateNewTargetPoint();
        PrecalculateLocalCurve();
    }


    public void EnableRepeatingMode(float extSpeed, float retSpeed, float holdTime)
    {
        repeatingMode = true;
        extendSpeed = extSpeed;
        retractSpeed = retSpeed;
        holdDuration = holdTime;

        shouldStopRepeating = false;

        // First random delay
        currentRepeatDelay = Random.Range(repeatDelayMin, repeatDelayMax);

        if (initialStartDelay > 0f)
        {
            waitingForInitialDelay = true;
            initialDelayTimer = 0f;
        }
    }


    public void StopRepeating()
    {
        shouldStopRepeating = true;
    }

    void CalculateNewTargetPoint()
    {
        if (Physics.Raycast(origin.transform.position, origin.transform.forward, out RaycastHit hit, maxLength))
            targetPoint = hit.point;
        else
            targetPoint = origin.transform.position + origin.transform.forward * maxLength;

        float angle = Random.Range(0f, 360f);
        curveDirectionOffset = Quaternion.AngleAxis(angle, origin.transform.forward) * Vector3.up;
    }


    void PrecalculateLocalCurve()
    {
        localCurvePoints = new Vector3[curveResolution];

        Vector3 p0 = Vector3.zero;
        Vector3 p2 = origin.transform.InverseTransformPoint(targetPoint);

        Vector3 localCurveOffset = origin.transform.InverseTransformDirection(curveDirectionOffset.normalized * curveAmount);
        Vector3 p1 = (p0 + p2) * 0.5f + localCurveOffset;

        for (int i = 0; i < curveResolution; i++)
        {
            float t = (float)i / (curveResolution - 1);
            localCurvePoints[i] = CalculateQuadraticBezierPoint(t, p0, p1, p2);
        }
    }


    void Start()
    {
        if (autoInitialize && origin != null)
        {
            Initialize(origin, ownerEnemy, manualRetract);
        }
    }

    void ResetForNewCycle()
    {
        progress = 0f;
        retracting = false;
        holdTimer = 0f;
        isWaitingToRepeat = false;

        CalculateNewTargetPoint();
        PrecalculateLocalCurve();
    }

    public void Initialize(TendrilOrigin originPoint, ComplexEnemyAI owner, bool retractsManually)
    {
        origin = originPoint;
        origin.activeTendril = this;
        manualRetract = retractsManually;
        initialized = true;

        lineRenderer = GetComponentInChildren<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError("LineRenderer not found in children of " + gameObject.name);
            return;
        }

        lineRenderer.positionCount = 2;
        Vector3 endPoint = origin.transform.position + origin.transform.forward * 0.1f;

        // Pick a random angle around the forward axis
        float angle = Random.Range(0f, 360f);

        // Use Quaternion to rotate the up vector around the forward axis
        curveDirectionOffset = Quaternion.AngleAxis(angle, origin.transform.forward) * Vector3.up;

        lineRenderer.SetPosition(0, origin.transform.position);
        lineRenderer.SetPosition(1, endPoint);

        // Raycast to find wall hit point
        if (Physics.Raycast(origin.transform.position, origin.transform.forward, out RaycastHit hit, maxLength))
            targetPoint = hit.point;
        else
            targetPoint = origin.transform.position + origin.transform.forward * maxLength;
    }

    void Update()
    {
        if (!initialized || origin == null)
        {
            Debug.LogWarning("TendrilBehavior was not initialized with a valid origin.");
            return;
        }

        // Initial delay before first extension
        if (waitingForInitialDelay)
        {
            initialDelayTimer += Time.deltaTime;
            if (initialDelayTimer >= initialStartDelay)
            {
                waitingForInitialDelay = false;
            }
            else
            {
                return;
            }
        }

        // Waiting for repeat delay
        if (isWaitingToRepeat)
        {
            repeatDelayTimer += Time.deltaTime;
            if (repeatDelayTimer >= currentRepeatDelay)
            {
                repeatDelayTimer = 0f;

                if (shouldStopRepeating)
                {
                    DestroyTendril();
                    return;
                }

                // Pick next random delay
                currentRepeatDelay = Random.Range(repeatDelayMin, repeatDelayMax);

                ResetForNewCycle();
            }
            return;
        }

        // EXTENDING
        if (!retracting)
        {
            progress += Time.deltaTime * extendSpeed;

            if (progress >= 1f)
            {
                progress = 1f;

                if (!manualRetract)
                {
                    holdTimer += Time.deltaTime;
                    if (holdTimer >= holdDuration)
                        retracting = true;
                }
            }
        }
        else
        {
            // RETRACTING
            progress -= Time.deltaTime * retractSpeed;

            if (progress <= 0f)
            {
                if (repeatingMode && !shouldStopRepeating)
                {
                    isWaitingToRepeat = true;
                    repeatDelayTimer = 0f;
                }
                else
                {
                    DestroyTendril();
                    return;
                }
            }
        }

        UpdateLineRenderer();
    }

    void UpdateLineRenderer()
    {
        if (lineRenderer == null || localCurvePoints == null) return;

        int count = Mathf.Clamp(Mathf.FloorToInt(progress * curveResolution), 2, curveResolution);
        lineRenderer.positionCount = count;

        for (int i = 0; i < count; i++)
        {
            Vector3 worldPoint = origin.transform.TransformPoint(localCurvePoints[i]);
            lineRenderer.SetPosition(i, worldPoint);
        }
    }


    // Quadratic Bezier helper
    Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        return Mathf.Pow(1 - t, 2) * p0 +
               2 * (1 - t) * t * p1 +
               Mathf.Pow(t, 2) * p2;
    }

    public void Retract()
    {
        if (!initialized || retracting) return;

        retracting = true;
        holdTimer = holdDuration;
        retractCalled = true;
    }


    void DestroyTendril()
    {
        if (origin != null)
        {
            if (origin.activeTendril == this)
                origin.activeTendril = null;
        }

        if (ownerEnemy != null && !ownerEnemy.availableOrigins.Contains(origin))
            ownerEnemy.availableOrigins.Add(origin);

        Destroy(gameObject);
    }
}
