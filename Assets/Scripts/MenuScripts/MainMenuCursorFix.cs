using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenuCursorFix : MonoBehaviour
{
    void Start()
    {
        // Always reset to UI cursor mode
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Guarantee EventSystem exists for menu navigation
        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        Debug.Log("[MainMenuCursorFix] Cursor unlocked and EventSystem verified.");
    }
}
