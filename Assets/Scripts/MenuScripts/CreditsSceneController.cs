using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class CreditsSceneController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    // If you have a PauseMenu static flag, we’ll clear it safely (optional).
    [SerializeField] private bool tryClearPauseMenu = true;

    void Awake()
    {
        EnsureEventSystem();
        EnsureMenuCursor(true);
        HealGameplayState();
    }

    void OnEnable()
    {
        EnsureMenuCursor(true);
        HealGameplayState();
    }

    void Update()
    {
        // Defensive: if something re-hides/locks the cursor, we flip it back.
        if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
            EnsureMenuCursor(true);

        // Esc to go back
        if (Input.GetKeyDown(KeyCode.Escape)) BackToMenu();
    }

    public void BackToMenu()
    {
        EnsureMenuCursor(true);
        if (!SceneInBuild(mainMenuSceneName))
        {
            Debug.LogError($"Main menu scene '{mainMenuSceneName}' not in Build Settings.");
            return;
        }
        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
    }

    private void EnsureMenuCursor(bool on)
    {
        Time.timeScale = 1f;
        Cursor.lockState = on ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = on;
    }

    private void HealGameplayState()
    {
        // If you have a global PauseMenu or similar lingering between scenes, unpause it.
        if (tryClearPauseMenu)
        {
            // Safely clear a common static flag if it exists.
            // Wrap in try/catch to avoid compile errors if PauseMenu isn’t in this project.
            try
            {
                var type = System.Type.GetType("PauseMenu");
                if (type != null)
                {
                    var pausedField = type.GetField("Paused");
                    if (pausedField != null && pausedField.FieldType == typeof(bool))
                        pausedField.SetValue(null, false);

                    // Try calling ResumeGame() on an instance if one exists.
                    var pm = Object.FindFirstObjectByType(type);
                    if (pm != null)
                    {
                        var m = type.GetMethod("ResumeGame");
                        if (m != null) m.Invoke(pm, null);
                    }
                }
            }
            catch { /* ignore – purely best-effort */ }
        }
    }

    private static void EnsureEventSystem()
    {
        if (!EventSystem.current)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }
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
