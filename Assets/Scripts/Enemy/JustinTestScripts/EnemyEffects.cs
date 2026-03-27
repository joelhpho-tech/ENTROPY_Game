using UnityEngine;

public class EnemyEffects : MonoBehaviour
{
    [Header("Effect Ranges")]
    //Closest
    [SerializeField] private float distance1;
    [SerializeField] private float distance2;
    //Farthest
    [SerializeField] private float distance3;

    [SerializeField] protected float intensity;

    [Header("References")]
    [SerializeField] private GameObject player;
    [SerializeField] private Material screenMaterials;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void CheckEffectDistance()
    {
        float currrentDistance = Vector3.Distance(transform.position, player.transform.position);

        if (currrentDistance > distance3)
        {
            return;
        }
        else if (currrentDistance <= distance3 && currrentDistance > distance2)
        {
            intensity = 0.25f;
        }
        else if (currrentDistance <= distance2 && currrentDistance > distance1)
        {
            intensity = 0.50f;
        }
        else if (currrentDistance <= distance1)
        {
            intensity = 1f;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Only draw when selected in the editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distance1);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distance2);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, distance3);
        // Draw a forward-facing line from the transform
        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * 2f; // adjust length as needed
        Gizmos.DrawLine(start, end);

        // Wake distance (yellow)
        Gizmos.color = Color.yellow;
        
    }
}
