using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;        // for GraphicRaycaster
using System.Collections;

public class PauseMenu : MonoBehaviour
{
    public static bool Paused;

    [Header("UI")]
    public GameObject PauseMenuScreen;              // Canvas with Resume/Settings/Quit
    [SerializeField] private GameObject settingsMenuScreen;  // Canvas with pause Settings
    [SerializeField] private GameObject firstSelectedButton;

    [Header("Gameplay (can be left empty)")]
    public Player player;
    public PlayerInput playerInput;
    public MonoBehaviour[] extraScriptsToDisable;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    void Awake()
    {
        Paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    void Start()
    {
        EnsureEventSystemExists();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // If we're NOT paused, keep FPS-style cursor.
        if (!Paused && (Cursor.visible || Cursor.lockState != CursorLockMode.Locked))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Paused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        EnsureEventSystemExists();

        if (PauseMenuScreen != null)
        {
            PauseMenuScreen.SetActive(true);
            ForceShowPanel(PauseMenuScreen);
        }

        if (settingsMenuScreen != null)
            settingsMenuScreen.SetActive(false);

        StartCoroutine(ShowPauseMenuRoutine());
    }

    private IEnumerator ShowPauseMenuRoutine()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            if (firstSelectedButton != null)
                EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }

        Time.timeScale = 0f;
        Paused = true;

        AudioListener.pause = true;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        Paused = false;

        if (PauseMenuScreen != null)
            PauseMenuScreen.SetActive(false);
        if (settingsMenuScreen != null)
            settingsMenuScreen.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        AudioListener.pause = false;
    }

    public void OpenSettings()
    {
        if (!settingsMenuScreen)
        {
            Debug.LogWarning("[PauseMenu] OpenSettings called but settingsMenuScreen is null.");
            return;
        }

        Debug.Log("[PauseMenu] OpenSettings -> enabling settingsMenuScreen");

        if (PauseMenuScreen != null)
            PauseMenuScreen.SetActive(false);

        settingsMenuScreen.SetActive(true);
        ForceShowPanel(settingsMenuScreen);

        Canvas.ForceUpdateCanvases();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void CloseSettings()
    {
        Debug.Log("[PauseMenu] CloseSettings called");

        if (settingsMenuScreen != null)
            settingsMenuScreen.SetActive(false);

        if (PauseMenuScreen != null)
        {
            PauseMenuScreen.SetActive(true);
            ForceShowPanel(PauseMenuScreen);
        }

        Canvas.ForceUpdateCanvases();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (EventSystem.current != null && firstSelectedButton != null)
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
    }

    public void QuitButton()
    {
        Time.timeScale = 1f;
        Paused = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!SceneInBuild(mainMenuSceneName))
        {
            Debug.LogError($"[PauseMenu] Scene '{mainMenuSceneName}' is not in Build Settings / active Build Profile.");
            return;
        }
        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }

    // --- Panel visibility / raycast helper ---
    private void ForceShowPanel(GameObject root)
    {
        if (!root) return;

        // Ensure the root is active
        if (!root.activeSelf) root.SetActive(true);

        // CanvasGroup: make sure it’s visible & interactive
        var cg = root.GetComponent<CanvasGroup>();
        if (!cg) cg = root.AddComponent<CanvasGroup>();

        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        // Ensure there's a GraphicRaycaster so the panel can receive clicks
        var canvas = root.GetComponentInParent<Canvas>(true);
        if (canvas)
        {
            var ray = canvas.GetComponent<GraphicRaycaster>();
            if (!ray) ray = canvas.gameObject.AddComponent<GraphicRaycaster>();

            canvas.enabled = true;
            canvas.overrideSorting = true;
            if (canvas.sortingOrder < 2000) canvas.sortingOrder = 2000;
        }
        else
        {
            // Fallback: put a Canvas + Raycaster on the root itself
            canvas = root.GetComponent<Canvas>();
            if (!canvas) canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 2000;

            var ray = root.GetComponent<GraphicRaycaster>();
            if (!ray) root.AddComponent<GraphicRaycaster>();
        }
    }

    private void EnsureEventSystemExists()
    {
        if (EventSystem.current != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
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
