using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class EnemySimpleAI : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3.0f;
    public float chaseDistance = 15.0f;
    public float escapeDistance = 20f;
    public bool useRandomRoaming;
    public float setRotationSpeed = 2.5f;
    private float rotationSpeed = 2.5f;

    [Header("Lunge Settings")]
    public float lungeDistance = 5f;
    public float chargeUpTime = 1.5f;
    public float lungeSpeed = 3.5f;
    public float lungeDuration = 3f;
    public int maxRicochets = 2;
    private int ricochetCount = 0;
    public float ricochetSpeedMultiplier = 0.8f; // slow down a bit after bounce

    [Header("Stun Settings")]
    public float stunSeconds = 3f;
    public float stunVelocityThreshold = 4f;

    [Header("References")]
    public GameObject player;
    private ZeroGravity playerController;
    public Waypoint startingWaypoint;
    public Transform waypointGroup;
    //public DoorScript door;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioSource audioSource2;
    public AudioClip alienSfx;
    public AudioClip chargingSound;
    public AudioClip lungeSound;
    public AudioClip takeDamage;

    [Header("Tendril Settings")]
    public GameObject tendrilPrefab;
    public List<TendrilOrigin> tendrilOrigins;
    public List<TendrilOrigin> backwardsOrigins = new List<TendrilOrigin>();
    public float spawnInterval = 4f;
    private float lastTendrilTime = 0f;

    [Header("Optimization")]
    public float wakeDistance = 50f;

    //Line of sight
    public LayerMask barrierLayer; // Set this to "Barrier"
    public float wakeLossCooldown = 10f;
    private float timeSinceLastSeenPlayer = 0f;
    private bool hasLineOfSight = false;

    // Internal state
    public Waypoint currentWaypoint;
    private Queue<Waypoint> path = new Queue<Waypoint>();
    public Waypoint playerWaypoint;
    public Waypoint targetWaypoint;

    private List<Waypoint> allWaypoints = new List<Waypoint>();
    private List<Waypoint> roamingWaypoints = new List<Waypoint>();
    private Waypoint goalWaypoint;


    //private float distanceToPlayer;
    private float sqrDist;
    public bool isChasingPlayer = false;
    public bool isStunned = false;

    public bool isCharging = false;
    public bool isLunging = false;
    public float lungeTimer = 0f;
    public bool isAwake = false;

    //direction calculation
    private float directionUpdateThreshold = 0.05f; // Minimal movement to update direction
    private Vector3 lastPosition;
    private Vector3 currentDirection;

    //public Vector3 initialPosition;
    private Quaternion initialRotation;

    private float resetCooldown = 0.2f;
    public List<TendrilOrigin> availableOrigins;

    //checking for clear path
    private float clearPathCheckCooldown = 0.25f;
    private float clearPathCheckTimer = 0f;
    private bool hasClearPath = true;


    void Start()
    {
        allWaypoints = waypointGroup.GetComponentsInChildren<Waypoint>().ToList();
        foreach (Waypoint wp in allWaypoints)
        {
            if (wp.type == Waypoint.WaypointType.Roaming)
            {
                roamingWaypoints.Add(wp);
            }
        }

        rotationSpeed = setRotationSpeed;
        // Set the current waypoint to the starting one
        currentWaypoint = startingWaypoint;

        //initialPosition = transform.position;
        initialRotation = transform.rotation;

        FindPlayerPath();

        playerController = player.GetComponent<ZeroGravity>();

        audioSource.clip = alienSfx;
        audioSource.loop = true;
        audioSource.Stop();

        // Rigidbody must exist and start in kinematic mode
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        lastPosition = transform.position;

        tendrilOrigins = GetComponentsInChildren<TendrilOrigin>().ToList();
        availableOrigins = new List<TendrilOrigin>(tendrilOrigins);

        StartCoroutine(UpdateLineOfSight());
    }

    void Update()
    {
        if (resetCooldown > 0f)
        {
            resetCooldown -= Time.deltaTime;
            return;
        }

        if (playerController.IsDead == true)
        {
            //RoamArea();

            CalculateDirection();
            RotateTowardsDirection();

            if (!isStunned && !isLunging && Time.time - lastTendrilTime >= spawnInterval)
            {
                SpawnTendril();
                lastTendrilTime = Time.time;
            }

            return;
        }

        sqrDist = (transform.position - player.transform.position).sqrMagnitude;

        if (sqrDist > wakeDistance * wakeDistance)
        {
            isAwake = false;
        }
        else
        {
            isAwake = true;
        }

        if (!isAwake)
        {
            return;
        }


        // 2) If stunned, do nothing else (charging/lunging will abort)
        if (isStunned) return;

        // 3) If mid-lunge, track timeout
        if (isLunging)
        {
            lungeTimer += Time.deltaTime;
            if (lungeTimer >= lungeDuration)
                EndLunge();
            return;
        }

        // 4) If mid-charge, skip normal AI
        if (isCharging)
        {
            ForceLookAtPlayer(); // Always rotate toward player during charge
            return;
        }
        // 5) Check if player is within lungeDistance, the enemy has line of sight with the player, and not charging or lunging → start charging
        if (hasLineOfSight && sqrDist < (lungeDistance * lungeDistance) && !isCharging && !isLunging)
        {


            Vector3 toPlayer = player.transform.position - transform.position;
            float checkDistance = toPlayer.magnitude;
            Vector3 direction = toPlayer.normalized;

            // Perform a SphereCast in the direction of the player
            float sphereRadius = 0.5f * transform.localScale.x; // adjust based on your alien's size
            

            //check to see if there's anything in the way before we lunge into a wall
            if (!Physics.SphereCast(transform.position, sphereRadius, direction, out RaycastHit hit, 5f, barrierLayer))
            {
                isCharging = true;
                StartCoroutine(ChargeAndLunge());
                return;
            }
            else
            {
                // Optional: debug visualization
                Debug.DrawRay(transform.position, direction * 5.0f, Color.red, 0.2f);
                //Debug.Log("Lunge blocked by: " + hit.collider.name);
            }
        }

        // 6) Otherwise, run normal AI (chase/roam/track)
        RunNormalAIBehavior();

        CalculateDirection();
        RotateTowardsDirection();

        if (!isStunned && !isLunging && Time.time - lastTendrilTime >= spawnInterval)
        {
            SpawnTendril();
            lastTendrilTime = Time.time;
        }

    }

    void RunNormalAIBehavior()
    {
        //if chasing player, check for line of sight.
        if (isChasingPlayer)
        {
            if (!audioSource.isPlaying) audioSource.Play();

            clearPathCheckTimer += Time.deltaTime;

            if (clearPathCheckTimer >= clearPathCheckCooldown)
            {
                Vector3 direction = (player.transform.position - transform.position).normalized;
                hasClearPath = HasClearPath(direction, Vector3.Distance(player.transform.position, transform.position)); // Or some distance like 5f
                clearPathCheckTimer = 0f;
            }

            if (hasClearPath)
            {
                ChasePlayer(); // Direct chase
            }
            //if line of sight is lost, go back to waypoint tracking
            else
            {
                // LOS lost, fall back to pathfinding
                FindPlayerPath();
                /*
                if(path.Count > 0)
                {
                    Waypoint nextWaypoint = path.Peek();
                    if (nextWaypoint != null)
                    {
                        // Check if we are between currentWaypoint and nextWaypoint
                        if (IsBetweenWaypoints(currentWaypoint.transform.position, nextWaypoint.transform.position, transform.position))
                        {
                            Vector3 directionToNext = (nextWaypoint.transform.position - transform.position).normalized;

                            // Check clear path to next waypoint
                            if (HasClearPath(directionToNext, Vector3.Distance(transform.position, nextWaypoint.transform.position)))
                            {
                                currentWaypoint = nextWaypoint;
                                path.Dequeue(); // Move along path
                            }
                        }
                    }
                }
                */
                TrackPlayer();
                //isChasingPlayer = false;

            }

            float sqrDist = (transform.position - player.transform.position).sqrMagnitude;
            if (sqrDist >= escapeDistance * escapeDistance && timeSinceLastSeenPlayer >= wakeLossCooldown)
            {
                isChasingPlayer = false;
            }
        }
        //if not chasing player
        else
        {
            float sqrDist = (transform.position - player.transform.position).sqrMagnitude;

            //if we have line of sight and we are within chase distance, start chasing
            if (hasLineOfSight && sqrDist <= chaseDistance * chaseDistance)
            {
                isChasingPlayer = true;
                ChasePlayer();
            }
            else if (sqrDist <= chaseDistance * chaseDistance)
            {
                //FindPlayerPath();
                
            }
            else
            {
                //simply roam the full area if we have escaped the limited roaming area
                if(currentWaypoint.type == Waypoint.WaypointType.General)
                {
                    RoamArea();
                    
                }
                else
                {
                    RoamLimited();
                }
                isChasingPlayer = false;
                if (audioSource.isPlaying) audioSource.Stop();
            }
        }
    }

    // Called by Unity when this collider hits another collider
    private void OnCollisionEnter(Collision collision)
    {
        GameObject other = collision.gameObject;

        // 1) If its a thrown pickup object, stun
        if (other.CompareTag("PickupObject")) 
        {
            Rigidbody objRb = collision.rigidbody;
            if (objRb != null && objRb.linearVelocity.magnitude >= stunVelocityThreshold)
            {
                Debug.Log("Object hit at this speed: " + objRb.linearVelocity.magnitude);
                StartCoroutine(StunCoroutine());
            }
        }
        // 2) If its the player, kill them
        else if (other.CompareTag("Player"))
        {
            
            if (!playerController.IsDead)
            {
                Debug.Log("Player killed by alien");
                playerController.IsDead = true;
                isChasingPlayer = false;
                EndLunge();
            }
            Rigidbody playerRb = other.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                Vector3 forceDirection = (other.transform.position - transform.position).normalized;
                float knockbackStrength = lungeSpeed * 2f; // adjust multiplier as needed

                playerRb.AddForce(forceDirection * knockbackStrength, ForceMode.Impulse);
            }
            
        }
        else if(isLunging)
        {
            if (other.CompareTag("Barrier"))
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb == null) return;

                Vector3 incomingVelocity = rb.linearVelocity;
                Vector3 normal = collision.contacts[0].normal;

                // Reflect the velocity vector around the collision normal
                Vector3 reflectedVelocity = Vector3.Reflect(incomingVelocity, normal);

                // Optionally reduce speed slightly after bounce
                reflectedVelocity *= ricochetSpeedMultiplier;

                // Apply the new velocity
                rb.linearVelocity = reflectedVelocity;

                ricochetCount++;

                // If ricochet count exceeded max, end the lunge early
                if (ricochetCount >= maxRicochets)
                {
                    EndLunge();
                }
            }
        }
    }

    private IEnumerator StunCoroutine()
    {
        if (isStunned) yield break;

        // Cancel any ongoing charge or lunge
        isStunned = true;
        if (isCharging)
        {
            isCharging = false;
            // We let the ChargeAndLunge coroutine exit gracefully on its next frame check.
        }
        if (isLunging)
        {
            EndLunge();
        }

        // Retract ALL current tendrils
        foreach (var origin in tendrilOrigins)
        {
            if (origin.activeTendril != null)
            {
                origin.activeTendril.Retract();
            }
        }

        //sfx
        audioSource.Stop();
        audioSource2.Stop();
        audioSource2.PlayOneShot(takeDamage);
        

        // Wait for stun duration
        yield return new WaitForSeconds(stunSeconds);

        // Un-stun and restore AI
        isStunned = false;
        FindPlayerPath();
    }

    void ChasePlayer()
    {
        // Move directly towards the player
        transform.position = Vector3.MoveTowards(transform.position, player.transform.position, speed * Time.deltaTime);

        UpdateCurrentWaypointToClosest();
    }

    void TrackPlayer()
    {
        if (path.Count == 0)
        {
            FindPlayerPath();
            return;
        }

        targetWaypoint = path.Peek();
        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.transform.position, speed * Time.deltaTime);

        // Advance when close enough
        if ((transform.position - targetWaypoint.transform.position).sqrMagnitude < 0.01f)
        {
            path.Dequeue();
            currentWaypoint = targetWaypoint;
        }

        // If finished path, recalculate
        if (path.Count == 0)
        {
            FindPlayerPath();
        }
    }

    void RoamArea()
    {
        // Pick new goal when we don't have one or reached current goal
        if (goalWaypoint == null || (path.Count == 0 && Vector3.Distance(transform.position, currentWaypoint.transform.position) < 0.1f))
        {
            // Pick a random waypoint at least 20 units away
            List<Waypoint> farWaypoints = allWaypoints.Where(wp =>
                Vector3.Distance(transform.position, wp.transform.position) > 20f
            ).ToList();

            if (farWaypoints.Count > 0)
            {
                goalWaypoint = farWaypoints[Random.Range(0, farWaypoints.Count)];
                path = BFS(currentWaypoint, goalWaypoint);
            }
        }

        // Follow the path
        if (path.Count > 0)
        {
            Waypoint nextWaypoint = path.Peek();
            transform.position = Vector3.MoveTowards(transform.position, nextWaypoint.transform.position, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, nextWaypoint.transform.position) < 0.1f)
            {
                currentWaypoint = nextWaypoint;
                path.Dequeue();
            }
        }
    }

    void RoamLimited()
    {
        if (currentWaypoint == null) return;

        // If we've reached the current waypoint, choose a new roaming neighbor
        if (Vector3.Distance(transform.position, currentWaypoint.transform.position) < 0.1f)
        {
            List<Waypoint> roamingNeighbors = new List<Waypoint>();

            foreach (Waypoint neighbor in currentWaypoint.neighbors)
            {
                if (neighbor != null && neighbor.type == Waypoint.WaypointType.Roaming)
                {
                    roamingNeighbors.Add(neighbor);
                }
            }

            if (roamingNeighbors.Count > 0)
            {
                Waypoint nextWaypoint = roamingNeighbors[Random.Range(0, roamingNeighbors.Count)];
                currentWaypoint = nextWaypoint;
            }
            else
            {
                // Stay at current waypoint if no valid roaming neighbors
                Debug.LogWarning($"Waypoint {currentWaypoint.name} has no roaming neighbors.");
            }
        }

        // Move toward the current waypoint
        transform.position = Vector3.MoveTowards(transform.position, currentWaypoint.transform.position, speed * Time.deltaTime);
    }

    private IEnumerator ChargeAndLunge()
    {
        Debug.Log("Charging Coroutine called");
        rotationSpeed = 20f;
        isChasingPlayer = false;
        ricochetCount = 0;

        audioSource2.clip = chargingSound;
        audioSource2.Play();

        // Retract ALL current tendrils
        foreach (TendrilOrigin origin in tendrilOrigins)
        {
            if (origin.activeTendril != null)
            {
                origin.activeTendril.Retract();
                origin.activeTendril = null; // clear immediately
            }
        }

        // Wait until the enemy is facing the player before continuing
        Vector3 toPlayer = (player.transform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, toPlayer);

        while (angle > 20f) // or whatever threshold feels right
        {
            toPlayer = (player.transform.position - transform.position).normalized;
            angle = Vector3.Angle(transform.forward, toPlayer);
            //Debug.Log(angle);
            yield return null;
        }


        // Step 2: Spawn 5 tendrils at random from backwardsOrigins
        List<TendrilOrigin> shuffled = new List<TendrilOrigin>(backwardsOrigins);
        int spawnCount = Mathf.Min(6, shuffled.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            int index = Random.Range(0, shuffled.Count);
            TendrilOrigin origin = shuffled[index];
            shuffled.RemoveAt(index);

            // Spawn tendril at the backwards origin
            GameObject t = Instantiate(tendrilPrefab, origin.transform.position, origin.transform.rotation, origin.transform);
            TendrilBehavior tb = t.GetComponent<TendrilBehavior>();
            if (tb != null)
            {
                tb.Initialize(origin, this, true); // true = manualRetract
                origin.activeTendril = tb;
            }
        }


        float timer = 0f;
        while (timer < chargeUpTime)
        {
            if (isStunned)
            {
                isCharging = false;
                yield break;
            }

            // Pull-back motion
            toPlayer = (player.transform.position - transform.position).normalized;
            Vector3 pullBackDirection = -toPlayer; // Opposite of direction to player
            float pullBackSpeed = 1.5f; // tweak as needed

            float pullBackDistance = pullBackSpeed * Time.deltaTime;
            float radius = 0.5f * transform.localScale.x;

            // Raycast to prevent backing into a wall
            RaycastHit hit;
            if (!Physics.SphereCast(transform.position, radius, pullBackDirection, out hit, pullBackDistance + 0.25f, barrierLayer))
            {
                transform.position += pullBackDirection * pullBackDistance;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        foreach (TendrilOrigin origin in backwardsOrigins)
        {
            if (origin.activeTendril != null)
            {
                origin.activeTendril.Retract();
            }
        }
        audioSource2.Stop();
        audioSource2.clip = lungeSound;
        audioSource2.Play();

        // Finish charging → launch the lunge
        Vector3 dir = (player.transform.position - transform.position).normalized;
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.linearVelocity = dir * lungeSpeed;

        isLunging = true;
        lungeTimer = 0f;
        rotationSpeed = setRotationSpeed;
        

    }

    private void EndLunge()
    {
        if (!isLunging) return;

        isLunging = false;
        isCharging = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        FindPlayerPath();
    }

    void FindPlayerPath()
    {
        if(playerWaypoint == null)
        {
            playerWaypoint = FindClosestWaypoint(player.transform.position);
            path = BFS(currentWaypoint, playerWaypoint);
        }
        else
        {
            Waypoint testWaypoint = FindClosestWaypoint(player.transform.position);
            //only do a new BFS if the player waypoint is new.
            if(playerWaypoint != testWaypoint)
            {
                playerWaypoint = testWaypoint;
                path = BFS(currentWaypoint, playerWaypoint);
            }
        }

    }

    Queue<Waypoint> BFS(Waypoint start, Waypoint goal)
    {
        Queue<Waypoint> queue = new Queue<Waypoint>();
        Dictionary<Waypoint, Waypoint> cameFrom = new Dictionary<Waypoint, Waypoint>();
        queue.Enqueue(start);
        cameFrom[start] = null;

        while (queue.Count > 0)
        {
            Waypoint current = queue.Dequeue();
            if (current == goal) break;

            foreach (Waypoint neighbor in current.neighbors)
            {
                if (!cameFrom.ContainsKey(neighbor))
                {
                    queue.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                }
            }
        }

        Stack<Waypoint> reversePath = new Stack<Waypoint>();
        for (Waypoint at = goal; at != null; at = cameFrom[at])
        {
            reversePath.Push(at);
        }

        Queue<Waypoint> path = new Queue<Waypoint>();
        while (reversePath.Count > 0)
        {
            path.Enqueue(reversePath.Pop());
        }

        return path;
    }

    Waypoint FindClosestWaypoint(Vector3 position)
    {
        Waypoint[] waypoints = waypointGroup.GetComponentsInChildren<Waypoint>();
        Waypoint closest = null;
        float minSqrDist = Mathf.Infinity;

        foreach (Waypoint waypoint in waypoints)
        {
            float sqrDist = (position - waypoint.transform.position).sqrMagnitude;
            if (sqrDist < minSqrDist)
            {
                closest = waypoint;
                minSqrDist = sqrDist;
            }
        }

        return closest;
    }


    void UpdateCurrentWaypointToClosest()
    {
        if (currentWaypoint == null) return;

        Waypoint closest = currentWaypoint;
        float minSqrDist = (transform.position - currentWaypoint.transform.position).sqrMagnitude;

        foreach (Waypoint neighbor in currentWaypoint.neighbors)
        {
            float sqrDist = (transform.position - neighbor.transform.position).sqrMagnitude;
            if (sqrDist < minSqrDist)
            {
                closest = neighbor;
                minSqrDist = sqrDist;
            }
        }

        if (closest != currentWaypoint)
        {
            currentWaypoint = closest;
        }
    }

    void SpawnTendril()
    {
        if (tendrilOrigins.Count == 0 || tendrilPrefab == null) return;
        if (availableOrigins.Count == 0) return;

        // Pick random available origin
        int index = Random.Range(0, availableOrigins.Count);
        TendrilOrigin origin = availableOrigins[index];

        // Spawn the tendril as a child of the origin
        GameObject t = Instantiate(tendrilPrefab, origin.transform.position, origin.transform.rotation, origin.transform);
        TendrilBehavior tb = t.GetComponent<TendrilBehavior>();
        if (tb != null)
        {
            tb.Initialize(origin, this, false);  // Pass the origin and owner
            origin.activeTendril = tb;
        }

        // Remove the used origin from available list
        availableOrigins.RemoveAt(index);
    }

    void ForceLookAtPlayer()
    {
        Vector3 toPlayer = (player.transform.position - transform.position);
        if (toPlayer.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void CalculateDirection()
    {
        Vector3 displacement = transform.position - lastPosition;

        // Only update direction if moved more than threshold
        if (displacement.sqrMagnitude > directionUpdateThreshold * directionUpdateThreshold)
        {
            currentDirection = displacement.normalized;
            lastPosition = transform.position;
        }
    }

    void RotateTowardsDirection()
    {
        if (currentDirection.sqrMagnitude < 0.001f) return; // No direction to face

        Quaternion targetRotation = Quaternion.LookRotation(currentDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    bool CheckLineOfSight()
    {
        if (isAwake)
        {
            Vector3 origin = transform.position; // Offset if needed
            Vector3 dir = (player.transform.position - origin).normalized;
            float dist = Vector3.Distance(origin, player.transform.position);

            if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, barrierLayer))
            {
                // Hit something before the player
                return false;
            }
            return true;
        }
        else
        {
            return false;
        }
        
    }

    IEnumerator UpdateLineOfSight()
    {
        while (true)
        {
            hasLineOfSight = CheckLineOfSight();
            yield return new WaitForSeconds(0.25f);
        }
    }

    bool HasClearPathTo(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);
        return HasClearPath(direction, distance);
    }

    bool HasClearPath(Vector3 direction, float checkDistance)
    {

        float radius = 0.5f * transform.localScale.x; // Use the collider's radius if needed

        if (Physics.SphereCast(transform.position, radius, direction, out RaycastHit hit, checkDistance, barrierLayer))
        {
            // If we hit *anything* in the barrier layer, the path is NOT clear
            return false;
        }

        return true; // Nothing in the way at all
    }

    bool IsBetweenWaypoints(Vector3 a, Vector3 b, Vector3 position)
    {
        Vector3 ab = b - a;
        Vector3 ap = position - a;

        float abSqr = ab.sqrMagnitude;
        float dot = Vector3.Dot(ap, ab);

        // dot must be >= 0 and <= abSqr to be between the points
        return dot >= 0 && dot <= abSqr;
    }

    /// <summary>
    /// Resets the alien to its original position and state.
    /// </summary>
    public void ResetToStart()
    {
        Debug.Log("Alien Reset called");

        // Stop everything
        StopAllCoroutines();
        isAwake = false;
        isStunned = false;
        isCharging = false;
        isLunging = false;
        isChasingPlayer = false;

        // Reset cooldown to block Update logic for 0.1s
        resetCooldown = 0.2f;

        // Clear movement and reset position
        transform.position = startingWaypoint.transform.position;
        transform.rotation = initialRotation;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            //rb.MovePosition(startingWaypoint.transform.position);
        }

        // Reset AI/pathing state
        path.Clear();
        currentWaypoint = startingWaypoint;
        targetWaypoint = null;

        // Kill any tendrils
        foreach (var origin in tendrilOrigins)
        {
            if (origin.activeTendril != null)
            {
                origin.activeTendril.Retract();
                origin.activeTendril = null;
            }
        }

        availableOrigins.Clear();
        availableOrigins.AddRange(tendrilOrigins);

        audioSource.Stop();
        audioSource2.Stop();

        // Start line of sight tracking after a short delay
        StartCoroutine(DelayedWake());
    }

    IEnumerator DelayedWake()
    {
        yield return null; // wait 1 frame to ensure position is stable
        StartCoroutine(UpdateLineOfSight());
    }

    void OnDrawGizmosSelected()
    {
        // Only draw when selected in the editor
        Gizmos.color = Color.yellow;

        // Draw a forward-facing line from the transform
        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * 2f; // adjust length as needed
        Gizmos.DrawLine(start, end);

        // Wake distance (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wakeDistance);

        // Chase distance (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);
    }

}
