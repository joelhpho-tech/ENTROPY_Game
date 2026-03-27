using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    public GameObject ZeroGPlayer;
    private ZeroGravity playerController;
    public DialogueManager dialogueManager;
    public DoorScript doorToOpen;
    //public DoorScript endingDoor;
    private PickupScript pickupScript;

    //keep track when inside of the tutorial
    public bool inTutorial = false;

    public int currentStep = 0;
    private bool isWaitingForAction = false;
    private Coroutine failureTimer;
    private bool tutorialSkipped = false;
    public bool stepComplete = false;

    //tutorial canvas groups
    public CanvasGroup grabCanvasGroup;
    public CanvasGroup propelCanvasGroup;
    //public CanvasGroup pushOffCanvasGroup;
    public CanvasGroup pickUpItemCanvasGroup;
    public CanvasGroup throwItemCanvasGroup;
    public CanvasGroup rollQCanvasGroup;
    public CanvasGroup rollECanvasGroup;
    public CanvasGroup enterCanvasGroup;

    [SerializeField] private Slider rollProgressBar;
    [SerializeField] private float requiredRotation = 180f; // how much roll needed

    //[SerializeField] private AudioClip tutorialStingerClip;

    //public AudioClip TutorialStingerClip => tutorialStingerClip; // property accessor

    public float fadeDuration = 1f;

    float timer = 10f;
    // failure flags so each only plays once
    private bool hasPlayedPushOffFailure = false;
    private bool hasPlayedRollFailure = false;
    private bool rollPanelHidden = false;
    private bool pushOffPanelHidden = false;


    //intended tutorial abilities
    private bool canGrab = true;
    private bool canRoll = true;
    private bool canPushOff = true;
    private bool canThrow = true;
    private bool canPropel = true;
    private float playerGrabRange;

    private float initialRollZ;

    //audio managers
    public DialogueAudio dialogueAudio;
    public StingerManager stingerManager;

    private bool inItemGrabTutorial = false;
    private bool inItemThrowTutorial = false;
    private bool detectedPickup = false;
    private bool hasAttemptedSecondGrab = false;

    // rolling threshold (in degrees) beyond which we consider upside down
    [SerializeField] private float rollAngleThreshold = 150f;


    //timer for checking if player is upside down
    private float upsideDownTimer = 0f;
    private const float upsideDownDuration = 3f;

    [SerializeField] private Slider skipProgressSlider;
    [SerializeField] private float holdDuration = 1f;
    private float currentHoldTime = 0f;


    public bool IsTutorialSkipped
    {
        get { return tutorialSkipped; }
    }


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        playerController = ZeroGPlayer.GetComponent<ZeroGravity>();
        pickupScript = ZeroGPlayer.GetComponent<PickupScript>();
        playerGrabRange = playerController.GrabRange;
        playerController.TutorialMode = true;

        //determines if the player is to be in tutorial from the player controller's "TutorialMode" bool, which is saved by the GSM
        if (playerController.TutorialMode == true && !GlobalSaveManager.LoadFromSave)
        {
            dialogueManager.OnDialogueEndTutorial += OnDialogueComplete;
            StartCoroutine(StartTutorial());
        }

        if (skipProgressSlider != null)
        {
            skipProgressSlider.minValue = 0f;
            skipProgressSlider.maxValue = 1f;
            skipProgressSlider.value = 0f;
        }

    }

    void Update()
    {
        // Skip tutorial with Enter
        if (!tutorialSkipped && inTutorial)
        {
            HandleTutorialSkip();
        }

        //isWaitingForAction is the how we determine when the tutorial is waiting for an action for the player, and it matches with step in accordance with RunTutorialStep()
        if (isWaitingForAction)
        {
            if (currentStep == 1 && playerController.IsGrabbing)
            {
                FadeOut(grabCanvasGroup);
                CompleteStep();
            }
            else if (currentStep == 2)
            {

                UpdateRollProgress();

                bool isUpsideDown = playerController.TotalRotation >= requiredRotation;

                if (isUpsideDown)
                {
                    //Debug.Log("Player rolled upside down");
                    playerController.StopRollingQuickly();

                    FadeOut(rollQCanvasGroup);
                    CompleteStep();
                    playerController.TotalRotation = 0;
                    rollProgressBar.gameObject.SetActive(false); // hide when done
                }
            }
            else if (currentStep == 3)
            {
                UpdateRollProgress();
                bool isUpright = playerController.TotalRotation <= requiredRotation;

                if (isUpright)
                {
                    //Debug.Log("Player rolled upright");
                    playerController.StopRollingQuickly();

                    FadeOut(rollECanvasGroup);
                    CompleteStep();
                    playerController.TotalRotation = 0;
                    rollProgressBar.gameObject.SetActive(false); // hide when done
                }
            }

            else if (currentStep == 4 && playerController.HasPropelled && hasAttemptedSecondGrab == false)
            {
                //Debug.Log("Detected player propel");
                hasAttemptedSecondGrab = true;
                playerController.HasPropelled = false; // Reset to prevent multiple detections
                SetPlayerAbilities(true, true, true, true, true);
                StartCoroutine(WaitForSecondGrab());

            }
        }

        //When the player enters the dorm hall there's an optional tutorial to handle grabbing items.
        if (inItemGrabTutorial)
        {
            if (pickupScript.HeldObject != null && !detectedPickup)
            {
                detectedPickup = true;
                inItemGrabTutorial = false;
                if (pickUpItemCanvasGroup.alpha > 0)
                {
                    pickUpItemCanvasGroup.alpha = 0;
                }
                FadeIn(throwItemCanvasGroup);
                inItemThrowTutorial = true;
            }
        }

        if (inItemThrowTutorial)
        {
            if (pickupScript.HeldObject == null)
            {
                FadeOut(throwItemCanvasGroup);
                inItemThrowTutorial = false;
            }
        }
    }

    //Starts the tutoral and sets up player actions, audio, and UI
    private IEnumerator StartTutorial()
    {
        SetPlayerAbilities(false, false, false, false, false);

        //temporarily reduce grab range so the player can only grab the closest bar to them.
        playerController.GrabRange = 1f;
        inTutorial = true;
        yield return new WaitForSeconds(1f);
        dialogueAudio.PlayJingle();

        // Play looping tutorial stinger with fade-in
        if (stingerManager != null)
        {
            StartCoroutine(DelayedStinger(8f));
        }


        FadeIn(enterCanvasGroup);
        dialogueManager.StartDialogueSequence(0, 2f); // Ensure correct tutorial sequence index

        //fading out the tutorial skip panel
        StartCoroutine(DelayFadeOut(7, enterCanvasGroup));
    }

    private IEnumerator DelayedStinger(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Play looping tutorial stinger with fade-in
        stingerManager.PlayTutorialStinger(fadeInDuration: 7f);
    }

    //Called from the Dialogue Manager when the next step is ready to occur
    public void ProgressTutorial()
    {
        if (isWaitingForAction) return;
        currentStep++;
        RunTutorialStep();
    }

    //Is intended to be used when the player skips the tutorial.
    public void ForceCompleteAllSteps()
    {
        stepComplete = true;
        isWaitingForAction = false;
    }

    //Switch statement that handles each step in the tutorial
    void RunTutorialStep()
    {
        //Debug.Log("Current Step: " + currentStep);
        switch (currentStep)
        {

            case 1:
                // Step 1: Grab a bar
                //Debug.Log("Tutorial 1: Grab bar");
                stepComplete = false;
                isWaitingForAction = true;
                SetPlayerAbilities(true, false, false, false, false); // Only allow roll
                FadeIn(grabCanvasGroup);

                break;

            case 2:
                // Step 2: Roll 180 degrees upside down
                //Debug.Log("Tutorial 2: Roll upside down");
                if (!rollProgressBar.gameObject.activeSelf)
                {
                    rollProgressBar.gameObject.SetActive(true);
                    rollProgressBar.value = 0f; // reset to empty immediately
                }

                stepComplete = false;
                isWaitingForAction = true;
                SetPlayerAbilities(true, false, false, true, false); // grab and roll only
                initialRollZ = playerController.cam.transform.eulerAngles.z;
                FadeIn(rollQCanvasGroup);

                break;

            case 3:
                // Step 3: Roll back upright
                //Debug.Log("Tutorial 3: Roll upright");
                if (!rollProgressBar.gameObject.activeSelf)
                {
                    rollProgressBar.gameObject.SetActive(true);
                    rollProgressBar.value = 0f; // reset to empty immediately
                }
                requiredRotation = -180f;
                stepComplete = false;
                isWaitingForAction = true;
                SetPlayerAbilities(true, false, false, true, false); // grab and roll only
                initialRollZ = playerController.cam.transform.eulerAngles.z;
                FadeIn(rollECanvasGroup);

                break;

            case 4:
                // Step 4: Propel and grab another bar
                //Debug.Log("Tutorial 4: Propel and grab another bar");
                stepComplete = false;
                isWaitingForAction = true;
                playerController.GrabRange = playerGrabRange;
                SetPlayerAbilities(true, true, true, true, true); // Enable all
                FadeIn(propelCanvasGroup);
                break;
            case 5:
                //Debug.Log("Tutorial Complete");

                EndTutorial();

                break;
        }
    }

    //IEnumerator for section 4. After the player has pushed off a bar, they need to grab another bar within 6 seconds to complete this challenge
    private IEnumerator WaitForSecondGrab()
    {
        //Debug.Log("Wait for second grab has been called");
        float timer = 0f;
        float maxTime = 6f;
        bool barGrabbed = false;

        while (timer < maxTime)
        {
            //Debug.Log("While Loop is still playing");
            if (playerController.IsGrabbing)
            {
                barGrabbed = true;
                break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        if (!barGrabbed)
        {
            // Player failed - play failure dialogue and WAIT for it to complete
            // The failure dialogue will automatically:
            // 1. Wait for current line to finish typing
            // 2. Pause the main dialogue sequence
            // 3. Play the failure dialogue (can be skipped)
            // 4. Increment currentDialogueIndex to skip the success dialogue (if incrementsDialogue=true)
            // 5. Resume the main sequence
            FadeOut(propelCanvasGroup);
            yield return StartCoroutine(dialogueManager.PlayFailureDialogueRoutine(0));
        }
        else
        {
            FadeOut(propelCanvasGroup);
            // Step is now complete

            CompleteStep();
        }




    }

    public void CompleteStep()
    {
        stepComplete = true;
        isWaitingForAction = false;
    }

    //sets the abilities of the player and has them reflected in the tutorial script
    public void SetPlayerAbilities(bool canGrab, bool canPropel, bool canPushOff, bool canRoll, bool canThrow)
    {
        playerController.CanGrab = canGrab;
        playerController.CanPropel = canPropel;
        playerController.CanPushOff = canPushOff;
        playerController.CanRoll = canRoll;
        pickupScript.CanPickUp = canThrow;

        this.canGrab = canGrab;
        this.canPropel = canPropel;
        this.canPushOff = canPushOff;
        this.canRoll = canRoll;
        this.canThrow = canThrow;
    }

    //Method that sets the abilities of the player back to that which is currently needed in the tutorial.
    public void SetAbilitiesToTutorial()
    {
        playerController.CanGrab = canGrab;
        playerController.CanPropel = canPropel;
        playerController.CanPushOff = canPushOff;
        playerController.CanRoll = canRoll;
        pickupScript.CanPickUp = canThrow;
    }

    void EndTutorial()
    {
        SetPlayerAbilities(true, true, true, true, true);
        inTutorial = false;
        isWaitingForAction = false;
        playerController.TutorialMode = false;
        playerController.GrabRange = playerGrabRange;


        // Fade out tutorial stinger



        //remove all tutorial panels
        HideAllPanels();
        //ambientController.Progress();
        currentStep = 5;


        /*        Debug.Log("EndTutorial called. Subscribing to OnDialogueEnd.");
                dialogueManager.StartDialogueSequence(1, 0.2f);

                dialogueManager.OnDialogueEnd += HandleDialogueFinished;
        */
        dialogueManager.StartDialogueSequence(1, 0.2f);

        StartCoroutine(WaitForDialogue1AndOpenDoor());

        /*        if (doorToOpen != null)
                {
                    if (doorToOpen.DoorState != DoorScript.States.Open)
                    {
                        doorToOpen.SetState(DoorScript.States.Open);
                    }
                }*/
    }

    private IEnumerator WaitForDialogue1AndOpenDoor()
    {
        // Wait until DialogueManager is idle and sequence 1 has finished
        while (dialogueManager.IsDialogueActive)
        {
            yield return null;
        }

        if (doorToOpen != null && doorToOpen.DoorState != DoorScript.States.Open)
        {
            doorToOpen.SetState(DoorScript.States.Open);
        }

        if (stingerManager != null)
        {
            stingerManager.StopTutorialStinger(fadeOutDuration: 12f);
        }

    }

    /*    private void HandleDialogueFinished(int sequenceIndex)
        {
            // Only react to sequence 1 finishing


            Debug.Log("HandleDialogueFinished RECEIVED index: " + sequenceIndex);
            dialogueManager.OnDialogueEnd -= HandleDialogueFinished; // Unsubscribe

            if (doorToOpen != null && doorToOpen.DoorState != DoorScript.States.Open)
            {
                doorToOpen.SetState(DoorScript.States.Open);
            }

            *//*            if (doorToOpen != null)
                        {
                            if (doorToOpen.DoorState != DoorScript.States.Open)
                            {
                                doorToOpen.SetState(DoorScript.States.Open);
                            }
                        }*//*

            stingerManager.StopTutorialStinger(fadeOutDuration: 12f);

        }

        private IEnumerator ForcePlayEndTutorialDialogue()
        {
            // Wait one frame so EndTutorial() finishes
            yield return null;

            // Ensure handler is correctly wired
            dialogueManager.OnDialogueEnd -= HandleDialogueFinished;
            dialogueManager.OnDialogueEnd += HandleDialogueFinished;

            // Force start sequence 1 manually
            dialogueManager.ForceStartSequence(1);
        }
    */


    //checks to see if the tutorial step is complete
    public bool TutorialStepCompleted()
    {
        //Debug.Log("Step completed? " + stepComplete);
        return stepComplete;
    }

    private void OnDialogueComplete(int sequenceIndex)
    {
        if (!isWaitingForAction) return;
        CompleteStep();
    }

    private IEnumerator DelayFadeOut(float delayTime, CanvasGroup canvas)
    {
        yield return new WaitForSeconds(delayTime); // Wait for the specified time
        FadeOut(canvas);
    }

    // Fade in the UI element (make it visible)
    public void FadeIn(CanvasGroup groupToFade)
    {
        StartCoroutine(FadeCanvasGroup(groupToFade, groupToFade.alpha, 1f));
    }

    // Fade out the UI element (make it invisible)
    public void FadeOut(CanvasGroup groupToFade)
    {
        StartCoroutine(FadeCanvasGroup(groupToFade, groupToFade.alpha, 0f));
    }

    // Coroutine to fade the CanvasGroup over time
    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha)
    {
        float timeElapsed = 0f;

        while (timeElapsed < fadeDuration)
        {
            // Lerp alpha from start to end
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timeElapsed / fadeDuration);
            timeElapsed += Time.deltaTime;
            yield return null; // Wait until the next frame
        }

        canvasGroup.alpha = endAlpha; // Ensure it's set to the final alpha
    }

    public void RestartTutorial()
    {
        //Debug.Log("Restarting tutorial...");

        StopAllCoroutines();

        // Reset tutorial state
        inTutorial = true;
        currentStep = 0;
        isWaitingForAction = false;
        stepComplete = false;
        tutorialSkipped = false;

        playerController.HasRolled = false;

        // Reset failure flags
        hasPlayedPushOffFailure = false;
        hasPlayedRollFailure = false;
        rollPanelHidden = false;
        pushOffPanelHidden = false;

        // Reset ability flags
        canGrab = true;
        canRoll = true;
        canPushOff = true;
        canThrow = true;
        canPropel = true;

        // Set player movement ability to NONE at the start
        SetPlayerAbilities(false, false, false, false, false);

        // Hide all tutorial canvas elements
        HideAllPanels();

        // Reset skip flags in DialogueManager
        //dialogueManager.SkipNextDialogue = false;
        //dialogueManager.IsFailureTriggered = false;
        //dialogueManager.TutorialSkipped = false;

        // Reactivate the tutorial door if it was opened
        if (doorToOpen != null)
        {
            doorToOpen.LockDoor();
        }

        // Restart dialogue from the beginning of the tutorial sequence
        dialogueManager.RestartCurrentDialogue(2f);

        // (Optional) Play intro jingle again
        dialogueAudio.PlayJingle();

        // Re-show the tutorial skip panel
        FadeIn(enterCanvasGroup);
        StartCoroutine(DelayFadeOut(7f, enterCanvasGroup));
    }

    private void HideAllPanels()
    {
        StopAllCoroutines();

        if (enterCanvasGroup.alpha != 0)
        {
            FadeOut(enterCanvasGroup);
        }
        if (rollQCanvasGroup.alpha != 0)
        {
            FadeOut(rollQCanvasGroup);
        }
        if (rollECanvasGroup.alpha != 0)
        {
            FadeOut(rollECanvasGroup);
        }
        if (grabCanvasGroup.alpha != 0)
        {
            FadeOut(grabCanvasGroup);
        }
        if (propelCanvasGroup.alpha != 0)
        {
            FadeOut(propelCanvasGroup);
        }
        if (rollProgressBar.gameObject.activeSelf == true)
        {
            rollProgressBar.gameObject.SetActive(false);
        }
    }

    private void UpdateRollProgress()
    {
        float progress = 0f;

        // Rolling right (positive TotalRotation)
        if (requiredRotation > 0)
        {
            if (playerController.TotalRotation < 0) playerController.TotalRotation = 0;
            progress = Mathf.Clamp01(playerController.TotalRotation / requiredRotation);
        }
        // Rolling left (negative TotalRotation)
        else if (requiredRotation < 0)
        {
            if (playerController.TotalRotation > 0) playerController.TotalRotation = 0;
            progress = Mathf.Clamp01(playerController.TotalRotation / requiredRotation);
        }
        // else progress = 0


        rollProgressBar.value = progress;
    }

    public void ItemGrabTutorial()
    {
        //Debug.Log("Item Grab Tutorial Started");
        inItemGrabTutorial = true;
        StartCoroutine(StartGrabTutorial());
    }
    private IEnumerator StartGrabTutorial()
    {
        FadeIn(pickUpItemCanvasGroup);
        yield return new WaitForSeconds(7f);
        if (pickUpItemCanvasGroup.alpha > 0)
        {
            FadeOut(pickUpItemCanvasGroup);
        }
    }



    private void HandleTutorialSkip()
    {
        if (Keyboard.current.enterKey.isPressed)
        {
            if (enterCanvasGroup.alpha < 1)
            {
                enterCanvasGroup.alpha = 1f;
            }

            skipProgressSlider.GetComponent<CanvasGroup>().alpha = 1.0f;
            currentHoldTime += Time.deltaTime;

            // Update slider progress
            if (skipProgressSlider != null)
            {
                skipProgressSlider.value = Mathf.Clamp01(currentHoldTime / holdDuration);
            }

            // Check if hold duration is complete
            if (currentHoldTime >= holdDuration)
            {
                skipProgressSlider.GetComponent<CanvasGroup>().alpha = 0f;
                tutorialSkipped = true;
                dialogueManager.SkipTutorial();
                ForceCompleteAllSteps();
                FadeOut(enterCanvasGroup);
                if (stingerManager != null)
                {
                    stingerManager.StopTutorialStinger(fadeOutDuration: 12f);
                }
                EndTutorial();
                // Reset after skipping
                currentHoldTime = 0f;
                if (skipProgressSlider != null)
                {
                    skipProgressSlider.value = 0f;
                }

            }
        }
        else
        {
            skipProgressSlider.GetComponent<CanvasGroup>().alpha = 0f;
            // Reset when key is released
            if (currentHoldTime > 0f)
            {
                currentHoldTime = 0f;
                if (skipProgressSlider != null)
                {
                    skipProgressSlider.value = 0f;
                }
            }
        }
    }
}


