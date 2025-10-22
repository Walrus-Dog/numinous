using UnityEngine;
using UnityEngine.UI;

public class PauseMenuBinder : MonoBehaviour
{
    public PauseMenu pauseMenu;     // drag your Level2 PauseMenu here
    public Button resumeButton;     // drag the actual Resume Button here

    [ContextMenu("Bind PauseMenuScreen from Resume Button")]
    public void BindNow()
    {
        if (!pauseMenu) pauseMenu = FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
        if (!pauseMenu) { Debug.LogError("[PM-Binder] No PauseMenu found."); return; }
        if (!resumeButton) { Debug.LogError("[PM-Binder] Please assign the Resume Button."); return; }

        var canvas = resumeButton.GetComponentInParent<Canvas>(true);
        if (!canvas)
        {
            Debug.LogError("[PM-Binder] Resume Button has no parent Canvas.");
            return;
        }

        pauseMenu.PauseMenuScreen = canvas.gameObject;
        Debug.Log($"[PM-Binder] Bound PauseMenuScreen => {canvas.gameObject.name}");
    }
}
