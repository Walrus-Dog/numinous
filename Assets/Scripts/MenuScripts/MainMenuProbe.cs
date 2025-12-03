using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// Runs early so it can heal menu state on scene load
[DefaultExecutionOrder(-9999)]
public class MainMenuProbe : MonoBehaviour
{
    [Header("Optional: highlight this first")]
    public Selectable firstSelect;

    [Header("Auto-heal on Awake")]
    public bool autoHeal = true;

    void Awake()
    {
        if (autoHeal) HealMenu();
    }

    void Start()
    {
        if (firstSelect != null)
            EventSystem.current?.SetSelectedGameObject(firstSelect.gameObject);
    }

    // Call this from the Inspector (context menu) if you want to retry
    [ContextMenu("Heal Menu Now")]
    public void HealMenu()
    {
        // 1) Force unpaused
        Time.timeScale = 1f;
        try { PauseMenu.Paused = false; } catch { }

        // 2) Cursor for UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3) EventSystem
        EnsureEventSystem();

        // 4) Enable Canvas raycasts in this scene
        EnableAllMenuRaycasts();

        // 5) Kill any Dropdown blockers that might be covering the screen
        KillDropdownArtifacts();

        // 6) Nudge canvases/selection
        Canvas.ForceUpdateCanvases();
        EventSystem.current?.SetSelectedGameObject(null);

        Debug.Log($"[MainMenuProbe] Heal complete. timeScale={Time.timeScale}, pausedFlag={(GetPausedFlag() ? "true" : "false")}");
    }

    void Update()
    {
        // Quick status log when you press F1
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log($"[MainMenuProbe] timeScale={Time.timeScale}, pausedFlag={(GetPausedFlag() ? "true" : "false")}, hasEventSystem={(EventSystem.current != null)}");
        }
    }

    static bool GetPausedFlag()
    {
        try { return PauseMenu.Paused; } catch { return false; }
    }

    static void EnsureEventSystem()
    {
        // Find ALL EventSystems, even hidden or in DontDestroyOnLoad
        var allES = Resources.FindObjectsOfTypeAll<EventSystem>();
        EventSystem inScene = null;

        // First, try to pick one that lives in the current scene
        foreach (var es in allES)
        {
            if (es == null) continue;
            if (es.gameObject.scene.IsValid())
            {
                inScene = es;
                break;
            }
        }

        // If none in scene, use any existing (DDOL) one as primary
        if (inScene == null && allES.Length > 0) inScene = allES[0];

        // Disable/destroy all other EventSystems so only one is active
        foreach (var es in allES)
        {
            if (es == null) continue;
            if (es == inScene) continue;

            // Prefer destroying ones not in the current scene (DDOL leftovers)
            if (!es.gameObject.scene.IsValid())
            {
                Object.Destroy(es.gameObject);
            }
            else
            {
                es.enabled = false;
                var gr = es.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (gr) gr.enabled = false;
            }
        }

        // If we still don't have one, create it now
        if (inScene == null)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
            inScene = go.GetComponent<EventSystem>();
        }
        else
        {
            // Make sure the input module exists/enabled on the kept ES
#if ENABLE_INPUT_SYSTEM
            var newIM = inScene.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (!newIM) inScene.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        var oldIM = inScene.GetComponent<StandaloneInputModule>();
        if (!oldIM) inScene.gameObject.AddComponent<StandaloneInputModule>();
#endif
            inScene.enabled = true;
        }
    }


    static void EnableAllMenuRaycasts()
    {
        foreach (var cg in Resources.FindObjectsOfTypeAll<CanvasGroup>())
        {
            if (!cg.gameObject.scene.IsValid()) continue; // skip DontDestroyOnLoad
            cg.alpha = Mathf.Max(cg.alpha, 1f);
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
        foreach (var gr in Resources.FindObjectsOfTypeAll<GraphicRaycaster>())
        {
            if (!gr.gameObject.scene.IsValid()) continue;
            gr.enabled = true;
        }
    }

    static void KillDropdownArtifacts()
    {
        var gos = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in gos)
        {
            if (!go) continue;
            if (go.name == "TMP Dropdown Blocker" || go.name == "TMP Dropdown List" ||
                go.name == "Blocker" || go.name == "Dropdown List")
            {
                if (string.IsNullOrEmpty(go.scene.name)) go.SetActive(false); // DontDestroyOnLoad
                else Object.Destroy(go);
            }
        }
    }
}
