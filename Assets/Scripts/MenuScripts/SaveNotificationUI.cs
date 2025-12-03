using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SaveNotificationUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup popupGroup;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float displayDuration = 1.5f;

    private Coroutine activeRoutine;

    void Awake()
    {
        if (popupGroup)
        {
            popupGroup.alpha = 0f;
        }

        // Make sure we start active so we can run coroutines
        gameObject.SetActive(true);
    }

    public void Show(string message)
    {
        // Ensure the object is active (prevents Coroutine crash)
        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        if (popupGroup == null || messageText == null)
        {
            Debug.LogWarning("[SaveNotificationUI] Missing UI references.");
            return;
        }

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(ShowRoutine(message));
    }

    private IEnumerator ShowRoutine(string message)
    {
        popupGroup.gameObject.SetActive(true);
        messageText.text = message;

        // Fade in
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            popupGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }

        popupGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(displayDuration);

        // Fade out
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            popupGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        popupGroup.alpha = 0f;
        popupGroup.gameObject.SetActive(false);
        activeRoutine = null;
    }
}
