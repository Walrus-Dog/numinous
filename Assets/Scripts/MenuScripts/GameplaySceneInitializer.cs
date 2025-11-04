using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(1000)] // run after most other scripts
public class GameplaySceneInitializer : MonoBehaviour
{
    [Tooltip("Optionally assign your PauseMenu in the Inspector. If left empty, we'll find it.")]
    public PauseMenu pauseMenuExplicit;

    private PauseMenu _pauseMenu;

    private void Awake()
    {
        // Start gameplay unpaused with FPS-style cursor.
        Time.timeScale = 1f;
        PauseMenu.Paused = false;

        ForceGameplayCursor();
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

        _pauseMenu = pauseMenuExplicit
                     ? pauseMenuExplicit
                     : Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);

        if (!_pauseMenu)
        {
            Debug.LogWarning("[GameplaySceneInitializer] No PauseMenu found in this scene.");
            yield break;
        }

        // Ensure pause menu starts closed
        if (_pauseMenu.PauseMenuScreen != null)
            _pauseMenu.PauseMenuScreen.SetActive(false);

        // Rebind Player
        if (_pauseMenu.player == null)
        {
            var player = Object.FindFirstObjectByType<Player>(FindObjectsInactive.Exclude);
            if (player != null) _pauseMenu.player = player;
        }

        // Rebind gameplay PlayerInput (not the UI/EventSystem one)
        if (_pauseMenu.playerInput == null)
        {
            PlayerInput candidate = null;

            if (_pauseMenu.player != null)
                candidate = _pauseMenu.player.GetComponent<PlayerInput>();

            if (candidate == null)
            {
                var all = Object.FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var pi in all)
                {
                    if (pi.GetComponent<EventSystem>() == null) { candidate = pi; break; }
                }
            }

            if (candidate != null) _pauseMenu.playerInput = candidate;
        }

        // Make sure gameplay input is enabled
        if (_pauseMenu.playerInput != null && !_pauseMenu.playerInput.enabled) _pauseMenu.playerInput.enabled = true;
        if (_pauseMenu.player != null && !_pauseMenu.player.enabled) _pauseMenu.player.enabled = true;

        Debug.Log("[GameplaySceneInitializer] PauseMenu bound and gameplay input ready.");
    }

    private void LateUpdate()
    {
        // While NOT paused, always enforce gameplay cursor (FPS-style).
        if (!PauseMenu.Paused)
        {
            ForceGameplayCursor();
        }
    }

    private static void ForceGameplayCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
