using UnityEngine;
using UnityEngine.Audio;

public class UIAudioBus : MonoBehaviour
{
    public static UIAudioBus Instance { get; private set; }

    [Header("Optional routing")]
    [SerializeField] private AudioMixerGroup sfxGroup; // drag your SFX group here if you use one

    [Header("Optional test")]
    [SerializeField] private AudioClip testClip;
    [SerializeField] private bool playTestOnStart = false;

    private AudioSource _source;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        EnsureListener();

        _source = gameObject.GetComponent<AudioSource>();
        if (!_source) _source = gameObject.AddComponent<AudioSource>();

        _source.playOnAwake = false;
        _source.loop = false;
        _source.spatialBlend = 0f;          // 2D
        _source.ignoreListenerPause = true; // still plays when paused
        if (sfxGroup) _source.outputAudioMixerGroup = sfxGroup;

        DontDestroyOnLoad(gameObject);

        if (playTestOnStart && testClip)
        {
            Debug.Log("[UIAudioBus] Test play on start");
            _source.PlayOneShot(testClip, 1f);
        }
    }

    public void PlayOneShot(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (!clip) { Debug.LogWarning("[UIAudioBus] No clip assigned."); return; }
        if (!_source) { Debug.LogWarning("[UIAudioBus] No AudioSource."); return; }

        var oldPitch = _source.pitch;
        _source.pitch = pitch;
        _source.PlayOneShot(clip, Mathf.Clamp01(volume));
        _source.pitch = oldPitch;
    }

    private static void EnsureListener()
    {
        // If there’s no active AudioListener, add one to the main camera (or a new GO)
        var listener = Object.FindFirstObjectByType<AudioListener>(FindObjectsInactive.Exclude);
        if (listener) return;

        var cam = Camera.main;
        if (cam) { cam.gameObject.AddComponent<AudioListener>(); return; }

        var go = new GameObject("TempAudioListener");
        go.AddComponent<AudioListener>();
    }
}
