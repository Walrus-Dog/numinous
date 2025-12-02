using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class VendingMachineSFX : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip vendingClip;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Interaction")]
    public KeyCode useKey = KeyCode.E;
    public float interactDistance = 3f;

    [Tooltip("Leave empty to auto-find object tagged 'Player'.")]
    public Transform player;

    private AudioSource _audio;

    // debug flags so we don't spam the console
    private bool _warnedNoPlayer;
    private bool _warnedNoClip;

    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null)
            _audio = gameObject.AddComponent<AudioSource>();

        _audio.playOnAwake = false;
        _audio.loop = false;
        _audio.spatialBlend = 1f;
        _audio.rolloffMode = AudioRolloffMode.Logarithmic;

        Debug.Log($"[VendingMachineSFX] Awake on {name}");
    }

    private void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                Debug.Log($"[VendingMachineSFX] Auto-found player: {p.name}");
            }
            else
            {
                Debug.LogWarning("[VendingMachineSFX] No player assigned and no object tagged 'Player' found.");
            }
        }

        if (vendingClip == null)
        {
            Debug.LogWarning($"[VendingMachineSFX] No vendingClip assigned on {name}.");
        }
    }

    private void Update()
    {
        // 1) Make sure we even can interact
        if (player == null)
        {
            if (!_warnedNoPlayer)
            {
                Debug.LogWarning("[VendingMachineSFX] Update running but 'player' is NULL. " +
                                 "Drag your Player into the 'player' field OR tag your player as 'Player'.");
                _warnedNoPlayer = true;
            }
            return;
        }

        if (vendingClip == null)
        {
            if (!_warnedNoClip)
            {
                Debug.LogWarning($"[VendingMachineSFX] Update running but vendingClip is NULL on {name}. " +
                                 "Assign an AudioClip in the inspector.");
                _warnedNoClip = true;
            }
            return;
        }

        // 2) Only react when E is pressed this frame
        if (Input.GetKeyDown(useKey))
        {
            float dist = Vector3.Distance(player.position, transform.position);
            Debug.Log($"[VendingMachineSFX] E pressed, distance to machine = {dist:F2}");

            if (dist <= interactDistance)
            {
                _audio.PlayOneShot(vendingClip, volume);
                Debug.Log("[VendingMachineSFX] Played vending sound!");
            }
            else
            {
                Debug.Log($"[VendingMachineSFX] Too far to interact (need <= {interactDistance}, currently {dist:F2}).");
            }
        }
    }
}
