using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MenuCursorGuard : MonoBehaviour
{
    [Header("Menu Scenes")]
    [Tooltip("Names of scenes where the mouse must stay unlocked/visible and UI must own input.")]
    public string[] menuSceneNames = new[] { "numinousv6menu" }; // set your menu scene name(s) here

    [Header("UI Focus")]
    public GameObject firstSelected; // optional: default highlighted button

    void Awake()
    {
        // Initial force for safety on scene entry
        ForceMenuState();
        EnsureEventSystemAndUIInput();
        FocusFirst();
    }

    void LateUpdate()
    {
        // Win the last-write-wins battle every frame in menu scenes
        if (IsInMenuScene())
        {
            ForceMenuState();
            EnsureEventSystemAndUIInput();
        }
    }

    private bool IsInMenuScene()
    {
        var s = gameObject.scene.name;
        return menuSceneNames != null && menuSceneNames.Contains(s);
    }

    private void ForceMenuState()
    {
        Time.timeScale = 1f;
        PauseMenu.Paused = false;

        // Cursor must be unlocked + visible for menus
        if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
        if (!Cursor.visible) Cursor.visible = true;
    }

    private void EnsureEventSystemAndUIInput()
    {
        // Ensure EventSystem exists
        if (EventSystem.current == null)
        {
            var esGO = new GameObject("EventSystem (Menu)", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            esGO.AddComponent<StandaloneInputModule>();
#endif
        }

        // Kill any EventSystems that are NOT in this scene (leftovers from gameplay)
        var thisScene = gameObject.scene;
        var allES = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var es in allES)
        {
            if (es.gameObject.scene != thisScene)
                Destroy(es.gameObject);
        }

        // Make sure the UI module matches your input backend
#if ENABLE_INPUT_SYSTEM
        var legacy = EventSystem.current.GetComponent<StandaloneInputModule>();
        if (legacy) Destroy(legacy);
        if (!EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>())
            EventSystem.current.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        var inputSys = EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (inputSys) Destroy(inputSys);
        if (!EventSystem.current.GetComponent<StandaloneInputModule>())
            EventSystem.current.gameObject.AddComponent<StandaloneInputModule>();
#endif

        // Disable any PlayerInput not attached to the (menu) EventSystem in this scene
        var inputs = Object.FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var pi in inputs)
        {
            bool isMenuES = pi.GetComponent<EventSystem>() != null && pi.gameObject.scene == thisScene;
            if (!isMenuES) pi.enabled = false;

#if ENABLE_INPUT_SYSTEM
            // If a UI action map exists, force it
            if (isMenuES && pi.actions != null)
            {
                var uiMap = pi.actions.actionMaps.FirstOrDefault(m => m.name.ToLower().Contains("ui"));
                if (uiMap != null && pi.currentActionMap != uiMap)
                    uiMap.Enable(); // switches to UI map
            }
#endif
        }
    }

    private void FocusFirst()
    {
        if (EventSystem.current != null && firstSelected != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelected);
        }
    }
}
