using UnityEngine;

public class EnemyEyeController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyStateMachine enemyStateMachine;
    [SerializeField] private Material eyeMaterial;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject eyeHolder;
    [SerializeField] private GameObject geistBody;

    [SerializeField] private float rotationSpeed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FollowBody()
    {
        transform.position = geistBody.transform.position;
    }

    private void RotateEye(Vector3 lookDirection)
    {
        Vector3 direction = (lookDirection - transform.position).normalized;

        if (lookDirection == Vector3.zero)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime); ;
    }

    private void ChangeEyeCollor(Color newColor)
    {
        eyeMaterial.SetColor("color 1",newColor);
    }
}
