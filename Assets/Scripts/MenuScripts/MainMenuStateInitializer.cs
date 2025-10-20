using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainMenuStateInitializer : MonoBehaviour
{
    [Header("Focus")]
    public GameObject firstSelected; // your default highlighted button (assign in Inspector)

    private void OnEnable()
    {
        StartCoroutine(InitMenu());
    }

    private IEnumerator InitMenu()
    {
        // 1) Menu-friendly runtime state
        Time.timeScale = 1f;
        PauseMenu.Paused = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 2) EventSystem hygiene
        EnsureSceneEventSystem();

        // 3) Disable any lingering PlayerInput (e.g., gameplay input)
        var inputs = Object.FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var pi in inputs)
        {
            // Keep enabled ONLY if it’s on the active EventSystem (UI input)
            bool isOnEventSystem = pi.GetComponent<EventSystem>() != null && pi.gameObject.scene == gameObject.scene;
            if (!isOnEventSystem) pi.enabled = false;
        }

        // 4) Let UI rebuild, then focus the first button
        yield return null;
        Canvas.ForceUpdateCanvases();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            if (firstSelected != null)
                EventSystem.current.SetSelectedGameObject(firstSelected);
        }
    }

    private void EnsureSceneEventSystem()
    {
        var thisScene = gameObject.scene;
        // A) Remove EventSystems that aren't in this scene (e.g., leftovers from gameplay)
        var all = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var es in all)
        {
            if (es == null) continue;
            if (es.gameObject.scene != thisScene)
            {
                Object.Destroy(es.gameObject);
            }
        }

        // B) Ensure exactly one EventSystem in this scene
        var sceneES = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                            .Where(es => es != null && es.gameObject.scene == thisScene)
                            .ToList();

        EventSystem current;
        if (sceneES.Count == 0)
        {
            var go = new GameObject("EventSystem (Menu)", typeof(EventSystem));
            go.scene.GetRootGameObjects(); // ensure it belongs to the active scene
            current = go.GetComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
        else
        {
            current = sceneES[0];
            // Disable any extras in this scene
            for (int i = 1; i < sceneES.Count; i++)
                sceneES[i].gameObject.SetActive(false);

            // C) Make sure it has the correct UI module
#if ENABLE_INPUT_SYSTEM
            var legacy = current.GetComponent<StandaloneInputModule>();
            if (legacy) Object.Destroy(legacy);
            if (!current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>())
                current.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            var inputSys = current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (inputSys) Object.Destroy(inputSys);
            if (!current.GetComponent<StandaloneInputModule>())
                current.gameObject.AddComponent<StandaloneInputModule>();
#endif
            current.gameObject.SetActive(true);
        }

        // D) Make this the active EventSystem
        if (EventSystem.current != current)
        {
            // Easiest way: temporarily disable/enable to force it to become current
            current.enabled = false;
            current.enabled = true;
        }
    }
}
