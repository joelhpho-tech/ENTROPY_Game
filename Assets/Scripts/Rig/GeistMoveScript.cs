using UnityEngine;

public class GeistMoveScript : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float moveRange = 3f;
    public float rotateSpeed = 45f;

    private Vector3 _startPos;

    void Start()
    {
        _startPos = transform.position;
    }

    void Update()
    {
        float t = Time.time;

        // Bob up and down, drift left and right
        transform.position = _startPos + new Vector3(
            Mathf.Sin(t * moveSpeed * 0.7f) * moveRange,
            Mathf.Sin(t * moveSpeed * 0.5f) * moveRange * 0.5f,
            Mathf.Cos(t * moveSpeed * 0.4f) * moveRange
        );

        // Slowly rotate on all axes
        transform.rotation = Quaternion.Euler(
            Mathf.Sin(t * 0.5f) * 30f,
            t * rotateSpeed % 360f,
            Mathf.Sin(t * 0.3f) * 20f
        );
    }
}
