using UnityEngine;

public class PauseSettingsBackButton : MonoBehaviour
{
    // Optional – you can leave this unassigned and we’ll auto-find.
    public PauseMenu pauseMenu;

    // Hook THIS to the Back button’s OnClick.
    public void OnBack()
    {
        if (!pauseMenu)
        {
            pauseMenu = FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
        }

        if (pauseMenu)
        {
            Debug.Log("[PauseSettingsBackButton] Back clicked -> CloseSettings()");
            pauseMenu.CloseSettings();
        }
        else
        {
            Debug.LogWarning("[PauseSettingsBackButton] No PauseMenu found in scene.");
        }
    }
}
