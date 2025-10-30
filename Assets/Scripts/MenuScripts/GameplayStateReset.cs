using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public static class GameplayStateReset
{
    /// <summary>
    /// Hard-restore to a clean, unpaused, gameplay-ready state.
    /// Safe to call from anywhere (pause menu, save/load, main menu, etc).
    /// </summary>
    public static void ResetToGameplay(bool hidePauseUI = true)
    {
        // --- 1) Time & global flags ---
        Time.timeScale = 1f;
        PauseMenu.Paused = false;

        // --- 2) Hide any Pause UI in the scene (even if inactive) ---
        if (hidePauseUI)
        {
            var menus = UnityEngine.Object.FindObjectsByType<PauseMenu>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var pm in menus)
            {
                if (pm == null) continue;

                // Public PauseMenuScreen (if present)
                try
                {
                    if (pm.PauseMenuScreen) pm.PauseMenuScreen.SetActive(false);
                }
                catch { /* ignore if not present */ }

                // Try to hide settings panel via reflection (works if field exists)
                var settingsGo = TryGetSettingsMenuGO(pm);
                if (settingsGo) settingsGo.SetActive(false);

                // Re-enable known gameplay script refs if exposed publicly
                try { if (pm.player) pm.player.enabled = true; } catch { /* ignore */ }

                // Re-enable any custom extras array if it exists (via reflection, optional)
                var extras = TryGetExtraScriptsArray(pm);
                if (extras != null)
                {
                    foreach (var mb in extras)
                        if (mb != null) mb.enabled = true;
                }
            }
        }

        // --- 3) Ensure EventSystem exists ---
        EnsureEventSystem();

        // --- 4) Re-enable PlayerInput(s) & switch to gameplay map (new Input System) ---
#if ENABLE_INPUT_SYSTEM
        var inputs = UnityEngine.Object.FindObjectsByType<PlayerInput>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var pi in inputs)
        {
            if (pi == null) continue;
            if (!pi.enabled) pi.enabled = true;

            var playerMap = pi.actions?.FindActionMap("Player", false);
            if (playerMap != null && pi.currentActionMap != playerMap)
                pi.SwitchCurrentActionMap("Player");
        }
#endif

        // --- 5) Re-enable Player MonoBehaviours (safety net) ---
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            foreach (var mb in playerGO.GetComponents<MonoBehaviour>())
                if (mb != null && !mb.enabled) mb.enabled = true;
        }

        // --- 6) Cursor: gameplay state (locked/hidden) ---
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // --- 7) Clear UI selection ---
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);

#if UNITY_EDITOR
        Debug.Log("[GameplayStateReset] Hard resume applied. timeScale=1, cursor locked, inputs enabled.");
#endif
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    // ===== Helpers that don’t assume fields exist =====

    /// <summary>
    /// Try to fetch a field called 'settingsMenuScreen' from PauseMenu (any access level).
    /// Returns the GameObject if found; otherwise null.
    /// </summary>
    private static GameObject TryGetSettingsMenuGO(PauseMenu pm)
    {
        if (pm == null) return null;

        var f = pm.GetType().GetField(
            "settingsMenuScreen",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (f != null && f.FieldType == typeof(GameObject))
            return (GameObject)f.GetValue(pm);

        return null;
    }

    /// <summary>
    /// Try to fetch a field called 'extraScriptsToDisable' from PauseMenu (any access level).
    /// Returns MonoBehaviour[] if found; otherwise null.
    /// </summary>
    private static MonoBehaviour[] TryGetExtraScriptsArray(PauseMenu pm)
    {
        if (pm == null) return null;

        var f = pm.GetType().GetField(
            "extraScriptsToDisable",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (f != null && typeof(MonoBehaviour[]).IsAssignableFrom(f.FieldType))
            return (MonoBehaviour[])f.GetValue(pm);

        return null;
    }
}
