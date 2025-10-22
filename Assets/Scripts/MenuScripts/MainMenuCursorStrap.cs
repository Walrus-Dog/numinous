using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

// Run early, keep cursor unlocked/visible, and optionally disable gameplay scripts.
[DefaultExecutionOrder(-9998)]
public class MainMenuCursorGuard : MonoBehaviour
{
    [Header("Optional: disable these gameplay scripts in Main Menu")]
    public MonoBehaviour[] disableOnMenu;  // drag in Player, PlayerInput, FirstPersonController, etc.

    void Awake()
    {
        // Make sure we're not paused (belt & suspenders)
        Time.timeScale = 1f;
        try { PauseMenu.Paused = false; } catch { }

        // Kill any PlayerInput components that might re-lock the cursor
#if ENABLE_INPUT_SYSTEM
        var inputs = Object.FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var pi in inputs)
        {
            if (!pi.gameObject.scene.IsValid()) continue; // ignore DDOL? (we'll still force cursor below)
            pi.enabled = false;
        }
#endif

        // Disable any other gameplay scripts you know about
        foreach (var mb in disableOnMenu)
            if (mb) mb.enabled = false;

        // Start with cursor in UI mode
        ForceUICursor();
    }

    void OnEnable() { ForceUICursor(); }
    void OnApplicationFocus(bool focus)
    {
        if (focus) ForceUICursor();
    }

    void Update()
    {
        // If *anything* tries to re-lock the cursor on click, we undo it immediately
        if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            ForceUICursor();

        // Extra safety: if an EventSystem exists but nothing is selected, don’t auto-hide
        // (Some controllers auto-hide when selection is null)
        if (EventSystem.current == null)
        {
            var esGO = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            esGO.AddComponent<InputSystemUIInputModule>();
#else
            esGO.AddComponent<StandaloneInputModule>();
#endif
        }
    }

    private static void ForceUICursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
