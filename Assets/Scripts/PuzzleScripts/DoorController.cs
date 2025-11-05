using System;
using System.Collections.Generic;
using System.Reflection;                  // reflection for puzzle detection
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;        // must be at top with the other usings

public class DoorController : MonoBehaviour
{
    //What is required to open the door
    public enum doorTypes { codeDoor, itemDoor, puzzleDoor }
    public doorTypes doorType;

    public TextMeshProUGUI codeDisplay;

    public GameObject player;

    //What Items are needed to open door (set by doorTypes)
    public List<int> code;
    public bool randCode;
    public List<GameObject> item;
    public List<GameObject> puzzle;

    public bool hasOpened = false;

    //How long till door unlocks after having fufilled the requirments
    public float unlockTimer = 2.5f;

    //Interact with door to open bools
    public bool interactToOpen = false;
    public bool interacted = false;
    public float interactTimer = .1f;

    public AudioSource doorUnlocking;
    //So door unlocking sound plays once
    bool unlockOnce = false;
    public AudioSource doorOpening;

    //For closing the door
    Vector2 originalPosition;
    public AudioSource doorClosingSound;

    // ===== Opening flow that stays across multiple frames =====
    [Header("Opening Motion")]
    [Tooltip("How far the door moves along its up vector when opening.")]
    public float openDistance = 3f;
    [Tooltip("Units per second to move when opening.")]
    public float openSpeed = 2f;

    private bool isUnlocking = false;                    // keep countdown alive
    private bool isOpening = false;                      // keep movement alive
    private Vector3 openTarget;                          // target position once opened

    // cache InteractorMain and add one-time logger (gets rid of Update spam)
    private InteractorMain interactor;
    private bool _loggedOnce;
    void LogOnce(string msg)
    {
        if (_loggedOnce) return;
        _loggedOnce = true;
        Debug.LogError($"[DoorController:{name}] {msg}", this);
    }

    // ===== Code Door Modes & Randomization =====
    [Header("Code Door Settings")]
    [Tooltip("If true, door checks the last N presses must match 'code' in exact order (auto-open supported). If false, uses press-count mode.")]
    public bool useOrderedSequence = true;               // FIX: default to the ordered sequence mode

    [Tooltip("Open as soon as the code condition is satisfied (ignores interactToOpen and unlockTimer).")]
    public bool autoOpenOnSolve = true;                  // FIX: auto open on correct order

    [Tooltip("Randomize the required press counts per slot when NOT using ordered sequence.")]
    public bool randomizeCounts = false;                 // (count mode only)

    [Tooltip("If true, required counts are a permutation of 1..N (each used exactly once).")]
    public bool usePermutationCounts = true;             // (count mode only)

    [Tooltip("Minimum random count per slot when not using permutation.")]
    public int minRandomCount = 1;                       // (count mode only)

    [Tooltip("Maximum random count per slot when not using permutation. 0 = use code.Count.")]
    public int maxRandomCount = 0;                       // (count mode only)

    // Generated once and used during play (only for count mode)
    private List<int> _countPattern;                     // count-mode pattern

    // ===== Flexible puzzle detector (defaults for DrawerPullout.activeState) =====
    [Header("Puzzle Detection (set these for your puzzle objects)")]
    [Tooltip("Type name on each puzzle element (e.g. DrawerPullout, DrawerBase, PuzzleNode).")]
    public string puzzleComponentType = "DrawerPullout";
    [Tooltip("Bool member that means 'active/solved' (e.g. activeState, isOpen, solved).")]
    public string puzzleBoolMember = "activeState";

    // Reflection caches (resolved on first use)
    private Type _cachedPuzzleType;
    private MemberInfo _cachedBoolMember;

    void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(puzzleComponentType) || puzzleComponentType == "Button")
            puzzleComponentType = "DrawerPullout";
        if (string.IsNullOrWhiteSpace(puzzleBoolMember))
            puzzleBoolMember = "activeState";
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        originalPosition = transform.position;

        // Precompute open target
        openTarget = transform.position + transform.up * openDistance;

        //Set Random Code (randomize IDs)
        if (randCode && code != null && code.Count > 0)
        {
            for (int i = 0; i < code.Count; i++)
            {
                code[i] = Random.Range(1, 4);
            }
        }

        // Build press-count pattern (only if using count mode)
        if (!useOrderedSequence && doorType == doorTypes.codeDoor && code != null && code.Count > 0)   // FIX
        {
            BuildCountPattern(); // FIX
        }

        //Find player
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) LogOnce("No GameObject with tag 'Player' found.");
        }

        if (player != null)
        {
            interactor = player.GetComponent<InteractorMain>();
            if (interactor == null) LogOnce("Player found, but InteractorMain component is missing.");
        }

        //If door type code, display code to canvas
        if (doorType == doorTypes.codeDoor)
        {
            if (codeDisplay != null && code != null)
            {
                string codeToDisplay = string.Empty;

                for (int i = 0; i < code.Count; i++)
                {
                    if (i < code.Count - 1)
                    {
                        codeToDisplay += $"{code[i]}, ";
                    }
                    else
                    {
                        codeToDisplay += code[i];
                    }
                }
                codeDisplay.text = codeToDisplay;
            }
            else
            {
                Debug.LogWarning("[DoorController] codeDoor selected but codeDisplay or code list is not assigned.", this);
            }
        }

        Debug.Log($"[DoorController:{name}] Code mode={(useOrderedSequence ? "ORDERED" : "COUNT")}. Detector {puzzleComponentType}.{puzzleBoolMember}", this);
    }

    // Build/refresh the press-count pattern (count mode only)
    void BuildCountPattern()
    {
        int n = code.Count;
        _countPattern = new List<int>(n);

        if (!randomizeCounts)
        {
            // default 1x, 2x, 3x, ...
            for (int i = 0; i < n; i++) _countPattern.Add(i + 1);
            return;
        }

        if (usePermutationCounts)
        {
            // use a shuffled 1..n (each count exactly once)
            var arr = new List<int>(n);
            for (int i = 1; i <= n; i++) arr.Add(i);
            // Fisher-Yates shuffle
            for (int i = n - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = arr[i]; arr[i] = arr[j]; arr[j] = tmp;
            }
            _countPattern.AddRange(arr);
        }
        else
        {
            // independent random counts per position
            int max = (maxRandomCount > 0) ? maxRandomCount : n;
            int min = Mathf.Max(1, minRandomCount);
            max = Mathf.Max(min, max);

            for (int i = 0; i < n; i++)
            {
                _countPattern.Add(Random.Range(min, max + 1)); // inclusive range
            }
        }

        // print the pattern for debugging
        Debug.Log($"[DoorController:{name}] Count pattern = [{string.Join(",", _countPattern)}]", this);
    }

    // Update is called once per frame
    void Update()
    {
        //True count increments for each correct code/item/puzzle
        int trueCount = 0;

        //Different door types
        switch (doorType)
        {
            //If a code door
            case doorTypes.codeDoor:
                {
                    if (interactor == null || interactor.numbersCollected == null || code == null || code.Count == 0)
                        break;

                    bool solved = false;

                    if (useOrderedSequence) // ===== ORDERED SEQUENCE MODE =====   // FIX
                    {
                        solved = IsOrderedSequenceSatisfied(interactor.numbersCollected, code);
                    }
                    else // ===== COUNT MODE (second option) =====
                    {
                        // Ensure pattern exists (e.g., if code list was modified at runtime)
                        if (_countPattern == null || _countPattern.Count != code.Count)
                            BuildCountPattern();

                        // Build required totals from 'code' + our count pattern
                        var required = new Dictionary<int, int>();
                        for (int i = 0; i < code.Count; i++)
                        {
                            int id = code[i];
                            int need = _countPattern[i];
                            if (required.ContainsKey(id)) required[id] += need;
                            else required[id] = need;
                        }

                        // Count what the player actually pressed
                        var pressed = new Dictionary<int, int>();
                        var hist = interactor.numbersCollected;
                        for (int i = 0; i < hist.Count; i++)
                        {
                            int id = hist[i];
                            if (pressed.ContainsKey(id)) pressed[id]++;
                            else pressed[id] = 1;
                        }

                        // Validate all required totals are met (>= allows over-pressing)
                        solved = true;
                        foreach (var kv in required)
                        {
                            int have = pressed.TryGetValue(kv.Key, out var cnt) ? cnt : 0;
                            if (have < kv.Value) { solved = false; break; }
                        }
                    }

                    if (solved)
                    {
                        if (autoOpenOnSolve)                   // FIX: auto-open path
                        {
                            // Play unlock sound once, then open immediately
                            if (!unlockOnce && doorUnlocking != null) { doorUnlocking.Play(); unlockOnce = true; }
                            BeginOpening();
                        }
                        else
                        {
                            TryBeginUnlock();                  // previous flow (timer/E if desired)
                        }
                    }
                    break;
                }

            //If a item/key door
            case doorTypes.itemDoor:
                if (interactor == null || interactor.inventory == null || item == null) break;

                if (interactor.inventory.Count >= item.Count)
                {
                    for (int i = 0; i < item.Count; i++)
                    {
                        if (interactor.inventory.Contains(item[i]))
                        {
                            trueCount++;
                        }
                    }
                    if (trueCount >= item.Count)
                    {
                        if (autoOpenOnSolve) BeginOpening(); else TryBeginUnlock();
                    }
                }
                break;

            //If a puzzle element door
            case doorTypes.puzzleDoor:
                if (puzzle == null || puzzle.Count == 0)
                {
                    LogOnce("puzzleDoor selected but 'puzzle' list is empty or null.");
                    break;
                }

                trueCount = 0;
                foreach (var element in puzzle)
                {
                    if (element == null) continue;

                    // for DrawerPullout.activeState (no reflection needed)
                    var dp = element.GetComponent<DrawerPullout>();
                    if (dp != null)
                    {
                        if (dp.activeState) trueCount++;
                        if (trueCount == puzzle.Count) { if (autoOpenOnSolve) BeginOpening(); else TryBeginUnlock(); }
                        continue; // skip reflection if we already handled it
                    }

                    // Fallback: reflective path using inspector strings
                    bool active;
                    if (TryIsPuzzleActive(element, out active))
                    {
                        if (active) trueCount++;
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[DoorController:{name}] Puzzle element '{element.name}' missing expected {puzzleComponentType}.{puzzleBoolMember}.",
                            this
                        );
                    }

                    if (trueCount == puzzle.Count)
                    {
                        if (autoOpenOnSolve) BeginOpening(); else TryBeginUnlock();
                    }
                }
                break;
        }

        //BUG FIX
        if (interacted && !hasOpened)
        {
            interactTimer -= Time.deltaTime;
            if (interactTimer < 0)
            {
                interacted = false;
                interactTimer = .1f;
            }
        }

        // ===== Run the unlock countdown every frame (used when not autoOpenOnSolve) =====
        if (isUnlocking && !hasOpened)
        {
            if (!unlockOnce)
            {
                if (doorUnlocking != null) doorUnlocking.Play();
                unlockOnce = true;
            }

            unlockTimer -= Time.deltaTime;
            if (unlockTimer <= 0f)
            {
                isUnlocking = false;
                BeginOpening();
            }
        }

        // ===== Move door until it reaches target =====
        if (isOpening && !hasOpened)
        {
            transform.position = Vector3.MoveTowards(transform.position, openTarget, openSpeed * Time.deltaTime);

            if (Vector3.SqrMagnitude(transform.position - openTarget) < 0.0001f)
            {
                isOpening = false;
                hasOpened = true;

                if (doorOpening != null) doorOpening.Play();
                Debug.Log($"[DoorController:{name}] Opened.");
            }
        }
    }

    // === ORDERED SEQUENCE CHECK ===
    // FIX: compare the LAST N entries in 'pressed' against 'code' (must match exactly)
    bool IsOrderedSequenceSatisfied(List<int> pressed, List<int> expected)  // FIX
    {
        if (pressed == null || expected == null) return false;
        int n = expected.Count;
        if (pressed.Count < n) return false;

        // Compare suffix of length n to expected sequence
        for (int i = 0; i < n; i++)
        {
            int pressedIdx = pressed.Count - n + i;
            if (pressed[pressedIdx] != expected[i]) return false;
        }
        return true;
    }

    //Uncloked Door can be opened (automatically or by interacting)
    void UnlockDoor()
    {
        // keep for historical reference; redirect to the persistent flow
        TryBeginUnlock();
    }

    void TryBeginUnlock()
    {
        // If interactToOpen is required, make sure we got a hit at least once.
        if (interactToOpen && !interacted)
        {
            return; // player hasn’t looked & pressed E on the door yet
        }

        if (!isUnlocking && !hasOpened)
        {
            isUnlocking = true; // countdown will tick in Update()
            Debug.Log($"[DoorController:{name}] Unlocking started (timer={unlockTimer:0.00}s).");
        }
    }

    void BeginOpening()
    {
        if (hasOpened) return;
        isUnlocking = false;
        isOpening = true;

        // One-shot unlock SFX if we skipped the countdown
        if (!unlockOnce && doorUnlocking != null)
        {
            doorUnlocking.Play();
            unlockOnce = true;
        }

        Debug.Log($"[DoorController:{name}] Opening...");
    }

    void OpenDoor()
    {
        // original one-shot translate kept for reference/comments
        gameObject.transform.Translate(transform.up * 5 * Time.deltaTime);

        if (!hasOpened)
        {
            if (doorOpening != null) doorOpening.Play();
            hasOpened = true;
        }
    }

    void CloseDoor()
    {
        gameObject.transform.position = originalPosition;
        if (doorClosingSound != null && !doorClosingSound.isPlaying)
        {
            doorClosingSound.Play();
        }
    }

    // Flexible "is active" check using reflection (component + bool member)
    bool TryIsPuzzleActive(GameObject go, out bool value)
    {
        value = false;
        if (go == null) return false;
        if (string.IsNullOrWhiteSpace(puzzleComponentType) || string.IsNullOrWhiteSpace(puzzleBoolMember)) return false;

        // resolve type once
        if (_cachedPuzzleType == null || _cachedPuzzleType.Name != puzzleComponentType)
        {
            _cachedPuzzleType = null;
            _cachedBoolMember = null;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                // try unqualified first, then Namespace.Type
                var t = asm.GetType(puzzleComponentType);
                if (t == null)
                {
                    // try common pattern: AssemblyName.TypeName
                    t = asm.GetType($"{asm.GetName().Name}.{puzzleComponentType}");
                }
                if (t != null) { _cachedPuzzleType = t; break; }
            }
            if (_cachedPuzzleType == null) return false;
        }

        var comp = go.GetComponent(_cachedPuzzleType);
        if (comp == null) return false;

        // resolve member once
        if (_cachedBoolMember == null)
        {
            _cachedBoolMember =
                _cachedPuzzleType.GetField(puzzleBoolMember, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?? (MemberInfo)_cachedPuzzleType.GetProperty(puzzleBoolMember, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (_cachedBoolMember == null) return false;
        }

        try
        {
            if (_cachedBoolMember is FieldInfo fi && fi.FieldType == typeof(bool))
            {
                value = (bool)fi.GetValue(comp);
                return true;
            }
            if (_cachedBoolMember is PropertyInfo pi && pi.PropertyType == typeof(bool))
            {
                value = (bool)pi.GetValue(comp, null);
                return true;
            }
        }
        catch
        {
            // ignore and return false
        }
        return false;
    }
}
