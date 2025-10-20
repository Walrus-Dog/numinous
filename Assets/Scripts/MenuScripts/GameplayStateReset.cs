using UnityEngine;
using UnityEngine.EventSystems;

public static class GameplayStateReset
{
    /// <summary>
    /// Force the game into a clean, unpaused, gameplay-ready state.
    /// Safe to call any time (before load, after load, from menus).
    /// </summary>
    public static void ResetToGameplay()
    {
        // 1) Time & flags
        Time.timeScale = 1f;
        PauseMenu.Paused = false;

        // 2) If a PauseMenu exists, resume via its own logic.
        var pm = Object.FindFirstObjectByType<PauseMenu>();
        if (pm != null)
        {
            pm.ResumeGame();
        }
        else
        {
            // Fallback if PauseMenu not present yet
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }

        // 3) Ensure an EventSystem exists
        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
