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

    [Header("Gameplay (no longer required to be assigned)")]
    public Player player;
    public PlayerInput playerInput;
    public MonoBehaviour[] extraScriptsToDisable;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

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

        StartCoroutine(ShowPauseMenuRoutine());
    }

    private IEnumerator ShowPauseMenuRoutine()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        EventSystem.current?.SetSelectedGameObject(null);
        if (firstSelectedButton != null)
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);

        // core pause state
        Time.timeScale = 0f;
        Paused = true;

        // ? REMOVE these – rely on Paused flag instead
        // if (playerInput != null) playerInput.enabled = false;
        // if (player != null) player.enabled = false;
        // foreach (var s in extraScriptsToDisable) if (s != null) s.enabled = false;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        Paused = false;

        PauseMenuScreen.SetActive(false);
        if (settingsMenuScreen != null) settingsMenuScreen.SetActive(false);

        // ? Don’t fight with other systems here either
        // if (playerInput != null) playerInput.enabled = true;
        // if (player != null) player.enabled = true;
        // foreach (var s in extraScriptsToDisable) if (s != null) s.enabled = true;

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
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void CloseSettings()
    {
        if (!settingsMenuScreen) return;
        settingsMenuScreen.SetActive(false);
        PauseMenuScreen.SetActive(true);
        Canvas.ForceUpdateCanvases();
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void QuitButton()
    {
        Time.timeScale = 1f;
        Paused = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // leave other enabling/disabling to scene init / global systems

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
