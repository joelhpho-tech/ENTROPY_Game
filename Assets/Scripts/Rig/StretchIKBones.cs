using UnityEngine;
using System.Collections.Generic;

public class StretchIKBones : MonoBehaviour
{
    [SerializeField]
    private float MAX_DISTANCE = 2f;
    private float stretchFactor = 1f;

    [SerializeField]
    private Transform root;
    [SerializeField]
    private Transform target;

    private Vector3 previousPosition;
    private Quaternion previousRotation;

    private List<Transform> bones;
    private Vector3[] initPositions;

    [SerializeField]
    private float distance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bones = GetBoneChain(root);

        initPositions = new Vector3[bones.Count];
        for (int i = 0; i < bones.Count; i++)
            initPositions[i] = bones[i].localPosition;


        distance = Vector3.Distance(root.position, target.position); ;
        previousPosition = target.position;
        previousRotation = target.rotation;
       
    }


    void LateUpdate()
    {

       
        distance = Vector3.Distance(root.position, target.position);
        stretchFactor = distance > MAX_DISTANCE ? distance / MAX_DISTANCE : 1f;
        previousPosition = target.position;
        

        for (int i = 1; i < bones.Count; i++)
            bones[i].localPosition = initPositions[i] * stretchFactor;
    }

    List<Transform> GetBoneChain(Transform root)
    {
        List<Transform> chain = new List<Transform>();
        Transform current = root;

        while (current != null)
        {
            chain.Add(current);
            current = current.childCount > 0 ? current.GetChild(0) : null;
        }

        return chain;
    }
}
