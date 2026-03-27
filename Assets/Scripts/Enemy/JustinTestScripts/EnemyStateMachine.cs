
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public enum SpecificEnemyState
{
    Chase,
    Track,
    Investigate,
    Patrol,
    Throw,
    Kill,
    Retreat,
    Stunned,
}

public enum GeneralEnemyState
{
    Pause,
    Active,
    Retreat,
    Dev,
    Idle, // Special State for certain circumstances
}

public enum EnemyVersion
{
    Complex,
    Simple
}

public class EnemyStateMachine : MonoBehaviour
{
    [Header("Enum Values")]
    public EnemyVersion enemyVersion;
    public GeneralEnemyState currentGeneralState = GeneralEnemyState.Active;
    public GeneralEnemyState pastGeneralState;
    public SpecificEnemyState currentSpecificState = SpecificEnemyState.Patrol;
    public SpecificEnemyState pastSpecificState;

    [Header("References")]
    public GameObject player;
    public ComplexEnemyAI complecEnemyAI;
    //[SerializeField] private GameObject simpleEnemy;
    //[SerializeField] private GameObject complexEnemy;

    [Header("Detection Settings")]
    public LayerMask detectionMask; // Set this to "Default", "Player", and "Obstacles"
    public float detectionRadius;
    public float defaultDetectionRadius = 10f;
    public float chaseDetectionRadius = 15f;
    public float detectionDuration = 1.5f; // Time to "spot" player
    
    [SerializeField] private float detectionTimer;
    public bool canDetectPlayer = false;
    [SerializeField] private bool chasePlayer = false;

    [Header("Interest/Search Settings")]
    public float interestDuration = 10f;
    [SerializeField] private float interestTimer;
    public Vector3 playersLastKnownLocation;
    private bool isInvestigating;
    public bool shouldFollow;

    [Header("Retreat Settings")]
    public float retreatDuration = 3f;
    public float retreatDistanceCheck = 20f;
    public LayerMask barrierLayer;
    public LayerMask waypointLayer;
    public float minRadius = 7f;
    public float maxRadius = 9f;
    [SerializeField] private float retreatTimer;
    [SerializeField] private float retreatPointReroll;
    [SerializeField] private bool canRetreat;

    [Header("Throw Settings")]

    [SerializeField] private float throwDistanceCheck;
    [SerializeField] private bool canThrow;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private int throwCheckLimit;
    [SerializeField] private Vector3 ThrowCheckOffset;

    [Header("Gizmos")]
    [SerializeField] private bool showDetectionRadius;
    [SerializeField] private bool showRetreatRadius;

    [Header("Light Detection")]

    private bool isLightOn;
    private float lightDetectionRange;
    private float lightDetectionCooldown = 3f;
    private float lightDetectionDuration = 0f;
    private float lightDetectionTimer = 0f;

    private void Start()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");

        if (complecEnemyAI == null) complecEnemyAI = GetComponent<ComplexEnemyAI>(); 

        currentSpecificState = SpecificEnemyState.Patrol;
    }

    public void GeneralLogic()
    {
        if (currentGeneralState == GeneralEnemyState.Pause) return;

        // Run detection every frame
        canDetectPlayer = PerformRaycastDetection();

        if (isLightOn)
        {
            //canDetectPlayer = FireDetectionGrid();
        }

        switch (currentGeneralState)
        {
            case GeneralEnemyState.Active:
                HandleActiveLogic();
                break;
            case GeneralEnemyState.Retreat:
                HandleRetreatLogic();
                break;
            case GeneralEnemyState.Idle:
                break;
            case GeneralEnemyState.Dev:
                break;

        }
    }

 

    #region Active Logic
    private void HandleActiveLogic()
    {
        switch (currentSpecificState)
        {
            case SpecificEnemyState.Patrol:
                complecEnemyAI.isPatroling();

                detectionRadius = defaultDetectionRadius;

                if (canDetectPlayer)
                {
                    if (!chasePlayer)
                    {
                        detectionTimer -= Time.deltaTime;
                        if (detectionTimer <= 0)
                        {
                            chasePlayer = true;
                            ChangeSpecificState(SpecificEnemyState.Chase);
                        }
                    }
                }
                else
                {
                    chasePlayer = false;
                    detectionTimer = detectionDuration;

                    complecEnemyAI.investigatingWaypoint = null;
                    complecEnemyAI.lastSeenWaypoint = null;
                }
                break;

            case SpecificEnemyState.Chase:
                detectionRadius = chaseDetectionRadius;

                complecEnemyAI.IsChasingPlayer();
                if (!canDetectPlayer)
                {
                    interestTimer = interestDuration;
                    GetLastSeenLocation();
                    isInvestigating = false;
                    ChangeSpecificState(SpecificEnemyState.Investigate);
                }
                break;

            case SpecificEnemyState.Investigate:
                interestTimer -= Time.deltaTime;

                if (canDetectPlayer)
                {
                    ChangeSpecificState(SpecificEnemyState.Chase);
                    isInvestigating = false;
                }
                // Only switch to Patrol if the timer is DONE
                else if (interestTimer <= 0)
                {
                    if (complecEnemyAI.investigatingWaypoint != null)
                    {
                        complecEnemyAI.currentWaypoint = complecEnemyAI.investigatingWaypoint;
                    }

                    isInvestigating = false;
                    complecEnemyAI.investigatingWaypoint = null;
                    ChangeSpecificState(SpecificEnemyState.Patrol);
                }
                else
                {
                    // Increase distance threshold to 1.0f for more reliable detection
                    float dist = (complecEnemyAI.investigatingWaypoint != null) ? 
                        Vector3.Distance(transform.position, complecEnemyAI.investigatingWaypoint.transform.position) : 0f;

                    bool reachedPoint = complecEnemyAI.investigatingWaypoint == null || dist < 1.0f;

                    if (reachedPoint)
                    {
                        // Reset the flag so GetRandomInvestPoint can be called again
                        isInvestigating = false;

                        Waypoint searchOrigin = (complecEnemyAI.lastSeenWaypoint != null) ? 
                            complecEnemyAI.lastSeenWaypoint : complecEnemyAI.currentWaypoint;

                        if (searchOrigin != null && !isInvestigating)
                        {
                            complecEnemyAI.investigatingWaypoint = GetRandomInvestPoint(searchOrigin);
                            shouldFollow = (Random.value > 0.5f);
                            isInvestigating = true;
                        }
                    }
                    complecEnemyAI.IsInvestigating();
                }
                break;
        }
    }
    #endregion

    #region Investigate
    public List<Waypoint> FindInvestWaypoints(Waypoint OriginPoint)
    {
        // Safety check to prevent NullReferenceException
        if (OriginPoint == null) return new List<Waypoint>();

        HashSet<Waypoint> waypoints = new HashSet<Waypoint>();

        // Layer 1: Only check the direct neighbors of the starting point
        foreach (Waypoint neighbor in OriginPoint.neighbors)
        {
            if (neighbor != null)
            {
                waypoints.Add(neighbor);
            }
        }

        // We no longer loop through 'neighbor.neighbors', effectively limiting the search
        return new List<Waypoint>(waypoints);
    }


    public Waypoint GetRandomInvestPoint(Waypoint OriginPoint)
    {
        List<Waypoint> waypoints = FindInvestWaypoints(OriginPoint);

        return waypoints[Random.Range(0, waypoints.Count)];
    }
    private void GetLastSeenLocation()
    {
        playersLastKnownLocation = player.transform.position;

        complecEnemyAI.lastSeenWaypoint = complecEnemyAI.FindClosestWaypoint(playersLastKnownLocation);


    }

    #endregion

    #region Retreat Logic

    private void HandleRetreatLogic()
    {
        retreatTimer -= Time.deltaTime;

        // 1. Exit Condition
        if (retreatTimer <= 0)
        {
            ChangeGeneralState(GeneralEnemyState.Active);
            ChangeSpecificState(SpecificEnemyState.Patrol);
            return;
        }

        // 2. Logic based on Player Line of Sight
        if (IsPlayerLookingAtMe())
        {
            // Only calculate a path if we don't have one or the current one is bad
            if (complecEnemyAI.path.Count == 0 || complecEnemyAI.CheckIfPlayerInWay())
            {
                bool foundSafePath = false;

                // Try to find a path that doesn't go through the player
                for (int i = 0; i < 5; i++)
                {
                    complecEnemyAI.FindRetreatPath(); // This picks a random point and BFSs

                    if (!complecEnemyAI.CheckIfPlayerInWay())
                    {
                        foundSafePath = true;
                        break;
                    }
                }

                // If we tried 5 times and the player is STILL in the way of every path
                if (!foundSafePath)
                {
                    // Force "Ghost Mode" through walls toward a retreat point
                    complecEnemyAI.MoveThanTeleportInPointDirection();
                    return; // Exit this frame to let it move
                }
            }

            // Move along the path we found
            complecEnemyAI.TrackPath();
        }
        else
        {
            // Player ISN'T looking: Escape quickly
            complecEnemyAI.TeleportToWaypoint();

            // Optionally end retreat early since we escaped
            retreatTimer = 0;
        }
    }

    private bool IsPlayerLookingAtMe()
    {
        Vector3 dir = player.transform.position - transform.position;
        // If there's a barrier in the way, the player CANNOT see me
        return !Physics.Raycast(transform.position, dir.normalized, retreatDistanceCheck, barrierLayer);
    }

    // A safer version of your random point logic using a loop instead of recursion
    public Waypoint GetRandomValidPoint()
    {
        float currentMax = maxRadius;
        for (int i = 0; i < 5; i++) // Try 5 times to expand radius
        {
            Collider[] cols = Physics.OverlapSphere(transform.position, currentMax, waypointLayer);
            List<Waypoint> validPoints = new List<Waypoint>();

            foreach (var c in cols)
            {
                if (Vector3.Distance(transform.position, c.transform.position) >= minRadius)
                {
                    Waypoint wp = c.GetComponent<Waypoint>();
                    if (wp != null) validPoints.Add(wp);
                }
            }

            if (validPoints.Count > 0)
                return validPoints[Random.Range(0, validPoints.Count)];

            currentMax += 5f; // Expand search area
        }
        return null;
    }
    #endregion

    #region Idle Logic

    #endregion

    #region Grab/Throw/Attack Logic

    private void GrabAttackLogic()
    {
        if (player.GetComponent<ZeroGravity>().PlayerHealth <= player.GetComponent<ZeroGravity>().MaxHealth / 2)
        {
            ChangeSpecificState(SpecificEnemyState.Kill);

        }
        else 
        {
            ChangeSpecificState(SpecificEnemyState.Throw);
        }
    }

    private void DetermineThrowLocation()
    {
        if (CanThrow(ThrowCheckOffset))
        {

        }
        else
        {
            //
            for (int x = 0; x < throwCheckLimit; x++)
            {

            }
        }
    }

    private bool CanThrow(Vector3 checkOfSet)
    {
        if (player == null) return false;

        Vector3 dir = player.transform.position - transform.position;
        RaycastHit hit;

        if (Physics.Raycast(transform.position, dir.normalized, out hit, throwDistanceCheck, wallLayer))
        {
            return false;
        }
        return true;
    }

    private void LockPlayerInputs()
    {

    }

    private void LookAtGeist()
    {

    }

    #endregion

    #region Detection Logic

    #endregion

    #region State Tools
    public void ChangeSpecificState(SpecificEnemyState newState)
    {
        if (currentSpecificState == newState) return;

        // Clear pathing when changing states to prevent "Ghost Paths"
        if (complecEnemyAI != null)
        {
            complecEnemyAI.path.Clear();
            complecEnemyAI.targetWaypoint = null;
        }

        pastSpecificState = currentSpecificState;
        currentSpecificState = newState;
        Debug.Log($"State Changed to: {newState}");
    }

    public void ChangeGeneralState(GeneralEnemyState newState)
    {
        if (currentGeneralState == newState) return;

        // Clear pathing when changing states to prevent "Ghost Paths"
        if (complecEnemyAI != null)
        {
            complecEnemyAI.path.Clear();
            complecEnemyAI.targetWaypoint = null;
        }

        pastGeneralState = currentGeneralState;
        currentGeneralState = newState;
        Debug.Log($"State Changed to: {newState}");
    }

    private bool PerformRaycastDetection()
    {
        if (player == null) return false;

        Vector3 dir = player.transform.position - transform.position;
        RaycastHit hit;

        if (Physics.Raycast(transform.position, dir.normalized, out hit, detectionRadius, detectionMask))
        {
            if (hit.collider.CompareTag("Player"))
            {
                return true;
            }
        }
        return false;
    }

    private bool DetectPlayerFlashLight()
    {
        if (!isLightOn) return false;

        

        return false;
    }

    private void FireDetectionGrid(float columns, float rows, float spacing, float range)
    {
        // Calculate offsets to ensure the grid is centered on the forward vector
        float halfWidth = (columns - 1) * spacing / 2f;
        float halfHeight = (rows - 1) * spacing / 2f;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                float xOffset = (x * spacing) - halfWidth;
                float yOffset = (y * spacing) - halfHeight;

                Vector3 rayDirection = transform.forward + (transform.right * xOffset) + (transform.up * yOffset);

                rayDirection.Normalize();

                if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, range))
                {
                    
                    // Add hit logic here
                }
            }
        }
    }
    #endregion

    #region Dev Tools



    #endregion

    private void OnDrawGizmos()
    {
        if (showDetectionRadius)
        {
            Gizmos.color = canDetectPlayer ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            if (player != null)
                Gizmos.DrawLine(transform.position, player.transform.position);
        }
        
        if (showRetreatRadius)
        {
            Gizmos.color = Color.blue;

            Gizmos.DrawWireSphere(transform.position, minRadius);
            Gizmos.DrawWireSphere(transform.position, maxRadius);
        }
    }
}
