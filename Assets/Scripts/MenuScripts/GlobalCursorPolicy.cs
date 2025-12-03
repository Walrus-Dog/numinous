using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public class GlobalCursorPolicy : MonoBehaviour
{
    [Header("Menu Scenes (names)")]
    [Tooltip("Any scene here enforces a visible/unlocked cursor on load.")]
    public string[] menuSceneNames = new[] { "MainMenu", "Credits" };

    [Header("Menu Enforcement")]
    [Tooltip("How many frames to enforce after a menu scene loads.")]
    public int enforceMenuFramesAfterLoad = 60;      // ~1s @60fps
    [Tooltip("Keep enforcing every frame while on a menu scene.")]
    public bool enforceContinuouslyOnMenu = true;

    [Header("Gameplay Enforcement")]
    [Tooltip("Enforce hidden/locked cursor for non-menu scenes after load.")]
    public bool enforceGameplayCursorOnNonMenu = true;
    [Tooltip("How many frames to enforce after a gameplay scene loads.")]
    public int enforceGameplayFramesAfterLoad = 30;  // ~0.5s @60fps

    [Header("Disable inputs on menu")]
    [Tooltip("Disable PlayerInput components (even in DontDestroyOnLoad) while on menu scenes.")]
    public bool disableAllPlayerInputsOnMenu = true;

    [Tooltip("Also disable these behaviours while on menu scenes (e.g., FirstPersonController).")]
    public MonoBehaviour[] extraBehavioursToDisableOnMenu;

#if ENABLE_INPUT_SYSTEM
    private readonly List<PlayerInput> _disabledInputs = new();
#endif
    private readonly List<(MonoBehaviour mb, bool wasEnabled)> _disabledBehaviours = new();

    private static GlobalCursorPolicy _instance;

    private void Awake()
    {
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsMenuScene(scene.name))
        {
            // Prepare menu and enforce menu cursor
            PrepareMenuUI();
            StartCoroutine(EnforceMenuForFrames(enforceMenuFramesAfterLoad));
            if (enforceContinuouslyOnMenu) StartCoroutine(ContinuousMenuEnforcement());
        }
        else
        {
            // Leaving menu ? restore inputs/behaviours first
            RestoreDisabled();

            // Optionally enforce gameplay cursor briefly so it wins the race
            if (enforceGameplayCursorOnNonMenu)
                StartCoroutine(EnforceGameplayForFrames(enforceGameplayFramesAfterLoad));
        }
    }

    // -------- Menu side --------
    private IEnumerator EnforceMenuForFrames(int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            yield return new WaitForEndOfFrame();
            ForceMenuCursor();
        }
    }
    private IEnumerator ContinuousMenuEnforcement()
    {
        while (IsMenuScene(SceneManager.GetActiveScene().name))
        {
            yield return new WaitForEndOfFrame();
            ForceMenuCursor();
        }
    }
    private void PrepareMenuUI()
    {
        ForceMenuCursor();

        if (!EventSystem.current)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        // Best-effort clear of Pause flag
        try { var t = System.Type.GetType("PauseMenu"); t?.GetField("Paused")?.SetValue(null, false); } catch { }

#if ENABLE_INPUT_SYSTEM
        if (disableAllPlayerInputsOnMenu)
        {
            var inputs = Object.FindObjectsByType<PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var pi in inputs)
            {
                if (pi && pi.enabled)
                {
                    pi.enabled = false;
                    _disabledInputs.Add(pi);
                }
            }
        }
#endif
        if (extraBehavioursToDisableOnMenu != null)
        {
            foreach (var mb in extraBehavioursToDisableOnMenu)
            {
                if (!mb) continue;
                _disabledBehaviours.Add((mb, mb.enabled));
                mb.enabled = false;
            }
        }
    }

    // -------- Gameplay side --------
    private IEnumerator EnforceGameplayForFrames(int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            yield return new WaitForEndOfFrame();
            ForceGameplayCursor();
        }
    }

    private void RestoreDisabled()
    {
#if ENABLE_INPUT_SYSTEM
        foreach (var pi in _disabledInputs) if (pi) pi.enabled = true;
        _disabledInputs.Clear();
#endif
        foreach (var (mb, wasEnabled) in _disabledBehaviours) if (mb) mb.enabled = wasEnabled;
        _disabledBehaviours.Clear();
    }

    // -------- Helpers --------
    private static void ForceMenuCursor()
    {
        Time.timeScale = 1f;
        if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
        if (!Cursor.visible) Cursor.visible = true;
    }

    private static void ForceGameplayCursor()
    {
        // Don’t force timescale here; let gameplay control that.
        if (Cursor.lockState != CursorLockMode.Locked) Cursor.lockState = CursorLockMode.Locked;
        if (Cursor.visible) Cursor.visible = false;
    }

    private bool IsMenuScene(string name)
    {
        if (menuSceneNames == null || menuSceneNames.Length == 0) return false;
        for (int i = 0; i < menuSceneNames.Length; i++)
        {
            if (string.Equals(menuSceneNames[i], name, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
