using UnityEngine;
using UnityEngine.InputSystem;

public class PauseInputListener : MonoBehaviour
{
    private PauseMenu pauseMenu;
    private bool ready = true;

    void Awake()
    {
        DontDestroyOnLoad(gameObject); // survives scene loads
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // detect ESC even when paused
        if (Keyboard.current.escapeKey.wasPressedThisFrame && ready)
        {
            StartCoroutine(Cooldown());
            if (pauseMenu == null)
                pauseMenu = FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);

            if (pauseMenu == null)
            {
                Debug.LogWarning("[PauseInputListener] No PauseMenu found in scene.");
                return;
            }

            if (pauseMenu.settingsMenuScreen != null && pauseMenu.settingsMenuScreen.activeSelf)
            {
                pauseMenu.CloseSettings();
                return;
            }

            if (PauseMenu.Paused)
                pauseMenu.ResumeGame();
            else
                pauseMenu.PauseGame();
        }
    }

    private System.Collections.IEnumerator Cooldown()
    {
        ready = false;
        yield return new WaitForSecondsRealtime(0.25f);
        ready = true;
    }
}
