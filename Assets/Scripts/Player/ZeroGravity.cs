using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI.Table;

public class ZeroGravity : MonoBehaviour, ISaveable
{
    #region Inspector Variables
    [Header("== Player Elements ==")]
    [SerializeField]
    private Rigidbody rb;
    [SerializeField]
    private CapsuleCollider boundingSphere;
    [SerializeField]
    public Camera cam;

    //look sensitivity
    [SerializeField]
    private float sensitivityX = 8.0f;
    [SerializeField]
    private float sensitivityY = 8.0f;

    //rotation variables to track how the camera is rotated
    private float rotationHoriz = 0.0f;
    private float rotationVert = 0.0f;
    private float rotationZ = 0.0f;

    //respawn reference
    public GameObject respawnLoc;

    //player health trackers
    [SerializeField]
    private int playerHealth = 4;
    [SerializeField]
    private int maxHealth = 4;

    [SerializeField]
    private int numStims = 0;
    [SerializeField]
    private int maxNumStim = 3;
    private bool usingStimCharge = false;


    private bool isDead = false;
    //win tracker
    private bool didWin = false;
    [SerializeField]
    private Collider end;

    //handle taking damage
    private bool justHit = false;
    private bool prevJustHit = false;
    [SerializeField]
    private float justHitCoolDown = .6f;
    private float justHitTimeStamp = 0f;

    //health indicator
    private bool prevHurt = false;
    [SerializeField]
    private float hurtCoolDown = 3.5f;
    [SerializeField]
    private float highDangerCoolDown = 5.0f;

    //access permissions
    [SerializeField]
    private bool[] accessPermissions = new bool[3];

    // Give the player a reference to the wrist monitor for saving
    [SerializeField]
    private WristMonitor wristMonitor;

    [Header("== Player Movement Settings ==")]
    [SerializeField]
    private float rollTorque = 250.0f;
    [SerializeField]
    private float maxRollSpeed = 75f;
    private float currentRollSpeed = 0f;
    [SerializeField]
    private float rollAcceleration = 10f; // How quickly it accelerates to rollTorque
    [SerializeField]
    private float deathRollAcceleration = 3f;
    private float rollFriction = 5f; // How quickly it decelerates when input stops
    //values for the roll friction depending on grabbing onto bars vs not
    [SerializeField]
    private float rollFrictionGrab = 50f;
    [SerializeField]
    private float rollFrictionNoGrab = 10f;
    private float prevRotZ = 0f;
    [SerializeField]
    private float bounceAcc = 10f;

    [Header("Audio")]
    public BarAudioHandler barAudioHandler;

    [Header("== Grabbing Settings ==")]
    // Grabbing mechanic variables
    private bool isGrabbing = false;
    private bool justGrabbed = false;
    private bool prevJustGrabbed = false;

    public Collider potentialGrabbedBar = null; //tracks a potential grabbable bar that the player looks at
    public Collider grabbedBar; //stores the bar the player is currently grabbing
    // the position of the current bar where the player is grabbing
    private Vector3 currentGrabPosition;
    [SerializeField]
    private float grabRange = 3f; // Range within which the player can grab bars
    [SerializeField]
    private float minGrabRange = 1f;
    [SerializeField]
    private float grabPadding = 50f;
    //Propel off bar 
    [SerializeField]
    private float propelThrust = 50000f;

    [Header("== Push Off Wall Settings ==")]
    [SerializeField]
    private float propelOffWallThrust = 50000f;
    private Transform potentialWall = null;

    //fields for speed maxes for hit detection
    [SerializeField]
    private float pushSpeed = 1.5f;
    [SerializeField]
    private float dangerSpeed = 10f;
    [SerializeField]
    private float mediumSpeed = 5f;
    [SerializeField]
    private float zeroGWalkSpeed = 3f;
    [SerializeField]
    private float minimumSpeed = 1f;

    [Header("== Swinging Settings==")]
    [SerializeField]
    private bool swinging = false;
    private Vector3 swingPoint; //stores the bar transform when calculating swings
    private SpringJoint joint;
    [SerializeField]
    private float grabDrag = .99f;
    [SerializeField]
    private float pullDrag = .95f;
    [SerializeField]
    private float pullToBarMod = 1f;
    [SerializeField]
    private float jointSpringForce = 4.5f;
    [SerializeField]
    private float jointDamperForce = 7f;
    [SerializeField]
    private float jointMassScale = 4.5f;


    //time stamps for swing cooldowns
    private float grabSwingTimeStamp;
    [SerializeField]
    private float swingCoolDownFastest = 4f;
    [SerializeField]
    private float swingCoolDownMedium = 2f;
    [SerializeField]
    private float swingCoolDownSlowest = 1f;


    [Header("== UI Settings ==")]
    [SerializeField]
    PlayerUIManager uiManager;
    private bool showTutorialMessages = true;

    //Input Values
    [SerializeField]
    private InputActionReference grab;
    [SerializeField]
    private InputActionReference pushOffWall;
    private float thrust1D;
    private float strafe1D;
    private float upandDown1D;


    [Header("== World Element Managers ==")]
    [SerializeField]
    private TutorialManager tutorialManager;
    [SerializeField]
    private EnemyManager enemyManager;
    [SerializeField]
    private PlayerAudio playerAudio;

    [Header("== IK Logic ==")]
    [SerializeField]
    bool useIK = false;
    private Quaternion[] initGrabRotation;
    private Vector3[] initGrabPosition;
    private Vector3[] initUpVector;
    private Transform grabHolder;

    [SerializeField]
    private Transform[] defaultHandPosition;
    [SerializeField]
    private TwoBoneIKConstraint[] hands;
    [SerializeField]
    private RigBuilder rigBuilder;
    [SerializeField]
    private Animator animator;

    [Header("== PLayer Movement Restriction Bools ==")]
    //used for freezing the camera movement while completing the puzzle.
    private bool canMove = true;

    //Fields for the tutorial
    [SerializeField]
    private bool tutorialMode = false;
    private bool canGrab = false;
    private bool canPropel = false;
    private bool canPushOff = false;
    private bool canRoll = false;
    private bool hasPropelled = false;
    private bool hasRolled = false;

    // Track if the movement keys were released
    private bool movementKeysReleased;

    [SerializeField] private bool useManualPullIn = false;
    private bool isPullingIn;

    //determines if the player is able to roll mid air or not
    [SerializeField]
    private bool onlyRollOnGrab = false;
    private bool hasUsedStim = false;
    private float totalRotation;

    // these will prevent rapid door collisions by providing a delay
    private int maxDoorCollisionCooldownFrames = 30;

    private int doorCollisionCooldownFrames = 0;

    // for storing the respawn information
    public PlayerData playerData;
    #endregion 

    #region Properties
    //Properties
    //this property allows showTutorialMessages to be assigned outside of the script. Needed for the tutorial mission

    #region devToolsProperties
    public bool GodMode { get; set; } = false;

    private bool playerFreeMoveNoClip = false;
    public bool PlayerFreeMoveNoClip
    {
        get { return playerFreeMoveNoClip; }
        set
        {
            if (playerFreeMoveNoClip == value) return;
            playerFreeMoveNoClip = value;

            if (!value)
            {
                //Debug.Log("NOCLIP OFF - isKinematic before: " + rb.isKinematic);
                rb.isKinematic = false;
                boundingSphere.enabled = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                //Debug.Log("NOCLIP OFF - isKinematic after: " + rb.isKinematic);
            }
            else
            {
                //Debug.Log("NOCLIP ON - isKinematic before: " + rb.isKinematic);
                ReleaseBar(); // ensure we clean up any grabbed bar states/ break those dependencies
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                boundingSphere.enabled = false;
                //Debug.Log("NOCLIP ON - isKinematic after: " + rb.isKinematic);
            }
        }
    }
    public float NoClipMoveSpeed { get; set; } = 10f;

    #endregion

    #region Other Properties
    public float GrabPadding
    {
        get { return grabPadding; }
    }

    public bool ShowTutorialMessages
    {
        get { return showTutorialMessages; }
        set { showTutorialMessages = value; }
    }

    public Collider PotentialGrabbedBar
    {
        get { return potentialGrabbedBar; }
        set { potentialGrabbedBar = value; }
    }

    public Collider GrabbedBar
    {
        get { return grabbedBar; }
    }

    public Vector3 CurrentGrabPosition
    {
        get { return currentGrabPosition; }
        set { currentGrabPosition = value; }
    }

    public Transform PotentialWall
    {
        get { return potentialWall; }
        set { potentialWall = value; }
    }

    public bool TutorialMode
    {
        get { return tutorialMode; }
        set { tutorialMode = value; }
    }

    public bool CanMove
    {
        get { return canMove; }
        set { canMove = value; }
    }

    public bool CanGrab
    {
        get { return canGrab; }
        set { canGrab = value; }
    }

    public bool CanPropel 
    { 
        get { return canPropel; } 
        set { canPropel = value; }
    }

    public bool CanPushOff 
    { 
        get { return canPushOff; }
        set { canPushOff = value; }
    }

    public float PushSpeed
    {
        get { return pushSpeed; }
    }

    public bool CanRoll 
    { 
        get { return canRoll; }
        set { canRoll = value; }
    }

    public bool HasPropelled 
    { 
        get { return hasPropelled; }
        set { hasPropelled = value; }
    }

    public bool HasRolled
    {
        get { return hasRolled; }
        set { hasRolled = value; }
    }

    public float TotalRotation
    {
        get { return totalRotation; }
        set { totalRotation = value; }
    }

    public int PlayerHealth { get { return playerHealth; } }

    public int MaxHealth { get { return maxHealth; } }

    public int NumStims { get { return numStims; } }

    public bool IsDead
    {
        get { return isDead; }
        set {  isDead = value; }
    }

    public float SensitivityX
    {
        get { return sensitivityX; }
        set { sensitivityX = value; }
    }

    public float SensitivityY
    {
        get { return sensitivityY; }
        set { sensitivityY = value; }
    }

    public float GrabRange
    {
        get { return grabRange; }
        set { grabRange = value; }

    }

    public bool[] AccessPermissions
    {
        get { return accessPermissions; }
       
    }

    // getter for isGrabbing
    public bool IsGrabbing => isGrabbing;
    #endregion

    #endregion

    private void Awake()
    {
        //set the player health
        playerHealth = 3;
        //make sure there are no stims in teh plaeyr inventory
        numStims = 0;
        rb.useGravity = false;
        //hard code isKinematic is false to ensure the No-Clip logic works and doesn't force the player into kinematic when they shouldn't be
        rb.isKinematic = false;
        boundingSphere.enabled = true;
        cam = Camera.main;
    }

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 120;  // match this with your build target frame rate.

        // give player default permissions

        //initial player booleans set if in tutorial mode
        if (tutorialMode)
        {
            canGrab = false;
            canPushOff = false;
            canPropel = false;
            canRoll = false;
        }
        //not in tutorial mode
        else
        {
            canGrab = true;
            canPushOff = true;
            canPropel = true;
            canRoll = true;
        }

        isDead = false;
        justHit = false;
        prevJustHit = false;
        prevHurt = false;


        //let the mouse move
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        

        if(useManualPullIn)
        {
            pullDrag = 0.9f;
            grabDrag = 0.1f;
            jointSpringForce = 5.5f;
        }
        // continue from save
        if (GlobalSaveManager.LoadFromSave) GlobalSaveManager.LoadSavable(this, false);
    }

    // Update is called once per frame
    #region Update Methods
    void FixedUpdate()
    {
        if (PlayerFreeMoveNoClip)
        {
            FreeMoveNoClip();
            return;
        }
        // if the player is allowed to move
        if (canMove && !PlayerFreeMoveNoClip)
        {
            //handle grabber icon logic
            if (canGrab)
            {

                //handle the grab movement
                if (isGrabbing)
                {
                    HandleGrabMovement(grabbedBar);
                }
                else if (!isGrabbing)
                {
                    uiManager.UpdateGrabberPosition(potentialGrabbedBar);
                }
            }
            //allow the player to bounce off the barriers
            DetectBarrierAndBounce();
            //take damage from door closing on the player
            if (doorCollisionCooldownFrames <= 0)
            {
                DetectClosingDoorTakeDamageAndBounce();
            } else
            {
                doorCollisionCooldownFrames -= 1;
            }
            JustHitCoolDown();

            if (isDead)
            {
                //apply the roll rotation to the camera
                cam.transform.Rotate(Vector3.forward * -1f * deathRollAcceleration);
            }
        }
    }

    void Update()
    {
        if (canMove)
        {
            RotateCam();
        }
    }
    #endregion

    #region Player Control Methods

    #region Camera Methods
    private void RotateCam()
    {

        float rotateAngleX = rotationHoriz * sensitivityX;
        float rotateAngleY = -rotationVert * sensitivityY;

        // Horizontal and vertical rotation
        cam.transform.Rotate(Vector3.up, rotateAngleX * Time.deltaTime);
        cam.transform.Rotate(Vector3.right, rotateAngleY * Time.deltaTime);
        //Debug.Log("previous rotation: " + prevRotZ);
        //Debug.Log("rotationZ " + rotationZ + "// PrevRotZ" + prevRotZ);


        // Apply roll rotation (Z-axis)
        if (canRoll)
        {
            //determine the friction of teh roll depending on if the player is grabbing a bar or not
            //if the player is grabbing a bar
            if (isGrabbing)
            {
                //the roll friction is higher
                rollFriction = rollFrictionGrab;
            }
            //if the player is not grabbing a bar
            else if (!isGrabbing && !onlyRollOnGrab)
            {
                //the roll friction is less
                rollFriction = rollFrictionNoGrab;
            }

            //if this is the initial roll input
            //if (prevRotZ < 0.1f)
            //{
            //    //set the current roll speed to 0 to ensure direct input take over
            //    currentRollSpeed = 0f;
            //    //Debug.Log("first time hitting roll");
            //}
            if (Mathf.Abs(rotationZ) > 0.1f) //only apply roll if rotationZ input is significant
            {
                if (prevRotZ != rotationZ)
                {
                    //Debug.Log("It's happening");
                    currentRollSpeed = Mathf.MoveTowards(currentRollSpeed, 0f, rollFriction * Time.deltaTime);
                }
                //Debug.Log("applying roll");
                //calculate target roll direction and speed based on input
                float targetRollSpeed = -Mathf.Sign(rotationZ) * rollTorque * rollFriction;

                //gradually increase currentRollSpeed towards targetRollSpeed
                currentRollSpeed = Mathf.MoveTowards(currentRollSpeed, targetRollSpeed, rollAcceleration * Time.deltaTime);

                //update the previous roll input
                prevRotZ = rotationZ;
            }
            else if (Mathf.Abs(rotationZ) < 0.1f) // Apply friction when no input
            {
                //Debug.Log("applying roll friction");
                //gradually decrease currentRollSpeed towards zero
                currentRollSpeed = Mathf.MoveTowards(currentRollSpeed, 0f, rollFriction * Time.deltaTime);
            }
            //check for a reasonable max roll speed
            //Debug.Log(currentRollSpeed);

            //ensure the roll is capped at 100 and -100 so the player doesn't gain speed past this in the roll
            if (currentRollSpeed > maxRollSpeed)
            {
                currentRollSpeed = maxRollSpeed;
            }
            else if (currentRollSpeed < -maxRollSpeed)
            {
                currentRollSpeed = -maxRollSpeed;
            }

            //apply the roll rotation to the camera
            if (!onlyRollOnGrab)
            {
                cam.transform.Rotate(Vector3.forward, currentRollSpeed * Time.deltaTime);
            }
            else if (onlyRollOnGrab && isGrabbing)
            {
                cam.transform.Rotate(Vector3.forward, currentRollSpeed * Time.deltaTime);
            }
            if (tutorialManager != null && tutorialManager.inTutorial)
            {
                // inside RotateCam, after applying roll
                float deltaRoll = currentRollSpeed * Time.deltaTime;
                totalRotation += deltaRoll;
                if (totalRotation > 360)
                {
                    totalRotation = 0;
                }
                if (totalRotation < -360)
                {
                    totalRotation = 0;
                }
                //Debug.Log(totalRotation);
            }
        }
    }

    public void StopRollingQuickly()
    {
        // Immediately stop applying input-based roll
        rotationZ = 0f;

        // Reduce current roll speed quickly toward 0
        StartCoroutine(QuickRollBrake());
    }

    private IEnumerator QuickRollBrake()
    {
        while (Mathf.Abs(currentRollSpeed) > 0.1f)
        {
            currentRollSpeed = Mathf.MoveTowards(currentRollSpeed, 0f, rollFriction * 5f * Time.deltaTime);
            yield return null;
        }

        currentRollSpeed = 0f; // Snap to 0 at the end
    }
    #endregion

    #region Dev Tool Methods
    /// <summary>
    /// This is a dev tool that switches the player from gameplay, grab and pull movement -> free movement with no clip
    /// Allowing for devs to freely move within the scene without needing to use the mechanics,
    /// just clean WASD ; forward, left, back, right
    /// SPACE, up
    /// C, down
    /// </summary>
    private void FreeMoveNoClip()
    {
        //ensure we can't use this logic if the PlayerFreeMoveNoClip boolean is not set to true
        if (!PlayerFreeMoveNoClip) return;

        //establish the forward vector
        Vector3 moveDirection = Vector3.zero;

        // Forward and back leveraging the thrust1D we alr created for the zero-g movement
        moveDirection += cam.transform.forward * thrust1D;
        // Left and Right leveraging strafe1D
        moveDirection += cam.transform.right * strafe1D;
        // Up and Down leverage upandDown1D
        moveDirection += cam.transform.up * upandDown1D;

        // directly set the position based on the moveDirection we calculated
        transform.position += moveDirection.normalized * NoClipMoveSpeed * Time.fixedDeltaTime;
    }

    #endregion

    #region Zero-Gravity Methods

    #region Grab and Propel Movement
    public void PlayerCutSceneHandler(bool inCutScene)
    {
        if (inCutScene)
        {
            uiManager.HideInteractables();
            uiManager.HideGrabber();
            ReleaseBar();
            rb.linearVelocity = Vector3.zero;
            canGrab = false;
            canPushOff = false;
            canPropel = false;
            //canRoll = false;
            //uiManager.Crosshair.sprite = null;
            uiManager.Crosshair.color = new Color(0f, 0f, 0f, 0f);
        }
        else if (!inCutScene)
        {
            //Debug.Log("running player cutscene handler");
            canGrab = true;
            canPushOff = true;
            canPropel = true;
            canRoll = true;
            //uiManager.Crosshair.sprite = uiManager.CrosshairIcon;
            uiManager.Crosshair.color = new Color(1f, 1f, 1f, 1f);

        }
    }

    /// <summary>
    /// Simple method that only allows player to propel off a bar if they are currently grabbing it
    /// </summary>
    /// <param name="horizontalAxisPos"></param>
    /// <param name="verticalAxisPos"></param>
    private void HandleGrabMovement(Collider bar)
    {
        //Propel off bar logic
        if (isGrabbing && bar != null)
        {
            PropelOffBar();
            Swing(bar.transform);
            uiManager.UpdateGrabberPosition(bar);
        }
    }

    public void GrabBar()
    {
        //Put lines 962-954 in if statement to only call if bar is ACTUALLY being grabbed

        //isGrabbing = true;
        //set just grabbed to true to send to swing cool down
        //justGrabbed = true;

        //Updated to only actually call if bar is being grabbed
        if (potentialGrabbedBar == null || isGrabbing) 
            return;
        
        isGrabbing = true;
        justGrabbed = true;
        grabbedBar = potentialGrabbedBar;

        //Debug.Log(grabbedBar.name);


        if (barAudioHandler != null)
        {
            barAudioHandler.PlayGrabSound(grabbedBar.transform.position);
        }
      

        //grabbedBar = potentialGrabbedBar;



        //// set up grab spots
        //if (useIK)
        //{
        //    GetBarGrabbers();
        //    MoveArmsToBar();
        //}

        //lock grabbed bar and change icon

        //swing set to true and sent to the cooldowns
        swinging = true;

        //Debug.Log(rb.linearVelocity.magnitude);
    }

    public void NewSwing(Transform bar)
    {
        if(isGrabbing && bar != null)
        {
            swingPoint = currentGrabPosition;

            //ensure we don't have a joint created yet for swinging
            if (this.gameObject.GetComponent<ConfigurableJoint>() == null)
            {
                //create the joint
                this.gameObject.AddComponent<ConfigurableJoint>();
            }
        }
    }

    public void NewPullToBar(float multiplier, Transform bar)
    {
        //Debug.Log(bar.gameObject.name);
        //Debug.Log(rb.linearVelocity.magnitude);
        //Debug.Log(bar.gameObject.name);
        //initially set the velocity to 0 so the momentum doesn't carry through from propel

        if (useManualPullIn && !isPullingIn)
            return;

        //if the joint is a long distance between the player and the bar
        if (joint.maxDistance >= joint.minDistance)
        {
            //decrease the length of the joint scaled by a multiplier to determine how fast this happens
            joint.maxDistance -= 0.1f * multiplier;
            //lessen the spring force of the joint scaled by a multiplier to determine how fast this happens
            joint.spring -= 0.1f * multiplier;
        }

        //increment down the linear and angular velocities so the player slows down
        if (rb.linearVelocity.magnitude >= zeroGWalkSpeed)
        {
            //decrease the velocity
            rb.linearVelocity *= grabDrag;
        }
        //if the linear velocity magnitude is below 3  
        else if (rb.linearVelocity.magnitude < zeroGWalkSpeed)
        {
            //create a target Transform to pull to
            //iterate through the children 
            foreach (Transform child in bar.transform)
            {
                //find the child that is the GrabTarget
                if (child.gameObject.name == "GrabTarget")
                {
                    // this will only call once as bars only have 1 grab target, therefor it is safe and efficient to do physics here
                    //save this child as the target
                    Vector3 target = new Vector3(child.position.x, child.position.y, child.position.z);
                    // align the target to the grab position
                    // first I will calculate the difference in the bars position and the current grab position
                    Vector3 grabOffset = currentGrabPosition - bar.transform.position;
                    // then I will add the grab offset to the target
                    target += grabOffset;
                    //if the position of the player and the target are about equal
                    if (Vector3.Distance(rb.transform.position, target) < .1f)
                    {
                        //Debug.Log("They are touching :)");
                        //begin the swing ability
                        Swing(bar);
                        return;
                    }
                    else
                    {
                        //create a direction vector to pull the player to the bar point
                        Vector3 pullDirection = target - rb.transform.position;
                        Vector3 normalizedpulldirection = pullDirection.normalized;
                        rb.AddForce(normalizedpulldirection * multiplier, ForceMode.VelocityChange);
                    }
                }
            }
        }

        //Debug.Log("linear velocity: " + rb.linearVelocity.magnitude);
    }

    /// <summary>
    /// This method is created to allow player to swing on the bars, similar to a grappling hook feature found in other games
    /// It creates a sringjoint between the player and the bar the length of the players arm
    /// </summary>
    /// <param name="bar"></param>
    private void Swing(Transform bar)
    {
        if (isGrabbing && bar != null)
        {
            //Debug.Log("swingaling");
            swingPoint = currentGrabPosition;

            //ensure that the player isn't alr swinging on another bar
            if (this.gameObject.GetComponent<SpringJoint>() == null)
            {
                joint = this.gameObject.AddComponent<SpringJoint>();
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedAnchor = swingPoint;
                float distanceFromPoint = Vector3.Distance(cam.transform.position, swingPoint);

                //disable the collision with bar when grabbing
                joint.enableCollision = false;

                //ensure the max and min distances are set properly
                if(!useManualPullIn)
                {
                    joint.maxDistance = grabRange;
                    joint.minDistance = minGrabRange;
                }
                else
                {
                    joint.maxDistance = 1.0f;
                    joint.minDistance = 0.5f;
                }
                
            }

            //tweak these values for the spring for better pendulum values
            joint.spring = jointSpringForce; //higher pull and push of the spring
            joint.damper = jointDamperForce;
            joint.massScale = jointMassScale;

            //if the player is not swinging
            if (!swinging)
            {
                rb.linearVelocity *= pullDrag;
                //pull to the bar
                if (!useManualPullIn || isPullingIn)
                {
                    PullToBar(pullToBarMod, bar);
                }

                return;
            }
            //swing cooldown
            if (!useManualPullIn)
            {
                SwingCoolDown(bar);
            }

            //ensure that the player will bounce off the wall
            DetectBarrierAndBounce();
        }
    }

    /// <summary>
    /// This method will take the joint created by the Swing method and will incrimentally 
    /// shrink it as the player continues to hold onto one bar. This will eventually get short enough to 
    /// where the player is able to stop themself, and then propel with no swing affecting their trajectory
    ///try to make the target point calculated in front of the player to the bar. therefore won't have issues with collisions too close to the wall 
    /// </summary>
    private void PullToBar(float multiplier, Transform bar)
    {
        //Debug.Log(bar.gameObject.name);
        //Debug.Log(rb.linearVelocity.magnitude);
        //Debug.Log(bar.gameObject.name);
        //initially set the velocity to 0 so the momentum doesn't carry through from propel

        if (useManualPullIn && !isPullingIn)
            return;

        //safeguard so that the player can't pull in and then release in the tutorial mode before they are able to propel
        if(tutorialMode && !canPropel)
        {
            return;
        }

        //if the joint is a long distance between the player and the bar
        if (joint.maxDistance >= joint.minDistance)
        {
            //decrease the length of the joint scaled by a multiplier to determine how fast this happens
            joint.maxDistance -= 0.1f * multiplier;
            //lessen the spring force of the joint scaled by a multiplier to determine how fast this happens
            joint.spring -= 0.1f * multiplier;
        }

        //increment down the linear and angular velocities so the player slows down
        if (rb.linearVelocity.magnitude >= zeroGWalkSpeed)
        {
            //decrease the velocity
            rb.linearVelocity *= grabDrag;
        }
        //if the linear velocity magnitude is below 3  
        else if (rb.linearVelocity.magnitude < zeroGWalkSpeed)
        {
            //create a target Transform to pull to
            //Debug.Log(bar.name);
            foreach (Transform child in bar)
            {
                //find the child that is the GrabTarget
                if(child.gameObject.name == "GrabTarget")
                {
                    // this will only call once as bars only have 1 grab target, therefor it is safe and efficient to do physics here
                    //save this child as the target
                    Vector3 target = new Vector3(child.position.x, child.position.y, child.position.z);
                    // align the target to the grab position
                    // first I will calculate the difference in the bars position and the current grab position
                    Vector3 grabOffset = currentGrabPosition - bar.transform.position;
                    // then I will add the grab offset to the target
                    target += grabOffset;
                    //if the position of the player and the target are about equal
                    if (Vector3.Distance(rb.transform.position, target) < .1f)
                    {
                        //Debug.Log("They are touching :)");
                        rb.linearVelocity = Vector3.zero;
                        return;
                    }
                    else
                    {
                        //create a direction vector to pull the player to the bar
                        Vector3 pullDirection = target - rb.transform.position;
                        Vector3 normalizedpulldirection = pullDirection.normalized;
                        rb.AddForce(normalizedpulldirection * multiplier, ForceMode.VelocityChange);

                    }
                }
            }
            //Debug.Log(target.gameObject.name);
        }

        //Debug.Log("linear velocity: " + rb.linearVelocity.magnitude);
    }
    /// <summary>
    /// This method will control the logic for the cooldown of swinging before the player 
    /// automatically starts gtting pulled into the bar to stop their movement while still holding the bar
    /// </summary>
    private void SwingCoolDown(Transform bar)
    {
        //confirm I have just grabbed a bar
        if (isGrabbing && justGrabbed && !prevJustGrabbed)
        {
            //Debug.Log("Linear velocity: " + rb.linearVelocity.magnitude);
            //create time stamps based on how fast the player is moving
            //if the player is moving at the dangerous speed
            if(rb.linearVelocity.magnitude >= mediumSpeed)
            {
                //Debug.Log("Danger Speed Reached");
                //the cooldown for swinging will be higher
                grabSwingTimeStamp = Time.time + swingCoolDownSlowest;
                //Debug.Log("medium Time Stamp: " + grabSwingTimeStamp + "TimeStampCurrent: " + Time.time);
            }
            //if the player is moving at a slower speed
            else if(rb.linearVelocity.magnitude >= zeroGWalkSpeed)
            {
                //Debug.Log("Normal Speed Reached");
                //the cooldown will be lower
                grabSwingTimeStamp = Time.time + swingCoolDownMedium;
                //Debug.Log("space walk Time Stamp: " + grabSwingTimeStamp + "TimeStampCurrent: " + Time.time);
            }
            //if the player is moving slower than the benchmark
            else if(rb.linearVelocity.magnitude < zeroGWalkSpeed)
            {
                grabSwingTimeStamp = Time.time + swingCoolDownFastest;
            }
            //set the prev just grabbed bool to confirm we do this once 
            prevJustGrabbed = justGrabbed;
        }
        //if the time has now gone past the cooldown timestamp we created
        if(Time.time > grabSwingTimeStamp && bar != null)
        {
            justGrabbed = false;
            swinging = false;
            prevJustGrabbed = justGrabbed;
            grabSwingTimeStamp = 0f;
        }
    }

    /// <summary>
    /// This method stops the swinging by destroying the pringjoint and setting the swingpoint back to zero
    /// </summary>
    private void StopSwing()
    {
        //Debug.Log("no swingaling");
        swingPoint = Vector3.zero;
        Destroy(joint);
        swinging = false;
    }


    // Release the bar and enable movement again
    private void ReleaseBar()
    {
        //stop swinging off the bar
        StopSwing();

        if (!isGrabbing) 
            return;

        //set isGrabbing to false
        isGrabbing = false;
        //set justgrabbed to false to send for the cool down to nullify
        justGrabbed = false;

        Vector3 barLoc = grabbedBar.transform.position;

        grabbedBar = null;

        

        // no bar grabbed, so no more grab locations
        //IK STUFF
        //if (useIK)
        //{
        //    ResetBarGrabbers();
        //}
        
        if (barAudioHandler != null)
        {
            barAudioHandler.PlayReleaseSound(barLoc);
        }

        //lock grabbed bar and change icon
        uiManager.ReleaseGrabber();


    }

    /// <summary>
    /// player uses the space bar to push off the wall when they are stuck
    /// </summary>
    private void PropelOffWall()
    {
        if (rb.linearVelocity.magnitude <= pushSpeed && canPushOff && !uiManager.BarInRaycast && !uiManager.BarInPeripheral)
        {
            //create a vector for the new velocity
            Vector3 propelDirection = Vector3.zero;
            propelDirection -= cam.transform.forward * propelOffWallThrust;

            //zero out the player velocity
            rb.linearVelocity *= .7f;
            //add the force to the rb
            rb.AddForce(propelDirection * Time.deltaTime, ForceMode.VelocityChange);
        }
    }

    //player uses WASD to propel themselves faster, only while currently grabbing a bar
    private void PropelOffBar()
    {
        //save the initial velocity to help scale the propel
        float initialVelocityMagnitude = rb.linearVelocity.magnitude * .35f;

        //ensure the velocity is 1 no matter what
        if (initialVelocityMagnitude < 1)
        {
            initialVelocityMagnitude = 1;
        }

        hasPropelled = false;
        //if the player is grabbing and no movement buttons are currently being pressed
        if (isGrabbing)
        {
            if (canPropel == false)
            {
                return;
            }
            //check if no movement buttons are currently being pressed
            bool isThrusting = Mathf.Abs(thrust1D) > 0.1f;
            bool isStrafing = Mathf.Abs(strafe1D) > 0.1f;

            if (movementKeysReleased && (isThrusting || isStrafing))
            {
                //initialize a vector 3 for the propel direction
                hasPropelled = true;
                Vector3 propelDirection = Vector3.zero;

                //set the player velocity to half to ensure the momentum doesn't influence the propel
                rb.linearVelocity *= 0.5f;

                //if W or S are pressed
                if (isThrusting)
                {
                    //release the bar and calculate the vector to propel based on the forward look
                    ReleaseBar();
                    propelDirection += cam.transform.forward * thrust1D * propelThrust;
                    //Debug.Log("Propelled forward or back");
                }
                //if A or D are pressed
                else if (isStrafing)
                {
                    //release the bar and calculate the vector to propel based on the right look
                    ReleaseBar();
                    propelDirection += cam.transform.right * strafe1D * propelThrust;
                    //Debug.Log("Propelled right or left");
                }
                //add the propel force to the rigid body
                rb.linearVelocity *= .5f;
                rb.AddForce(propelDirection * initialVelocityMagnitude, ForceMode.VelocityChange);

                // Set the flag to false since keys are now pressed
                movementKeysReleased = false;
            }
            // Update the flag if no movement keys are pressed
            else if (!isThrusting && !isStrafing)
            {
                movementKeysReleased = true;
            }
        }
    }

    #endregion

    #region Collision Detection and Health Handling
    private void DetectBarrierAndBounce()
    {
        float detectionRadius = boundingSphere.radius + .01f; // Slightly larger for early detection
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, uiManager.BarrierLayer);

        //if the player is grabbing on a bar and going slower than walking speed
        if (!useManualPullIn)
        {
            if (isGrabbing && rb.linearVelocity.magnitude < zeroGWalkSpeed)
            {
                //ignore bouncing
                return;
            }
        }

        //Debug.Log(hitColliders.Length);

        if (hitColliders.Length == 0)
        {
            return; //no collision, no bounce 
        }

        Vector3 avgBounceDirection = Vector3.zero;
        int bounceCount = 0;
        float ogSpeed = rb.linearVelocity.magnitude; //store initial velocity magnitude
        Vector3 impactPoint = transform.position;

        //Debug.Log(ogSpeed);

        foreach (Collider barrier in hitColliders)
        {
            //Debug.Log("colliding");
            Vector3 closestPoint = barrier.ClosestPoint(transform.position);
            impactPoint = closestPoint; // Store most recent impact
            Vector3 wallNormal = (transform.position - closestPoint).normalized;
            Vector3 reflectDirection = Vector3.Reflect(rb.linearVelocity.normalized, wallNormal);

            //push the player away slighly so they won't be stuck colliding with the wall.
            Vector3 pushAway = wallNormal * 0.05f; // small offset to prevent overlap
            transform.position += pushAway;

            //get bounce directions
            avgBounceDirection += reflectDirection;
            bounceCount++;
            //early exit if multiple bounces aren't needed
            if (bounceCount > 1)
            {
                break;
            }
        }

        if (bounceCount > 0)
        {
            avgBounceDirection.Normalize(); // average direction of the bounce
            float bounceSpeed = ogSpeed * .75f; // keep 75% of initial speed so it doesn't gain velocity on bounces

            //Debug.Log("BounceSpeed: " + bounceSpeed);

            //we want to make sure that all bounces off the wall while traversing are set to  minimum speed
            //however, if the player is grabbing we dont want these reset bounces as they jolt the player while grabbing
            if (!isGrabbing)
            {
                //verify the bounce is faster than minimum speed
                if (bounceSpeed <= minimumSpeed)
                {
                    //reset the bounce speed to be the minimum, ensuring constant bounces
                    bounceSpeed = minimumSpeed;
                }
            }

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            //rb.linearVelocity = avgBounceDirection * bounceSpeed;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, avgBounceDirection * bounceSpeed, bounceAcc);
        }

        //check if the bounce is a hard bounce and we haven't been previously hit in the last 1.5 seconds  
        if (ogSpeed >= dangerSpeed && !justHit && !isDead)
        {
            //decrease the player's health by 3
            DecreaseHealth(2);
            justHit = true;
            playerAudio.PlayFatalBounce(impactPoint);
        }
        else if (ogSpeed >= mediumSpeed && !justHit && !isDead)
        {
            //decrease the player's health by 1
            DecreaseHealth(1);
            justHit = true; ;
            playerAudio.PlayHardBounce(impactPoint);
        }
        else
        {
            playerAudio.PlaySoftBounce(impactPoint);
        }
    }

    private void DetectClosingDoorTakeDamageAndBounce()
    {
        float detectionRadius = boundingSphere.radius + 0.1f; // slightly larger for early detection
        Collider[] hitDoors = Physics.OverlapSphere(transform.position, detectionRadius, uiManager.DoorLayer);

        if (hitDoors.Length == 0)
        {
            return; //no collision, no bounce 
        }

        Vector3 avgBounceDirection = Vector3.zero;
        Vector3 impactPoint = transform.position; // default
        int bounceCount = 0;
        float ogSpeed = rb.linearVelocity.magnitude; //store initial velocity magnitude

        foreach (Collider door in hitDoors)
        {
            //get the doorscript of the parent object of the door collider
            DoorScript doorScript = door.GetComponentInParent<DoorScript>();

            if (doorScript != null && doorScript.IsClosing)
            {
                //check that the state is a broken door
                if (doorScript.DoorState == DoorScript.States.Broken)
                {
                    bounceCount++;
                    //Debug.Log("bounce count" + bounceCount);

                    //calculate bounce direction away from the door
                    Vector3 bounceDirection = (transform.position - door.bounds.center).normalized;

                    impactPoint = door.bounds.center;

                    avgBounceDirection += bounceDirection;

                    //early exit if multiple bounces aren't needed
                    if (bounceCount >= 1)
                    {
                        break;
                    }
                }
            }
        }
        //if we have a bounce to calculate, and we haven't been hit
        if (bounceCount > 0)
        {
            //set velocity to zero
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            //only normalize if avgBounceDirection is valid
            if (avgBounceDirection != Vector3.zero)
            {
                avgBounceDirection.Normalize();
            }

            //Debug.Log("Avg Bounce Direction: " + avgBounceDirection);

            float bounceSpeed = ogSpeed * .75f; // keep 30% of initial speed so it doesn't gain 

            //calculate the direction of the bounce
            Vector3 propelDirection = avgBounceDirection * ogSpeed * (propelThrust * .50f) * 0.07f;
            //Debug.Log("propel direction: " + propelDirection);
            rb.AddForce(propelDirection, ForceMode.VelocityChange);
            playerAudio.PlayFatalBounce(impactPoint);

            //decrease the player health after they have collided with the closing door
            if (!isDead) isDead = true;

            //set door cooldown so the collisions do not spam
            doorCollisionCooldownFrames = maxDoorCollisionCooldownFrames;
        }
    }
    //decreases the health of the player
    private void DecreaseHealth(int i)
    {
        //if we are in GodMode
        if (GodMode)
        { //exit the method, we will not be taking damage
            return;
        }
        
        //decrease the player health by however many is inputted
        if(playerHealth - i < 0)
        {
            playerHealth = 0;
        }
        else
        {
            playerHealth -= i;
        }

        //check for if the player is dead or not
        if (playerHealth <= 0)
        {
            //set isDead as true
            isDead = true;
        }
    }

    private void IncreaseHealth(int i)
    {
        playerHealth += i;
        if (playerHealth > 4)
        {
            playerHealth = 4;
            return;
        }
        prevHurt = false;
    }

    //this controls logic for the stim charges to be used
    public void UseStimCharge()
    {
        if(playerHealth < 4 && numStims > 0)
        {
            if (!usingStimCharge)
            {
                usingStimCharge = true;
                StartCoroutine(UseStim());
            }
            
        }
    }

    //dependent of UseStimeCharge
    private IEnumerator UseStim()
    {
        if (hasUsedStim == false)
        {
            hasUsedStim = true;
        }

        playerAudio.PlayUseStim();
        yield return new WaitForSeconds(1.8f);
        IncreaseHealth(2);
        numStims--;
        usingStimCharge = false;

    }
    /// <summary>
    /// logic that allows you to add stims to the inventory. the integer inputed is added to the current stim inventory. however, if the added amt goes over the max, it sets it only to the max
    /// </summary>
    /// <param name="i"></param>
    public void AddStimsToInv(int i)
    {
        if(numStims < maxNumStim)
        {
            numStims += i;
            if(numStims > maxNumStim)
            {
                numStims = maxNumStim;
            }
        }
    }

    private void JustHitCoolDown()
    {
        if (justHit && !prevJustHit)
        {
            //create a timestamp representing the end of the cooldown
            justHitTimeStamp = Time.time + justHitCoolDown;

            //Debug.Log("timestamp for cooldown: " + justHitTimeStamp);
            prevJustHit = justHit;
        }

        //if the timestamp is less than or equal to the current time
        if (Time.time >= justHitTimeStamp)
        {
            //set justHit to false, allowing player to take damage again
            justHit = false;
            prevJustHit = justHit;
        }
    }

    #endregion

    /// <summary>
    /// This method is used to set movement of the player to restrcited as the player is now dead
    /// </summary>
    public void PlayerDead()
    {
        canMove = true;
        canRoll = false;
        canPropel = false;
        canPushOff = false;
        canGrab = false;
    }

    public void Respawn()
    {
        // whether or not we load from save depends on whether temp data exists
        GlobalSaveManager.LoadFromSave = GlobalSaveManager.TempDataExists();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    #endregion
    #endregion

    #region Input Methods
    //when we press the buttons on the keyboard or controller these methods pass the buttons through to read the values
    //MUST MANUALLY SET THE CONNECTIONS IN THE EVENTS PANEL ONCE ADDED A PLAYER INPUT COMPONENT
    public void OnMouseX(InputAction.CallbackContext context)
    {
        rotationHoriz = context.ReadValue<float>();
    }

    public void OnMouseY(InputAction.CallbackContext context)
    {
        rotationVert = context.ReadValue<float>();
    }

    public void OnForwardandBack(InputAction.CallbackContext context)
    {
        /*once the button, Keyboard or Controller, that is passed through the
         Player Input event to this value of thrust1D*/
        thrust1D = context.ReadValue<float>();
    }

    public void OnLeftandRight(InputAction.CallbackContext context)
    {
        strafe1D = context.ReadValue<float>();
    }

    public void OnUpandDown(InputAction.CallbackContext context)
    {
        upandDown1D = context.ReadValue<float>();
    }
    public void OffWall(InputAction.CallbackContext context)
    {
        //manual pull in logic
        if (useManualPullIn)
        {
            if (isGrabbing)
            {
                // Handle manual pull-in
                if (context.performed)
                {
                    isPullingIn = true;
                    swinging = false;
                }
                else if (context.canceled)
                {
                    isPullingIn = false;
                    swinging = true;
                }
            }
        }
        
        if (context.performed && potentialWall != null && uiManager.CanPushOffNow)
        {
            //Debug.Log("space pressed");
            PropelOffWall();
        }
    }
    public void OnRoll(InputAction.CallbackContext context)
    {
        rotationZ = context.ReadValue<float>();
        if(canRoll)
        {
            hasRolled = true;
        }
        
    }
    public void OnGrab(InputAction.CallbackContext context)
    {
        if (context.performed && potentialGrabbedBar != null)
        {
            if (canGrab)
            {
                GrabBar();
            }
            
        }
        else if (context.canceled)
        {
            ReleaseBar();
        }
    }

    public void OnStim(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            UseStimCharge();
        }
    }
    #endregion


    #region Level/ Scene Transferring

    public void ApplySceneTransferState(Vector3 velocity, Vector3 angularVelocity, Quaternion camRotation)
    {
        rb.linearVelocity = velocity;
        rb.angularVelocity = angularVelocity;
        cam.transform.rotation = camRotation;
        //clear input state so no phantom movement
        rotationHoriz = 0f;
        rotationVert = 0f;
        rotationZ = 0f;
    }
    #endregion

    #region Global Save Manager Integration

    // backs up player data for saving
    public void StorePlayerData(Vector3 _position) // takes the checkpoints respawn point
    {
        playerData = new PlayerData(
            _position,
            cam.transform.rotation,
            playerHealth, 
            numStims, 
            hasUsedStim,
            tutorialManager.inTutorial,
            (bool[])accessPermissions.Clone(),
            wristMonitor.HasWristMonitor,
            wristMonitor.IsActive,
            new List<WristMonitor.Objective>(wristMonitor.mainObjectives),
            new List<WristMonitor.Objective>(wristMonitor.completedObjectives)
            );
    }

    // called when loading a save
    private void LoadPlayerData()
    {
        transform.position = playerData.Position;
        cam.transform.rotation = playerData.Rotation;
        playerHealth = playerData.Health;
        numStims = playerData.Stims;
        hasUsedStim = playerData.HasUsedStim;
        // reset all actions
        if (playerData.InTutorial)
        {
            tutorialManager.RestartTutorial();
        }
        else
        {
            // get rid of the tutorial
            tutorialMode = false;
            canGrab = true;
            canMove = true;
            canPropel = true;
            canRoll = true;
            canPushOff = true;
        }
        // set wrist monitor data
        accessPermissions = (bool[])playerData.AccessPermissions.Clone();
        wristMonitor.HasWristMonitor = playerData.HasWristMonitor;
        wristMonitor.IsActive = playerData.ShowingWristMonitor;
        wristMonitor.mainObjectives = new List<WristMonitor.Objective>(playerData.MainObjectives);
        wristMonitor.completedObjectives = new List<WristMonitor.Objective>(playerData.CompletedObjectives);
    }

    public void LoadSaveFile(string fileName)
    {
        string path = Application.persistentDataPath;
        string loadedData = GlobalSaveManager.LoadTextFromFile(path, fileName);
        if (loadedData != null && loadedData != "")
        {
            playerData = JsonUtility.FromJson<PlayerData>(loadedData);
            LoadPlayerData();
            // stop the player from moving on respawn
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void CreateSaveFile(string fileName)
    {
        string json = JsonUtility.ToJson(playerData);
        string path = Application.persistentDataPath;
        GlobalSaveManager.SaveTextToFile(path, fileName, json);
    }
    #endregion

}
