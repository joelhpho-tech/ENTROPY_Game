using UnityEngine;

public class BandaidGeistAttach : MonoBehaviour
{

    public GameObject geistRigPrefab;

    private GameObject geistRig;
    [SerializeField]
    private Transform[] children;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        geistRig = Instantiate(geistRigPrefab, Vector3.zero, Quaternion.identity, null);

        children = geistRig.GetComponentsInChildren<Transform>();

        // holy bandaid
        children[1].parent = this.transform;
        children[1].transform.localPosition = Vector3.zero;
        children[1].transform.localRotation = Quaternion.identity;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
