using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

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

        // let layout build a couple frames (build can be slower)
        yield return null;
        yield return null;
        yield return new WaitForSecondsRealtime(startDelay);

        // force full layout rebuild so rect heights are valid in build
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        float viewH = viewport.rect.height;
        float contentH = content.rect.height;

        // if content is still not taller than viewport, try TMP preferred heights
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
                h += vlg.padding.top + vlg.padding.bottom + vlg.spacing * Mathf.Max(0, tmps.Length - 1);

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
        Debug.Log($"[Credits] viewportH={viewH:F1}, contentH={contentH:F1}, delta={maxY:F1}");

        // IMPORTANT: do NOT early-exit here, always run the scroll loop

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

                // animate top -> bottom
                scroll.verticalNormalizedPosition = 1f - p;

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
