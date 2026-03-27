using NUnit.Framework;
using System.Collections;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices.WindowsRuntime;

public class WristMonitor : MonoBehaviour
{
    // Variables
    bool isActive = false;

    [Header("Dependencies")]
    [SerializeField] private ZeroGravity player;
    //[SerializeField] GameObject wristMonitorObject;
    [SerializeField]
    private bool hasWristMonitor = false;
    // checks the wrist monitor object and manipulates the state of having the wrist monitor accordingly

    [Header("Health Section")]
    [SerializeField] TextMeshProUGUI stimText;
    [SerializeField] TextMeshProUGUI healthText;
    [SerializeField] GameObject fullHealthAnimationImage;
    [SerializeField] GameObject injuredAnimationImage;
    [SerializeField] GameObject lowHealthAnimationImage;

    [Header("Objectives")]
    public List<Objective> mainObjectives = new List<Objective>();
    public List<Objective> completedObjectives = new List<Objective>();
    [SerializeField] TextMeshProUGUI currentObjectiveText;
    [SerializeField] TextMeshProUGUI completedObjectiveText;
    [SerializeField] private GameObject[] _displayTexts;
    [SerializeField] ObjectiveUpdate objectiveUpdator;

    private bool tutorialShowing = true;
    [SerializeField]
    private DormHallEvent dormHallScript;

    [Header("Feedback Sliders")]
    public CanvasGroup tabCanvasGroup;
    public CanvasGroup wristMonitorCanvasGroup;
    [SerializeField] private Slider skipProgressSlider;
    [SerializeField] private float holdDuration = 0.5f;
    private float currentHoldTime = 0f;

    // Properties
    public bool IsActive
    {
        get { return isActive; }
        set { isActive = value; }
    }

    public bool HasWristMonitor
    {
        get { return hasWristMonitor; }
        set {
            if (hasWristMonitor == value) return;
            hasWristMonitor = value;
            OnWristMonitorAcquired?.Invoke(hasWristMonitor);
        }
    }

    public event System.Action<bool> OnWristMonitorAcquired;
    /// <summary>
    /// Public class used to display vital information to the player of how they must proceed
    /// </summary>
    [Serializable]
    public class Objective
    {
        public Objective(string _objectiveName, string _objectiveDescription, string _subObjective, bool _isCompleted)
        {
            objectiveName = _objectiveName;
            objectiveDescription = _objectiveDescription;
            subObjective = _subObjective;
            isCompleted = _isCompleted;
        }
        [SerializeField]
        private string objectiveName;
        public string ObjectiveName
        {
            get { return objectiveName; }
            set { objectiveName = value; }
        }
        [SerializeField]
        private string objectiveDescription;
        public string ObjectiveDescription
        {
            get { return objectiveDescription; }
            set { objectiveDescription = value; }
        }
        [SerializeField]
        private string subObjective;
        public string SubObjective
        {
            get { return subObjective; }
            set { subObjective = value; }
        }
        [SerializeField]
        private bool isCompleted;
        public bool IsCompleted
        {
            get { return isCompleted; }
            set { isCompleted = value; }
        }
    }

    /// <summary>
    /// Creates the objectives for the wristwatch to display
    /// </summary>
    private void Start()
    {
        //player = GameObject.FindWithTag("Player").GetComponent<ZeroGravity>();
        //mainObjectives.Add(new Objective("Empty", "<color=orange>Current Objective: </color>\nEMPTY", "<size=8><color=orange>Sub Objective: </color>\n\tReconnect ALAN</size>", false));
        mainObjectives.Add(new Objective("Empty", @"Connect alan:\ to the nearest terminal", "", false));
        mainObjectives.Add(new Objective("Medbay", @"    -reach Medical_Bay\n    -reconnect alan:\", "", false));
        mainObjectives.Add(new Objective("Medbay_Stim", @"    -refill e-stims\n    -administer e-stim", "", false));
        mainObjectives.Add(new Objective("Dining_Room", @"    -reach Dining_Room\n    -reconnect alan:\", "", false));
        mainObjectives.Add(new Objective("Server_Farm", "    -reach Server_Farm\n    -override Manual_Lockdown", "", false));
        mainObjectives.Add(new Objective("Vocational_Wing", "    -reach Vocational_Wing</size> \n", "", false));
        //if (targetRectTransform == null) {
        //    Debug.LogError("TargetRectTransform not assigned");
        //    return;
        //}
        //startPosition = targetRectTransform.anchoredPosition3D;
        //duration = 0f;
    }

    public void CloseWristMonitorCanvas(InputAction.CallbackContext context)
    {
        if(isActive && context.performed)
        {
            isActive = false;
        }
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

        while (timeElapsed < .5f)
        {
            // Lerp alpha from start to end
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timeElapsed / .5f);
            timeElapsed += Time.deltaTime;
            yield return null; // Wait until the next frame
        }

        canvasGroup.alpha = endAlpha; // Ensure it's set to the final alpha
    }

    public void ToggleMonitorObject()
    {
        if (hasWristMonitor)
        {
            isActive = !isActive;
        }
    }

    /// <summary>
    /// Updates health text as well as the current objective. 
    /// </summary>
    private void FixedUpdate()
    {
        if (isActive)
        {
            wristMonitorCanvasGroup.alpha = 1f;
            UpdateHealthAndInfo();
            if (player.IsGrabbing)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
            }
        }
        else
        {
            wristMonitorCanvasGroup.alpha = 0f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Updates the health animation as well as the stim charges
    /// </summary>
    private void UpdateHealthAndInfo()
    {
        // Show current objective
        if (mainObjectives.Count > 0)
        {
            currentObjectiveText.text = $"{mainObjectives[0].ObjectiveDescription}\n{mainObjectives[0].SubObjective}";
        }
        // Update stim number
        stimText.text = $"{player.NumStims}/3";

        if(player.PlayerHealth <= 1)
        {
            healthText.text = "<Color=red>Status: DYING</color>";
            fullHealthAnimationImage.SetActive(false);
            injuredAnimationImage.SetActive(false);
            lowHealthAnimationImage.SetActive(true);
        }
        if (player.PlayerHealth <= 3)
        {
            healthText.text = "<Color=yellow>Status: INJURED</color>";
            fullHealthAnimationImage.SetActive(false);
            injuredAnimationImage.SetActive(true);
            lowHealthAnimationImage.SetActive(false);
        }
        if (player.PlayerHealth == 4)
        {
            healthText.text = "<Color=green>Status: HEALTHY</color>";
            fullHealthAnimationImage.SetActive(true);
            injuredAnimationImage.SetActive(false);
            lowHealthAnimationImage.SetActive(false);
        }
    }
    /// <summary>
    /// Public method called outside of script (by objective triggers) to complete the first objective in the list
    /// </summary>
    public void CompleteObjective()
    {
        if (mainObjectives.Count > 0)
        {
            mainObjectives[0].IsCompleted = true;
            CheckObjectives();
            objectiveUpdator.TextFade(mainObjectives[0].ObjectiveName);
            

            //Debug.Log(mainObjectives[0].ObjectiveDescription);
        }
    }

    /// <summary>
    /// Cleans up list by removing completed objectives and adding them to a seperate list
    /// </summary>
    void CheckObjectives()
    {
        if (mainObjectives[0].IsCompleted) 
        {
            completedObjectives.Add(mainObjectives[0]);
            mainObjectives.Remove(mainObjectives[0]);
        }
    }

    public void ShowCompleted()
    {
        completedObjectiveText.text = "";
        foreach (var objective in completedObjectives)
        {
            completedObjectiveText.text += objective.ObjectiveName.ToString();
        }
    }
    /// <summary>
    /// Switches the active display of the Wrist Monitor
    /// </summary>
    /// <param name="displayIndex"></param>
    public void SwitchDisplay(int displayIndex)
    {
        for(int i = 0; i < _displayTexts.Length; i++)
        {
            _displayTexts[i].SetActive(i== displayIndex);
        }
    }

    
}

