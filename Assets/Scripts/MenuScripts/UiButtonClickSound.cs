using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UnityEngine.UI.Button))]
public class UIButtonClickSound : MonoBehaviour, IPointerDownHandler, ISubmitHandler
{
    public AudioClip click;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 2f)] public float pitch = 1f;

    public void OnPointerDown(PointerEventData eventData)
    {
        PlayClick();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        // For keyboard / controller "submit"
        PlayClick();
    }

    private void PlayClick()
    {
        if (!click)
        {
            Debug.LogWarning($"[UIButtonClickSound] '{name}' has no click clip.");
            return;
        }

        // Ensure a bus exists even if you forgot to place one
        if (UIAudioBus.Instance == null)
        {
            var go = GameObject.Find("UIAudioBus") ?? new GameObject("UIAudioBus");
            go.AddComponent<UIAudioBus>();
        }

        UIAudioBus.Instance.PlayOneShot(click, volume, pitch);
    }
}
