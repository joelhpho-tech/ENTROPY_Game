using UnityEngine;

public class AlienAI : MonoBehaviour
{
    [Header("Waypoint Setup")]
    public Waypoint currentNode;

    [Header("Movement Settings")]
    public float wanderSpeed = 3f;
    public float rotationSpeed = 5f;
    public float nodeReachThreshold = 0.5f;

    // State Machine
    public AlienStateMachine stateMachine { get; private set; }

    // States (just wander for now)
    public WanderState wanderState { get; private set; }

    // Current target node
    public Waypoint targetNode { get; set; }


    void Awake()
    {
        // Create state machine
        stateMachine = new AlienStateMachine();

        // Create wander state instance
        wanderState = new WanderState(this, stateMachine);
    }

    void Start()
    {
        // Start in wander state
        stateMachine.Initialize(wanderState);
    }

    void Update()
    {
        // Execute current state logic every frame
        stateMachine.currentState.FrameUpdate();
    }


    // === MOVEMENT HELPER METHODS ===

    public void MoveTowards(Vector3 targetPosition, float speed)
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );
    }

    public void RotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }


    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), "State: " + stateMachine.currentState.GetType().Name);
        if (targetNode != null)
        {
            GUI.Label(new Rect(10, 30, 300, 20), "Target: " + targetNode.name);
        }
    }
}
