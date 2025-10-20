using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(1000)]
public class PauseMenuUIFixer : MonoBehaviour
{
    [Tooltip("Optional explicit PauseMenu. If empty, we'll find it.")]
    public PauseMenu pauseMenu;

    private Canvas _canvas;
    private CanvasGroup _group;

    void Awake()
    {
        _canvas = GetComponentInParent<Canvas>(true);
        _group = GetComponent<CanvasGroup>();
        EnsureEventSystem();
    }

    void OnEnable()
    {
        ForceVisibleAndInteractive();
    }

    void LateUpdate()
    {
        // While paused, keep the panel on top & interactable (handles post-load quirks).
        var pm = pauseMenu ? pauseMenu : FindFirstPauseMenu();
        if (pm != null && PauseMenu.Paused)
            ForceVisibleAndInteractive();
    }

    private PauseMenu FindFirstPauseMenu()
    {
        return Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
    }

    private void ForceVisibleAndInteractive()
    {
        if (_canvas != null)
        {
            _canvas.enabled = true;
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;     // << key
            if (_canvas.sortingOrder < 2000)    // put it above other canvases
                _canvas.sortingOrder = 2000;
        }
        if (_group != null)
        {
            _group.alpha = 1f;
            _group.interactable = true;
            _group.blocksRaycasts = true;
        }

        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
    }
}
