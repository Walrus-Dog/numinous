using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GameplaySceneInitializer : MonoBehaviour
{
    [Tooltip("Optionally assign your PauseMenu in the Inspector. If left empty, we'll find it.")]
    public PauseMenu pauseMenuExplicit;

    private void Awake()
    {
        // Start gameplay unpaused with FPS-style cursor.
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        EnsureEventSystem();
    }

    private void Start()
    {
        // Bind after one frame so spawned Player exists.
        StartCoroutine(BindPauseMenuNextFrame());
    }

    private IEnumerator BindPauseMenuNextFrame()
    {
        yield return null;

        var pm = pauseMenuExplicit
                 ? pauseMenuExplicit
                 : Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);

        if (!pm)
        {
            Debug.LogWarning("[GameplaySceneInitializer] No PauseMenu found in this scene.");
            yield break;
        }

        // Ensure pause menu starts closed
        if (pm.PauseMenuScreen != null)
            pm.PauseMenuScreen.SetActive(false);

        // Rebind Player
        if (pm.player == null)
        {
            var player = Object.FindFirstObjectByType<Player>(FindObjectsInactive.Exclude);
            if (player != null) pm.player = player;
        }

        // Rebind gameplay PlayerInput (not the UI/EventSystem one)
        if (pm.playerInput == null)
        {
            PlayerInput candidate = null;

            if (pm.player != null)
                candidate = pm.player.GetComponent<PlayerInput>();

            if (candidate == null)
            {
                var all = Object.FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var pi in all)
                {
                    if (pi.GetComponent<EventSystem>() == null) { candidate = pi; break; }
                }
            }

            if (candidate != null) pm.playerInput = candidate;
        }

        // Make sure gameplay input is enabled
        if (pm.playerInput != null && !pm.playerInput.enabled) pm.playerInput.enabled = true;
        if (pm.player != null && !pm.player.enabled) pm.player.enabled = true;

        Debug.Log("[GameplaySceneInitializer] PauseMenu bound and gameplay input ready.");
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
