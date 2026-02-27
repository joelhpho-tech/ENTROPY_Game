using UnityEngine;

public class FloatingObject : MonoBehaviour
{

    Rigidbody rb;
    Collider collider;
   

    [SerializeField]
    bool useDirectionInput = false;
    [SerializeField]
    Vector3 initDirection;

    [SerializeField]
    bool useSpeedInput = false;
    [SerializeField]
    float initSpeed = 100f;

    [SerializeField]
    float randomSpeedLower = 25f;
    [SerializeField]
    float randomSpeedUpper = 100f;

    [SerializeField]
    bool applyManualTorque = false;
    [SerializeField]
    Vector3 initTorque;

    [SerializeField]
    bool useFriction = false;
    [SerializeField]
    float minimumSpeed = 0.3f;

    [SerializeField]
    PhysicsMaterial frictionMaterial;
    [SerializeField]
    PhysicsMaterial noFrictionMaterial;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = this.gameObject.GetComponent<Rigidbody>();
        collider = this.gameObject.GetComponent<Collider>();


        if (useFriction)
        {
            collider.sharedMaterial = noFrictionMaterial;
        }

        if (!useSpeedInput)
        {
            initSpeed = Random.Range(randomSpeedLower, randomSpeedUpper);
        }
        
        // very basic scaling: more mass = less speed
        initSpeed = initSpeed / rb.mass;

        // if no manual input direction, randomly create direction
        if (!useDirectionInput)
        {
            initDirection = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
                );

            initDirection.Normalize();

        }

        // add initial forces
        if (applyManualTorque)
        {
            rb.AddTorque(initTorque * (initSpeed));
        }
        else
        {
            rb.AddTorque(initDirection * (initSpeed * 0.1f));
        }
        
        rb.AddForce(initDirection * initSpeed);


    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!useFriction == !rb.isKinematic && rb.linearVelocity.magnitude <= minimumSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * minimumSpeed;

        }
                

    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position;
        Vector3 dir = initDirection.normalized * 0.8f; // scale for visibility
        if (useDirectionInput)
        {
            Gizmos.DrawLine(origin, origin + dir);
        }
        
        
    }
}
