using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.InputSystem;

public class Flashlight : MonoBehaviour
{
    [SerializeField]
    private GameObject player;
    [SerializeField]
    ZeroGravity zeroGPlayer;
    [SerializeField]
    private Transform holdPos;
    [SerializeField]
    private Camera cam;
    [SerializeField]
    private PlayerUIManager uiManager;

    private bool lookingAtFlashlight = false;

    [SerializeField]
    GameObject flashlightObjectParentedToPlayer;
    [SerializeField]
    GameObject flashlightObjectInScene;

    [SerializeField]
    private bool flashlightEquipped = false;
    private bool flashlightOn = false;

    #region Properties
    [SerializeField]
    public bool FlashlightEquipped
    {
        get { return flashlightEquipped; }
        set { flashlightEquipped = value;
            flashlightObjectParentedToPlayer.SetActive(value);
        }
    }
    // bool to check wether or not the player is looking at the flashlight in the scene,
    // if true the player can pick up the flashlight and use it.
    public bool LookingAtFlashlight
    {
        get { return lookingAtFlashlight; }
        set { lookingAtFlashlight = value; }
    }
    // property for the flashlight in the scene,
    //if true the flashlight will be destroyed in scene. 
    // the player instead will use the flashlight parented to ZeroGPlayer
    public bool HasFlashlightInScene
    {
        get { return !flashlightObjectInScene.activeSelf; }
        set { flashlightObjectInScene.SetActive(!value); }
    }

    #endregion

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //ensure the  flashlight parented to the player is off at the start of the game
        flashlightObjectParentedToPlayer.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void EquipFlashlightFromScene(InputAction.CallbackContext context)
    {
        if (LookingAtFlashlight 
            && !HasFlashlightInScene 
            && !FlashlightEquipped
            && context.performed)
        {
            //go through the lights of the scene object, match what ever their status is with the flashlightOn bool
            foreach (Light light in flashlightObjectInScene.GetComponentsInChildren<Light>())
            {
                flashlightOn = light.enabled;
                //Debug.Log("Light: " + flashlightOn);
            }
            HasFlashlightInScene = true; // disables scene flashlight
            flashlightEquipped = true;   // enables player flashlight
            //set the default of the flashlight of the lights of the player flashlight from the scene flashlight
            Debug.Log("Toggling flashlight " + flashlightOn);
            foreach (Light light in flashlightObjectParentedToPlayer.GetComponentsInChildren<Light>())
            {
                light.enabled = flashlightOn;
            }
            //Debug.Log("Equipping flashlight from scene|| HasFlashlightInScene: " + HasFlashlightInScene + " FlashlightEquipped: " + FlashlightEquipped);
            LookingAtFlashlight = false;
        }
    }

    public void EquipFlashlightFromInventory(InputAction.CallbackContext context)
    {
        if (HasFlashlightInScene && context.performed)
        {
            FlashlightEquipped = !FlashlightEquipped;
        }
        //Debug.Log("Equipping flashlight from inventory|| HasFlashlightInScene: " + HasFlashlightInScene + " FlashlightEquipped: " + FlashlightEquipped);

    }

    public void ToggleFlashlight(InputAction.CallbackContext context)
    {
        if (HasFlashlightInScene && FlashlightEquipped && context.performed)
        {
            flashlightOn = !flashlightOn;
            Debug.Log("Toggling flashlight" + flashlightOn);
            foreach (Light light in flashlightObjectParentedToPlayer.GetComponentsInChildren<Light>())
            {
                light.enabled = flashlightOn;
            }
        }
    }
}
