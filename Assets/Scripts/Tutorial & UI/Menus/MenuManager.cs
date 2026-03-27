using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
//using static UnityEditor.SceneView;

public class MenuManager : MonoBehaviour
{
    // Serialize Menus for managing
    [SerializeField] private GameObject deathMenu;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject thanksMenu;
    [SerializeField] private GameObject optionsMenu;
    public List<GameObject> activeMenus;

    // Serialize Player info for checkpoints
    [SerializeField] private ZeroGravity player;
    [SerializeField] private GameObject respawnLoc;
    [SerializeField] private GameObject playerCanvas;
    [SerializeField] private CameraFade cameraFade;
    public static bool playerDead = false;

    [SerializeField] private GameObject dialogueCanvas;
    [SerializeField] private GameObject tutorialCanvas;

    // Audio Manager
    [SerializeField] private AudioSource dialogue;

    // Win Condition
    [SerializeField]
    private Win winScript;

    // Set options
    [SerializeField] SettingsMenu SettingsMenu;

    // Check pause condition
    bool _isPaused;

    // Camera effects
    [SerializeField] private GameObject _UICamera;
    private float _unscaledTime = 1000;

    // Level Loading Screen Canvas
    [SerializeField] private GameObject lvlLoadingScreen;

    public void Start()
    {
        activeMenus = new List<GameObject>();
        SettingsMenu.ApplyOptions();
        _isPaused = false;
        _UICamera.SetActive(false);
    }
    private void Update()
    {
        if (activeMenus.Count == 0 && deathMenu.activeInHierarchy)
        {
            activeMenus.Add(deathMenu);
        }
        if (activeMenus.Count == 0 && pauseMenu.activeInHierarchy)
        {
            activeMenus.Add(pauseMenu);
        }
        if (player.IsDead)
        {
            Dead();
        }
        if (winScript.WinCondition && activeMenus.Count== 0)
        {
            activeMenus.Add(thanksMenu);
            _UICamera.SetActive(true);
            thanksMenu.SetActive(true);
        }
        if (activeMenus.Count > 0)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            playerCanvas.SetActive(false);
            dialogueCanvas.SetActive(false);
            tutorialCanvas.SetActive(false);
            _unscaledTime += .01f;
            Shader.SetGlobalFloat("_UnscaledTime", _unscaledTime);
        }   
    }
    public void Pause()
    {
        _unscaledTime = 1000;
        _isPaused = !_isPaused;
        _UICamera.SetActive(true);
        pauseMenu.SetActive(_isPaused);
        if (_isPaused)
        {
            Time.timeScale = 0;
            if (dialogue != null) dialogue.Pause();
        }
        else
        {
            Resume();
        }
        
    }

    public void Resume()
    {
        _UICamera.SetActive(false);
        CloseMenus();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        playerCanvas.SetActive(true);
        dialogueCanvas.SetActive(true);
        tutorialCanvas.SetActive(true);
        Time.timeScale = 1;
        if (dialogue != null) dialogue.UnPause();
        _isPaused = false;
    }

    public void OptionsButton()
    {
        optionsMenu.SetActive(true);
        activeMenus.Add(optionsMenu);
        pauseMenu.SetActive(false);
    }

    public void LastCheckpoint()
    {
        // Need state machine for player to be reset
        Debug.Log("Load Last Checkpoint selected");
        Resume();
        
        Time.timeScale = 1f; // Ensure normal game speed
        playerDead = false;

        StartCoroutine(cameraFade.FadeIn(1.5f));
        player.Respawn();
        playerCanvas.SetActive(true);  // Re-enable player UI
    }

    public void LoadMenu()
    {
        Debug.Log("Menu selected");
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void Dead()
    {
        //restrict the player from movement
        player.PlayerDead();
        _UICamera.SetActive(true);
        //set the death menu to true.
        deathMenu.SetActive(true);

        if(dialogue != null) dialogue.Pause();
    }

    public void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void LastMenu()
    {
        if (activeMenus.Count > 0) {
            activeMenus[0].SetActive(true);
            optionsMenu.SetActive(false);
            activeMenus.Clear();
        }
    }

    void CloseMenus()
    {
        foreach (var menu in activeMenus)
        {
            menu.SetActive(false);
        }
        activeMenus.Clear();
    }
}
