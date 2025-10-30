using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // <-- needed

public class AutoScrollCredits : MonoBehaviour
{
    [Header("Wiring (auto-filled if left blank)")]
    public ScrollRect scroll;
    public RectTransform viewport;
    public RectTransform content;

    [Header("Timing (UNSCALED time)")]
    public float startDelay = 1f;
    public float duration = 25f;
    public float endHold = 2f;

    [Header("Behavior")]
    public bool loop = false;
    public bool allowSkip = true;
    public string exitSceneName = "MainMenu";

    void Awake() { AutoWire(); }
    void Reset() { AutoWire(); }

    void AutoWire()
    {
        if (!scroll) scroll = GetComponent<ScrollRect>();
        if (scroll)
        {
            if (!viewport) viewport = scroll.viewport;
            if (!content) content = scroll.content;
        }
    }

    IEnumerator Start()
    {
        AutoWire();
        if (!scroll || !viewport || !content)
        {
            Debug.LogWarning("[AutoScrollCredits] Missing ScrollRect/Viewport/Content wiring.");
            yield break;
        }

        // start at top
        scroll.verticalNormalizedPosition = 1f;

        // let layout build, then unscaled delay
        yield return null;
        yield return new WaitForSecondsRealtime(startDelay);

        // --- Ensure content has a real height (fallback to TMP preferredHeight) ---
        float viewH = viewport.rect.height;
        float contentH = content.rect.height;

        if (contentH <= viewH + 0.5f)
        {
            // Try to compute from TMP preferred heights
            float h = 0f;
            var tmps = content.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps)
            {
                // Force TMP to update preferred sizes
                t.ForceMeshUpdate();
                h += t.preferredHeight;
            }

            // Add some padding between sections if using a VerticalLayoutGroup
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            if (vlg) h += vlg.padding.top + vlg.padding.bottom + vlg.spacing * Mathf.Max(0, tmps.Length - 1);

            // Only grow if we computed something sensible
            if (h > viewH)
            {
                var size = content.sizeDelta;
                // keep width, grow height
                content.sizeDelta = new Vector2(size.x, h);
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                contentH = content.rect.height;
            }
        }

        float maxY = Mathf.Max(0f, contentH - viewH);
        Debug.Log($"[Credits] viewportH={viewH:F1}, contentH={contentH:F1}, delta={maxY:F1}");

        if (maxY <= 1f)
        {
            // Still nothing to scroll; just hold then exit (or loop)
            yield return new WaitForSecondsRealtime(endHold);
            ExitIfNeeded();
            yield break;
        }

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
                scroll.verticalNormalizedPosition = 1f - p; // top -> bottom
                yield return null;
            }

            yield return new WaitForSecondsRealtime(endHold);

        } while (loop);

        ExitIfNeeded();
    }

    void ExitIfNeeded()
    {
        if (!loop && !string.IsNullOrEmpty(exitSceneName))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(exitSceneName);
        }
    }
}
