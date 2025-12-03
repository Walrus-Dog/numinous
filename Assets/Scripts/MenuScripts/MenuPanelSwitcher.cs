using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuPanelSwitcher : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuRoot;       // Main menu-only UI
    [SerializeField] private GameObject settingsMenuRoot;   // Settings UI root
    [SerializeField] private GameObject creditsMenuRoot;    // Credits UI root
    [SerializeField] private GameObject loadMenuRoot;       // NEW: Load UI root

    [Header("First-selected (keyboard / gamepad)")]
    [SerializeField] private Selectable firstOnMain;
    [SerializeField] private Selectable firstOnSettings;
    [SerializeField] private Selectable firstOnCredits;
    [SerializeField] private Selectable firstOnLoad;        // NEW

    [Header("Always keep these active (e.g., Music)")]
    [SerializeField] private GameObject[] keepAlive;

    [Header("Hide these when NOT on Main (e.g., NuminousImage, main BGs)")]
    [SerializeField] private GameObject[] hideWhenNotMain;

    [Header("Auto-find fallback names (optional)")]
    [SerializeField] private string mainMenuName = "MainMenu";
    [SerializeField] private string settingsMenuName = "SettingsMenuScreen";
    [SerializeField] private string creditsMenuName = "CreditsMenuScreen";
    [SerializeField] private string loadMenuName = "LoadMenuScreen"; // NEW

    [Header("Startup")]
    [SerializeField] private StartPanel defaultPanel = StartPanel.Main;

    [Header("Debug")]
    [SerializeField] private bool verbose = false;

    private enum Panel { Main, Settings, Credits, Load }
    public enum StartPanel { Main, Settings, Credits, Load }

    private void Awake()
    {
        // Ensure menus start in a clean state
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Ensure an EventSystem exists
        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
            if (verbose) Debug.Log("[MenuPanelSwitcher] Created EventSystem");
        }

        // Auto-wire by names if fields are unassigned
        if (!mainMenuRoot) mainMenuRoot = GameObject.Find(mainMenuName);
        if (!settingsMenuRoot) settingsMenuRoot = GameObject.Find(settingsMenuName);
        if (!creditsMenuRoot) creditsMenuRoot = GameObject.Find(creditsMenuName);
        if (!loadMenuRoot) loadMenuRoot = GameObject.Find(loadMenuName);

        // Force a single-active panel on startup
        switch (defaultPanel)
        {
            case StartPanel.Settings: TogglePanels(Panel.Settings); break;
            case StartPanel.Credits: TogglePanels(Panel.Credits); break;
            case StartPanel.Load: TogglePanels(Panel.Load); break;
            default: TogglePanels(Panel.Main); break;
        }
    }

    // ========== Public API (wire buttons to these) ==========
    public void ShowMain() => TogglePanels(Panel.Main);
    public void ShowSettings() => TogglePanels(Panel.Settings);
    public void ShowCredits() => TogglePanels(Panel.Credits);
    public void ShowLoad() => TogglePanels(Panel.Load);   // NEW
    public void OnBackToMain() => ShowMain();
    // ========================================================

    private void TogglePanels(Panel target)
    {
        if (verbose) Debug.Log($"[MenuPanelSwitcher] Toggle -> {target}");

        // Activate only the target panel
        SafeSetActive(mainMenuRoot, target == Panel.Main);
        SafeSetActive(settingsMenuRoot, target == Panel.Settings);
        SafeSetActive(creditsMenuRoot, target == Panel.Credits);
        SafeSetActive(loadMenuRoot, target == Panel.Load);

        // Heal target panel visibility (CanvasGroup/Raycaster/Canvas) if needed
        switch (target)
        {
            case Panel.Settings: if (settingsMenuRoot) ForceVisible(settingsMenuRoot); break;
            case Panel.Credits: if (creditsMenuRoot) ForceVisible(creditsMenuRoot); break;
            case Panel.Load: if (loadMenuRoot) ForceVisible(loadMenuRoot); break;
        }

        // Keep-alive objects always on (e.g., Music)
        if (keepAlive != null)
            foreach (var go in keepAlive) SafeSetActive(go, true);

        // Objects that should only show on Main (e.g., NuminousImage)
        bool showMainExtras = (target == Panel.Main);
        if (hideWhenNotMain != null)
            foreach (var go in hideWhenNotMain) SafeSetActive(go, showMainExtras);

        // Controller/keyboard focus
        EventSystem.current?.SetSelectedGameObject(null);
        Selectable first =
            target == Panel.Main ? firstOnMain :
            target == Panel.Settings ? firstOnSettings :
            target == Panel.Credits ? firstOnCredits :
                                       firstOnLoad;

        if (first) first.Select();

        // Cursor for menus
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (verbose) DebugDump();
    }

    private static void SafeSetActive(GameObject go, bool state)
    {
        if (!go) return;
        if (go.activeSelf != state) go.SetActive(state);
    }

    private void ForceVisible(GameObject root)
    {
        // Make sure the target panel can render & receive input
        var canvasGroup = root.GetComponent<CanvasGroup>();
        if (canvasGroup)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        var ray = root.GetComponentInParent<GraphicRaycaster>(true);
        if (ray) ray.enabled = true;

        // If the panel has its own Canvas, ensure it isn't behind everything
        var canvas = root.GetComponent<Canvas>();
        if (canvas)
        {
            canvas.enabled = true;
            canvas.overrideSorting = true;
            if (canvas.sortingOrder < 1000) canvas.sortingOrder = 1000;
        }
    }

    private void DebugDump()
    {
        Debug.Log($"[MenuPanelSwitcher] Active -> " +
                  $"Main:{(mainMenuRoot && mainMenuRoot.activeSelf)}  " +
                  $"Settings:{(settingsMenuRoot && settingsMenuRoot.activeSelf)}  " +
                  $"Credits:{(creditsMenuRoot && creditsMenuRoot.activeSelf)}  " +
                  $"Load:{(loadMenuRoot && loadMenuRoot.activeSelf)}");
    }

    [ContextMenu("Force Show Credits (Top & Full)")]
    public void ForceShowCreditsNow()
    {
        // Ensure we think we're on Credits
        ShowCredits();

        if (!creditsMenuRoot)
        {
            Debug.LogError("[MenuPanelSwitcher] creditsMenuRoot not assigned.");
            return;
        }

        // Full-stretch, sane transform, render last among siblings
        var rt = creditsMenuRoot.GetComponent<RectTransform>();
        if (rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.SetAsLastSibling(); // draw above siblings
        }

        // Make 100% sure it's visible and interactive
        var cg = creditsMenuRoot.GetComponent<CanvasGroup>() ?? creditsMenuRoot.AddComponent<CanvasGroup>();
        cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true;

        // Put it on a very high sorting layer temporarily
        var c = creditsMenuRoot.GetComponent<Canvas>() ?? creditsMenuRoot.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 5000;

        // Ensure it can receive clicks
        if (!creditsMenuRoot.GetComponent<GraphicRaycaster>())
            creditsMenuRoot.AddComponent<GraphicRaycaster>();

        Debug.Log("[MenuPanelSwitcher] Forced Credits visible on top (order=5000).");
    }
}
