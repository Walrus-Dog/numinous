using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections;

public class PauseMenu : MonoBehaviour
{
    public static bool Paused;

    [Header("UI")]
    public GameObject PauseMenuScreen;
    [SerializeField] private GameObject settingsMenuScreen;
    [SerializeField] private GameObject firstSelectedButton;

    [Header("Gameplay")]
    public Player player;
    public PlayerInput playerInput;
    public MonoBehaviour[] extraScriptsToDisable;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu"; // <-- set in Inspector if different

    void Awake()
    {
        Paused = false;
        Time.timeScale = 1f;
    }

    void Start()
    {
        EnsureEventSystemExists();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Paused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        EnsureEventSystemExists();

        PauseMenuScreen.SetActive(true);
        if (settingsMenuScreen != null) settingsMenuScreen.SetActive(false);

        // initialize UI before freeze & unlock
        StartCoroutine(ShowPauseMenuRoutine());
    }

    private IEnumerator ShowPauseMenuRoutine()
    {
        // 1) wait one frame so UI event/raycast systems rebuild
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        // 2) now unlock and show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3) reset EventSystem focus
        EventSystem.current?.SetSelectedGameObject(null);
        if (firstSelectedButton != null)
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);

        // 4) finally freeze time and disable gameplay
        Time.timeScale = 0f;
        Paused = true;

        if (playerInput != null) playerInput.enabled = false;
        if (player != null) player.enabled = false;
        foreach (var s in extraScriptsToDisable) if (s != null) s.enabled = false;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        Paused = false;

        PauseMenuScreen.SetActive(false);
        if (settingsMenuScreen != null) settingsMenuScreen.SetActive(false);

        if (playerInput != null) playerInput.enabled = true;
        if (player != null) player.enabled = true;
        foreach (var s in extraScriptsToDisable) if (s != null) s.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        EventSystem.current?.SetSelectedGameObject(null);
    }

    public void OpenSettings()
    {
        if (!settingsMenuScreen) return;
        PauseMenuScreen.SetActive(false);
        settingsMenuScreen.SetActive(true);
        Canvas.ForceUpdateCanvases();
        EventSystem.current?.SetSelectedGameObject(null);
    }

    public void CloseSettings()
    {
        if (!settingsMenuScreen) return;
        settingsMenuScreen.SetActive(false);
        PauseMenuScreen.SetActive(true);
        Canvas.ForceUpdateCanvases();
        EventSystem.current?.SetSelectedGameObject(null);
    }

    public void QuitButton()
    {
        // Unpause & set a menu-safe cursor BEFORE loading the menu scene
        Time.timeScale = 1f;
        Paused = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Disable gameplay inputs/scripts so nothing re-locks the cursor during load
        if (playerInput != null) playerInput.enabled = false;
        if (player != null) player.enabled = false;
        foreach (var s in extraScriptsToDisable) if (s != null) s.enabled = false;

        // Load the main menu by NAME (safer than index math)
        if (!SceneInBuild(mainMenuSceneName))
        {
            Debug.LogError($"[PauseMenu] Scene '{mainMenuSceneName}' is not in Build Settings / active Build Profile.");
            return;
        }
        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }

    private void EnsureEventSystemExists()
    {
        if (EventSystem.current != null) return;
        GameObject es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    private static bool SceneInBuild(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (string.Equals(name, sceneName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
