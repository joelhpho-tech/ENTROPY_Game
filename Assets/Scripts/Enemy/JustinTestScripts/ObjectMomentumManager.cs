using UnityEngine;

public class ObjectMomentumManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PickupScript pickupScript;

    [Header("Item Tracker")]
    public GameObject lastGrabbedObject;
    public float grabbedObjectMomentum;
    public float momentumThreshhold; 
    public bool collidedWithWall = false;
    public bool collidedWithGeist = false;
    public bool isMoving = false;

    private void Update()
    {
        
    }

    private void CheckIfObjectCollided()
    {
        if (!collidedWithWall)
        {
            
        }
    }

    private bool CheckIfFastEnough()
    {
        
        if (grabbedObjectMomentum >= momentumThreshhold && isMoving)
        {
            return true;
        }
        return false;
    }
}
