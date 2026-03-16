using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DormHallEvent : MonoBehaviour, ISaveable
{
    [SerializeField]
    private ZeroGravity player;

    // wrist monitor
    //whether or not player is within grab distance of the wrist monitor
    [SerializeField]
    private bool canGrab;
    //can the wrist monitor be picked up yet?
    private bool isGrabbable;
    [SerializeField]
    private WristMonitor wristMonitor;

    [SerializeField]
    private DoorScript medDoor;
    public GameplayBeatAudio audioManager;

    [SerializeField]
    private CanvasGroup wristMonitorTutorial;

    private bool tutorialMonitorFaded = false;

    private DialogueManager dialogueManager;

    [SerializeField] private Light monitorLight;

    public StingerManager stingerManager;

    public bool CanGrab
    {
        get { return canGrab; }
        set { canGrab = value; }
    }

    public bool IsGrabbable
    {
        get { return isGrabbable; }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canGrab = false;
        isGrabbable = true;

        dialogueManager = FindFirstObjectByType<DialogueManager>();
        // continue from save
        if (GlobalSaveManager.LoadFromSave) GlobalSaveManager.LoadSavable(this, false);
        StartCoroutine(BlinkMonitor());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // This method is called by the input system when the player interacts with the wrist monitor
    public void OnInteractWristMonitor(InputAction.CallbackContext context)
    {

        //Handle wrist monitor pickup
        if (canGrab && isGrabbable)
        {
            player.AccessPermissions[0] = true;

            isGrabbable = false;
            canGrab = false;

            audioManager.playMonitorPickup();

            //wrist.SetActive(false);

            wristMonitor.HasWristMonitor = true;

            StartCoroutine(FadeCanvasGroup(wristMonitorTutorial, 0f, 1f));

            StartCoroutine(FadeTutorialPanelTimer());

            //medDoor.SetState(DoorScript.States.Closed);

            //ambientController.Progress();

            //dialogueManager.StartDialogueSequence(6, 1f);

            //cuttingOutTrigger.enabled = true;

        }
    }

    public void CompleteDormTerminal()
    {
        StartCoroutine(TerminalComplete());
    }

    private IEnumerator TerminalComplete()
    {
        medDoor.SetState(DoorScript.States.Closed);
        dialogueManager.StartDialogueSequence(2, 1f);
        stingerManager.PlayDormRoomStinger();
        yield return new WaitUntil(() => dialogueManager.IsDialogueActive == false);
        wristMonitor.CompleteObjective();

    }
    private IEnumerator FadeTutorialPanelTimer()
    {
        yield return new WaitForSeconds(8f);

        if (tutorialMonitorFaded == false)
        {
            tutorialMonitorFaded = true;
            StartCoroutine(FadeCanvasGroup(wristMonitorTutorial, 1f, 0f));
        }
    }

    public void FadeOutMonitorTutorial()
    {
        if (tutorialMonitorFaded == false)
        {
            tutorialMonitorFaded = true;
            StartCoroutine(FadeCanvasGroup(wristMonitorTutorial, 1f, 0f));
        }

    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha)
    {
        float timeElapsed = 0f;
        float fadeDuration = 1f;

        while (timeElapsed < fadeDuration)
        {
            // Lerp alpha from start to end
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timeElapsed / fadeDuration);
            timeElapsed += Time.deltaTime;
            yield return null; // Wait until the next frame
        }

        canvasGroup.alpha = endAlpha; // Ensure it's set to the final alpha
    }

    public void LoadSaveFile(string fileName)
    {
        // this will load data from the file to a variable we will use to change this objects data
        string path = Application.persistentDataPath;
        string loadedData = GlobalSaveManager.LoadTextFromFile(path, fileName);
        if (loadedData != null && loadedData != "")
        {
            if (loadedData == "False")
            {
                isGrabbable = false;
            } else if (loadedData == "True")
            {
                isGrabbable = true;
            }
        }
    }

    public void CreateSaveFile(string fileName)
    {
        // this will create a file backing up the data we give it
        string path = Application.persistentDataPath;
        GlobalSaveManager.SaveTextToFile(path, fileName, isGrabbable.ToString());
    }

    private IEnumerator BlinkMonitor()
    {
        while (wristMonitor.HasWristMonitor == false)
        {
            monitorLight.enabled = true;
            yield return new WaitForSeconds(0.5f);

            monitorLight.enabled = false;
            yield return new WaitForSeconds(0.5f);
        }
    }
}
