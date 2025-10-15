using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PauseMenu : MonoBehaviour
{
    public static bool Paused = false;
    public GameObject PauseMenuScreen;
    [SerializeField] private GameObject settingsMenuScreen; // drag your settings menu here
    public Player player;
    public PlayerInput playerInput;
    public MonoBehaviour[] extraScriptsToDisable; // drag camera scripts here (CameraToggle, etc.)

    // FIX: Always reset pause state when this script is loaded (prevents being stuck paused after scene changes)
    void Awake()
    {
        Paused = false;
        Time.timeScale = 1f;
    }

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        //--escape key pauses/starts game--
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Paused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    void PauseGame()
    {
        PauseMenuScreen.SetActive(true);
        if (settingsMenuScreen != null)
            settingsMenuScreen.SetActive(false); // make sure settings are hidden
        EventSystem.current.SetSelectedGameObject(null);
        Time.timeScale = 0f;
        Paused = true;

        if (playerInput != null)
            playerInput.enabled = false;
        if (player != null)
            player.enabled = false;

        foreach (var script in extraScriptsToDisable)
        {
            if (script != null)
                script.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        PauseMenuScreen.SetActive(false);
        if (settingsMenuScreen != null)
            settingsMenuScreen.SetActive(false); // ensure settings menu is hidden
        EventSystem.current.SetSelectedGameObject(null);
        Time.timeScale = 1f;
        Paused = false;

        if (playerInput != null)
            playerInput.enabled = true;
        if (player != null)
            player.enabled = true;

        foreach (var script in extraScriptsToDisable)
        {
            if (script != null)
                script.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    //--opens the settings menu--
    public void OpenSettings()
    {
        if (settingsMenuScreen != null)
        {
            PauseMenuScreen.SetActive(false);     // hide pause menu first
            settingsMenuScreen.SetActive(true);   // then show settings
        }
        else
        {
            Debug.LogWarning("PauseMenu: Settings Menu Screen not assigned!");
        }
    }

    //--closes the settings menu--
    public void CloseSettings()
    {
        if (settingsMenuScreen != null)
        {
            settingsMenuScreen.SetActive(false);  // hide settings first
            PauseMenuScreen.SetActive(true);      // then show pause menu again
        }
        else
        {
            Debug.LogWarning("PauseMenu: Settings Menu Screen not assigned!");
        }
    }

    //--LOADS SCENE BY SUBTRACTING 1 IN BUILD INDEX! (E.g. PauseMenu (1) -> Quit -> Subtracts 1 -> Main Menu (0))
    //--Will NOT work if the PauseMenu is not behind the Main Menu!!--
    public void QuitButton()
    {
        // FIX: make sure pause state and timescale reset before leaving scene
        Time.timeScale = 1f;
        Paused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }
}

