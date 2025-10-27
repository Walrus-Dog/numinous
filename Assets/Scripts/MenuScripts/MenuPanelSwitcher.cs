using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuPanelSwitcher : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject mainMenuRoot;      // Only the main menu UI (NOT your Music)
    [SerializeField] GameObject settingsMenuRoot;  // Root GameObject of the settings UI

    [Header("Optional first-selected (keyboard / gamepad)")]
    [SerializeField] Selectable firstOnMain;
    [SerializeField] Selectable firstOnSettings;

    [Header("Always keep these active (e.g., Music)")]
    [SerializeField] GameObject[] keepAlive;       // Drag your Music GameObject(s) here

    [Header("Auto-find fallback names (optional)")]
    [SerializeField] string mainMenuName = "MainMenu";
    [SerializeField] string settingsMenuName = "SettingsMenuScreen";

    [Header("Debug")]
    [SerializeField] bool verbose = false;

    void Awake()
    {
        // Heal timescale/cursor just in case you arrived from gameplay.
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Ensure EventSystem exists
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

        // Try to auto-wire if left empty
        if (!mainMenuRoot) mainMenuRoot = GameObject.Find(mainMenuName);
        if (!settingsMenuRoot) settingsMenuRoot = GameObject.Find(settingsMenuName);

        // Start on main by default
        ShowMain();
    }

    public void ShowSettings() => TogglePanels(true);
    public void ShowMain() => TogglePanels(false);

    private void TogglePanels(bool showSettings)
    {
        if (verbose) Debug.Log($"[MenuPanelSwitcher] Toggle -> showSettings={showSettings}");

        // Activate target, deactivate the other
        SafeSetActive(settingsMenuRoot, showSettings);
        SafeSetActive(mainMenuRoot, !showSettings);

        // Force Settings panel to be visible & clickable (in case a CanvasGroup or Raycaster is off)
        if (showSettings && settingsMenuRoot)
            ForceVisible(settingsMenuRoot);

        // Keep some objects (like Music) alive regardless of which panel is active
        if (keepAlive != null)
        {
            foreach (var go in keepAlive)
                SafeSetActive(go, true);
        }

        // Clear & set selected button
        EventSystem.current?.SetSelectedGameObject(null);
        var target = showSettings ? firstOnSettings : firstOnMain;
        if (target) target.Select();

        // Keep cursor usable in menus
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private static void SafeSetActive(GameObject go, bool state)
    {
        if (!go) return;
        if (go.activeSelf != state) go.SetActive(state);
    }

    private void ForceVisible(GameObject root)
    {
        // Ensure Canvas enabled and on top
        var canvas = root.GetComponentInParent<Canvas>(true);
        if (canvas)
        {
            canvas.enabled = true;
            canvas.overrideSorting = true;
            if (canvas.sortingOrder < 1000) canvas.sortingOrder = 1000;
        }

        // Ensure the panel itself receives input
        var cg = root.GetComponent<CanvasGroup>();
        if (cg)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        var ray = root.GetComponentInParent<GraphicRaycaster>(true);
        if (ray) ray.enabled = true;
    }
}
