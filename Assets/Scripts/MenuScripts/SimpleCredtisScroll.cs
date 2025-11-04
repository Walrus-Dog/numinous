using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleCreditsScroll : MonoBehaviour
{
    [Header("Scroll")]
    [Tooltip("Seconds to wait before starting the scroll.")]
    public float startDelay = 1f;

    [Tooltip("Pixels per second the credits move upward.")]
    public float scrollSpeed = 50f;

    [Tooltip("Extra distance to travel past the top before exiting.")]
    public float extraDistance = 200f;

    [Header("Exit")]
    [Tooltip("Hold at the end before exiting (seconds).")]
    public float endHold = 2f;

    [Tooltip("Allow skipping with any key / mouse button.")]
    public bool allowSkip = true;

    [Tooltip("Scene to load when credits finish or are skipped.")]
    public string exitSceneName = "MainMenu";

    private RectTransform _rect;
    private float _startY;
    private bool _done;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    private IEnumerator Start()
    {
        if (_rect == null)
        {
            Debug.LogWarning("[SimpleCreditsScroll] No RectTransform, aborting.");
            yield break;
        }

        // Make sure time is running
        Time.timeScale = 1f;

        _startY = _rect.anchoredPosition.y;

        // Give layout a moment to build, then wait the user-facing delay
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return new WaitForSecondsRealtime(startDelay);

        float travelled = 0f;
        float totalDistance = _rect.rect.height + extraDistance;

        while (travelled < totalDistance)
        {
            if (allowSkip && Input.anyKeyDown)
            {
                Exit();
                yield break;
            }

            float delta = scrollSpeed * Time.unscaledDeltaTime;
            travelled += delta;

            _rect.anchoredPosition = new Vector2(
                _rect.anchoredPosition.x,
                _startY + travelled
            );

            yield return null;
        }

        // Small hold at the end
        float t = 0f;
        while (t < endHold)
        {
            if (allowSkip && Input.anyKeyDown)
            {
                Exit();
                yield break;
            }

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Exit();
    }

    private void Exit()
    {
        if (_done) return;
        _done = true;

        if (!string.IsNullOrEmpty(exitSceneName))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(exitSceneName);
        }
    }
}
