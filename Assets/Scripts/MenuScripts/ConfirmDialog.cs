using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;  // CanvasGroup, GraphicRaycaster

public class ConfirmDialog : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private CanvasGroup group;                 // optional; auto-found if null
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private UnityEngine.UI.Button yesButton;   // fully-qualified to avoid UI Toolkit conflicts
    [SerializeField] private UnityEngine.UI.Button noButton;

    private Action _onYes;
    private Action _onNo;

    void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>();

        // Try to auto-find children if not wired
        if (!messageText) messageText = GetComponentInChildren<TMP_Text>(true);
        if (!yesButton) yesButton = transform.Find("Yes")?.GetComponent<UnityEngine.UI.Button>();
        if (!noButton) noButton = transform.Find("No")?.GetComponent<UnityEngine.UI.Button>();

        // Ensure canvas + raycaster exist (clickable)
        var canvas = GetComponentInParent<Canvas>(true);
        if (!canvas) { canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; }
        if (!canvas.GetComponent<GraphicRaycaster>()) canvas.gameObject.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();

        // Start hidden
        HideImmediate();

        if (yesButton) yesButton.onClick.AddListener(Yes);
        if (noButton) noButton.onClick.AddListener(No);
    }

    public void Show(string message, Action onYes, Action onNo = null)
    {
        _onYes = onYes;
        _onNo = onNo;

        if (messageText) messageText.text = string.IsNullOrEmpty(message) ? "Are you sure?" : message;

        // Make sure object is active so coroutines/events can run
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        SetVisible(true);
        Debug.Log("[ConfirmDialog] Show()");
    }

    public void Hide()
    {
        SetVisible(false);
        Debug.Log("[ConfirmDialog] Hide()");
    }

    private void Yes()
    {
        Hide();
        _onYes?.Invoke();
        _onYes = _onNo = null;
    }

    private void No()
    {
        Hide();
        _onNo?.Invoke();
        _onYes = _onNo = null;
    }

    private void HideImmediate()
    {
        // Keep object active so Show() always works, just gate with CanvasGroup
        if (!group)
        {
            // No CanvasGroup? fallback to de/activate
            gameObject.SetActive(false);
            return;
        }

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void SetVisible(bool visible)
    {
        if (!group)
        {
            // Fall back to SetActive if no CanvasGroup present
            gameObject.SetActive(visible);
            return;
        }

        // Ensure active when showing
        if (visible && !gameObject.activeSelf) gameObject.SetActive(true);

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null) return;
        var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
    }
}
