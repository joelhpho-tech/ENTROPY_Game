using UnityEngine;

public class SceneTransitionTrigger : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;
    [SerializeField] private bool isEntryTrigger;

    void OnTriggerEnter(Collider other)
    {
        if (isEntryTrigger) return;
        if (!other.CompareTag("Player")) return;

        ZeroGravity zg = other.GetComponentInParent<ZeroGravity>();
        if(zg == null) { Debug.Log("ZeroGravity not found on player!"); return; }

        if (SceneLoader.Instance == null) { Debug.LogError("SceneLoader instance is null! Is it in the scene?"); return; }

        // Normalize offset by box size so it's world-position independent
        BoxCollider box = GetComponent<BoxCollider>();
        Vector3 localOffset = transform.InverseTransformPoint(other.transform.position);
        localOffset.x /= box.size.x;
        localOffset.y /= box.size.y;
        localOffset.z /= box.size.z;

        Vector3 velocity = zg.GetComponent<Rigidbody>().linearVelocity;
        Vector3 angularVelocity = zg.GetComponent<Rigidbody>().angularVelocity;
        Quaternion camRotation = zg.cam.transform.rotation;

        SceneLoader.Instance.LoadScene(sceneToLoad, localOffset, velocity, angularVelocity, camRotation);
    }
}
