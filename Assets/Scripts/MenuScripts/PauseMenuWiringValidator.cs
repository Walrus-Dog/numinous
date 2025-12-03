using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-50)]
public class PauseMenuWiringValidator : MonoBehaviour
{
    public PauseMenu pauseMenu;             // drag your PauseMenu here, or it will find it
    [Tooltip("Try to auto-assign PauseMenuScreen by scanning canvases for a 'Resume' button.")]
    public bool autoAssignPauseScreen = true;

    void Awake()
    {
        if (!pauseMenu) pauseMenu = FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
        EnsureEventSystem();
    }

    void Start()
    {
        if (!pauseMenu)
        {
            Debug.LogError("[PM-Validator] No PauseMenu found in scene.");
            return;
        }

        if (autoAssignPauseScreen && (pauseMenu.PauseMenuScreen == null || !HasAnyButtons(pauseMenu.PauseMenuScreen)))
        {
            var candidate = FindPauseCanvasByButtons();
            if (candidate != null)
            {
                pauseMenu.PauseMenuScreen = candidate;
                Debug.Log($"[PM-Validator] Auto-assigned PauseMenuScreen => {candidate.name}");
            }
        }

        // Final report
        var scr = pauseMenu.PauseMenuScreen;
        Debug.Log($"[PM-Validator] PauseMenu='{pauseMenu.name}', PauseMenuScreen='{(scr ? scr.name : "NULL")}', HasButtons={HasAnyButtons(scr)}");
        if (scr == null)
        {
            Debug.LogWarning("[PM-Validator] PauseMenuScreen is NULL. Assign the Canvas that contains your pause buttons (Resume/Settings/Quit).");
        }
        else
        {
            // Make sure it renders on top and receives input
            var canvas = scr.GetComponentInParent<Canvas>(true);
            if (canvas)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 2000;
                if (!scr.GetComponent<GraphicRaycaster>()) scr.AddComponent<GraphicRaycaster>();
            }
        }
    }

    void Update()
    {
        // Press F1 to force show the pause panel (useful to confirm it’s the right canvas)
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            if (pauseMenu && pauseMenu.PauseMenuScreen)
            {
                pauseMenu.PauseMenuScreen.SetActive(true);
                Debug.Log("[PM-Validator] Forced PauseMenuScreen.SetActive(true)");
            }
            else
            {
                Debug.LogWarning("[PM-Validator] Cannot force show: PauseMenu or PauseMenuScreen is NULL.");
            }
        }
    }

    private GameObject FindPauseCanvasByButtons()
    {
        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (!c.gameObject.activeInHierarchy && c.enabled == false) continue;

            // look for typical pause buttons under this canvas
            var buttons = c.GetComponentsInChildren<Button>(true);
            var names = buttons.Select(b => b.name.ToLower()).ToArray();
            bool hasResume = names.Any(n => n.Contains("resume") || n == "continue");
            bool hasSettings = names.Any(n => n.Contains("settings"));
            bool hasQuit = names.Any(n => n.Contains("quit") || n.Contains("exit"));

            if (hasResume && hasSettings && hasQuit)
                return c.gameObject;
        }

        // Fallback: object named "PauseMenuScreen"
        var maybe = GameObject.Find("PauseMenuScreen");
        if (maybe != null) return maybe;

        return null;
    }

    private bool HasAnyButtons(GameObject root)
    {
        if (root == null) return false;
        return root.GetComponentsInChildren<Button>(true).Length > 0;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }
}
