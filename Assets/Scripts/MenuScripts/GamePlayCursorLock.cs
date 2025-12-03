using UnityEngine;

public class GameplayCursorLock : MonoBehaviour
{
    void Awake()
    {
        // Enter gameplay in FPS mode
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // If the window regains focus, re-lock the cursor (as long as we’re not paused)
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && !PauseMenu.Paused)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
