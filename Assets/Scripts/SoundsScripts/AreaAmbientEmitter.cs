using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AreaAmbientEmitter : MonoBehaviour
{
    [Header("Distance (world units)")]
    [Tooltip("Inside this radius: full volume.")]
    public float innerRadius = 2f;

    [Tooltip("Beyond this radius: silent.")]
    public float outerRadius = 15f;

    [Header("Base volume")]
    [Range(0f, 1f)] public float maxVolume = 0.8f;

    [Header("Listener (optional)")]
    [Tooltip("If left empty, will use Camera.main.")]
    public Transform listener;

    private AudioSource src;

    private void Awake()
    {
        src = GetComponent<AudioSource>();

        // Make sure the source is set up for ambience
        src.loop = true;
        src.playOnAwake = true;
        src.spatialBlend = 0f; // we control volume manually, keep it 2D
        src.dopplerLevel = 0f;

        // Optional: start at a random time so loops don't all line up
        if (src.clip != null)
        {
            src.time = Random.Range(0f, src.clip.length);
        }
    }

    private void Start()
    {
        if (listener == null && Camera.main != null)
            listener = Camera.main.transform;
    }

    private void Update()
    {
        if (listener == null || src == null) return;

        float dist = Vector3.Distance(listener.position, transform.position);

        // 1 when inside innerRadius, 0 when beyond outerRadius
        float t = Mathf.InverseLerp(outerRadius, innerRadius, dist);
        float targetVol = maxVolume * t;

        src.volume = targetVol;
    }
}
