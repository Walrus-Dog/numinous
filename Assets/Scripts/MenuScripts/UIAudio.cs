using UnityEngine;

public class UIAudio : MonoBehaviour
{
    public static UIAudio Instance { get; private set; }

    [Tooltip("AudioSource used to play UI sounds.")]
    public AudioSource source;

    [Tooltip("Fallback click sound if no clip is passed in.")]
    public AudioClip defaultClick;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // keeps it across scenes

        if (!source) source = GetComponent<AudioSource>();
    }

    public void PlayClick(AudioClip clip = null)
    {
        if (!source) return;

        var chosen = clip ? clip : defaultClick;
        if (chosen == null) return;

        source.PlayOneShot(chosen);
    }
}
