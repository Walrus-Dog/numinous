using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[RequireComponent(typeof(ScrollRect))]
public class AutoScrollCredits : MonoBehaviour
{
    [Header("Wiring (auto-filled if left blank)")]
    public ScrollRect scroll;
    public RectTransform viewport;
    public RectTransform content;

    [Header("Timing (UNSCALED time)")]
    [Tooltip("Delay before the credits start moving.")]
    public float startDelay = 1f;

    [Tooltip("How long it takes to scroll from top to bottom.")]
    public float duration = 25f;

    [Tooltip("How long to hold at the end before exiting (if not looping).")]
    public float endHold = 2f;

    [Header("Behavior")]
    public bool loop = false;
    [Tooltip("Press any key to skip.")]
    public bool allowSkip = true;

    [Tooltip("Scene to load when finished (or skipped). Leave empty to stay here.")]
    public string exitSceneName = "MainMenu";

    void Awake() => AutoWire();
    void Reset() => AutoWire();

    private void AutoWire()
    {
        if (!scroll) scroll = GetComponent<ScrollRect>();
        if (scroll)
        {
            if (!viewport) viewport = scroll.viewport;
            if (!content) content = scroll.content;
        }
    }

    private IEnumerator Start()
    {
        AutoWire();

        if (!scroll || !viewport || !content)
        {
            Debug.LogWarning("[AutoScrollCredits] Missing ScrollRect/Viewport/Content wiring.");
            yield break;
        }

        // Start at top
        scroll.verticalNormalizedPosition = 1f;

        // Let layout build a couple of frames (important in builds)
        yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(startDelay);

        // Force full layout rebuild so rect heights are valid
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        float viewH = viewport.rect.height;
        float contentH = content.rect.height;

        // If content looks too short, estimate height from TMP preferred sizes
        if (contentH <= viewH + 0.5f)
        {
            float h = 0f;
            var tmps = content.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps)
            {
                t.ForceMeshUpdate();
                h += t.preferredHeight;
            }

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            if (vlg)
            {
                h += vlg.padding.top + vlg.padding.bottom;
                h += vlg.spacing * Mathf.Max(0, tmps.Length - 1);
            }

            if (h > viewH)
            {
                var size = content.sizeDelta;
                content.sizeDelta = new Vector2(size.x, h);
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                Canvas.ForceUpdateCanvases();
                contentH = content.rect.height;
            }
        }

        float maxY = Mathf.Max(0f, contentH - viewH);
        Debug.Log($"[AutoScrollCredits] viewportH={viewH:F1}, contentH={contentH:F1}, delta={maxY:F1}");

        // Always run the scroll loop, even if delta is 0 (you still get skip + exit)
        do
        {
            scroll.verticalNormalizedPosition = 1f;

            float t = 0f;
            while (t < duration)
            {
                if (allowSkip && Input.anyKeyDown)
                {
                    ExitIfNeeded();
                    yield break;
                }

                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);

                // Animate from top (1) to bottom (0)
                scroll.verticalNormalizedPosition = 1f - p;

                yield return null;
            }

            yield return new WaitForSecondsRealtime(endHold);

        } while (loop);

        ExitIfNeeded();
    }

    private void ExitIfNeeded()
    {
        if (!loop && !string.IsNullOrEmpty(exitSceneName))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(exitSceneName);
        }
    }
}
