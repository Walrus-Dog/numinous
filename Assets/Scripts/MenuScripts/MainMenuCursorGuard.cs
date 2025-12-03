using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

// Run *last* so nothing else can override the cursor state this frame.
[DefaultExecutionOrder(10000)]
public class MainMenuCursorGuard : MonoBehaviour
{
    [Header("Behavior")]
    [Tooltip("Continuously enforce a UI-friendly cursor (visible/unlocked). Recommended: ON.")]
    public bool enforceEveryFrame = true;

    [Tooltip("Disable PlayerInput components even if they live in DontDestroyOnLoad.")]
    public bool disableDDOLPlayerInputs = true;

    [Tooltip("Also disable these gameplay scripts while on the Main Menu (drag FPC, Camera rigs, etc.).")]
    public MonoBehaviour[] disableOnMenu;

    [Header("Optional Focus")]
    [Tooltip("Selectable to focus when the menu loads (e.g., your first button).")]
    public Selectable firstSelected;

#if ENABLE_INPUT_SYSTEM
    private readonly List<PlayerInput> _disabledInputs = new();
#endif
    private readonly List<(MonoBehaviour mb, bool wasEnabled)> _disabledMBs = new();
    private Coroutine _enforceLoop;

    private void Awake()
    {
        // 1) Menu-safe baseline
        Time.timeScale = 1f;
        ForceUICursor();

        // 2) Heal EventSystem so buttons receive clicks
        EnsureEventSystem();

        // 3) Clear any stray Pause flag (best-effort; safe if not present)
        TryClearPauseFlag();

        // 4) Neutralize gameplay input that might relock cursor
        DisableGameplayStuff();

        // 5) Optional focus
        if (firstSelected) firstSelected.Select();
    }

    private void OnEnable()
    {
        // Start late-frame enforcement so we beat any LateUpdate relockers
        if (enforceEveryFrame && _enforceLoop == null)
            _enforceLoop = StartCoroutine(KeepCursorVisible());
    }

    private void OnDisable()
    {
        if (_enforceLoop != null) { StopCoroutine(_enforceLoop); _enforceLoop = null; }
    }

    private void OnDestroy()
    {
        // Restore anything we disabled so other scenes function normally
#if ENABLE_INPUT_SYSTEM
        foreach (var pi in _disabledInputs)
            if (pi) pi.enabled = true;
        _disabledInputs.Clear();
#endif
        foreach (var (mb, wasEnabled) in _disabledMBs)
            if (mb) mb.enabled = wasEnabled;
        _disabledMBs.Clear();
    }

    private System.Collections.IEnumerator KeepCursorVisible()
    {
        // Run at the very end of each frame
        while (enabled && gameObject.activeInHierarchy)
        {
            yield return new WaitForEndOfFrame();
            ForceUICursor();
        }
    }

    private static void ForceUICursor()
    {
        if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
        if (!Cursor.visible) Cursor.visible = true;
        Time.timeScale = 1f; // keep menus unpaused
    }

    private void DisableGameplayStuff()
    {
#if ENABLE_INPUT_SYSTEM
        // Disable ALL PlayerInput components we can see (incl. inactive & DDOL if requested)
        var inputs = Object.FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var pi in inputs)
        {
            if (!disableDDOLPlayerInputs && !pi.gameObject.scene.IsValid()) continue; // skip DDOL if opted out
            if (pi.enabled)
            {
                pi.enabled = false;
                _disabledInputs.Add(pi);
            }
        }
#endif
        // Disable extra scripts you’ve specified
        if (disableOnMenu != null)
        {
            foreach (var mb in disableOnMenu)
            {
                if (!mb) continue;
                _disabledMBs.Add((mb, mb.enabled));
                mb.enabled = false;
            }
        }
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }

    private static void TryClearPauseFlag()
    {
        // Best-effort: clear common static Pause flag if it exists
        try
        {
            var t = System.Type.GetType("PauseMenu");
            t?.GetField("Paused")?.SetValue(null, false);
        }
        catch { /* ignore */ }
    }
}
