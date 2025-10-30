using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// NOTE: We fully-qualify UGUI Button to avoid conflicts with UI Toolkit's Button.
public class MainMenuController : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Scene to start when choosing New Game (no saves).")]
    public string level1SceneName = "Level1";

    [Header("Buttons (UGUI)")]
    public UnityEngine.UI.Button playContinueButton;   // Play/Continue -> most recent slot
    public UnityEngine.UI.Button loadSlot1Button;
    public UnityEngine.UI.Button loadSlot2Button;
    public UnityEngine.UI.Button loadSlot3Button;
    public UnityEngine.UI.Button newGameButton;

    [Header("Delete Buttons (UGUI)")]
    public UnityEngine.UI.Button deleteSlot1Button;
    public UnityEngine.UI.Button deleteSlot2Button;
    public UnityEngine.UI.Button deleteSlot3Button;

    [Header("Optional labels next to buttons")]
    public TMP_Text playContinueLabel;                 // shows “Continue • Slot X • timestamp” or “Play • No save found”
    public TMP_Text slot1Label;
    public TMP_Text slot2Label;
    public TMP_Text slot3Label;

    [Header("Options")]
    [Tooltip("If true, disable Load buttons for empty slots.")]
    public bool disableLoadButtonsIfEmpty = true;

    [Tooltip("If true, will create a SaveManager at runtime if none exists.")]
    public bool autoBootstrapSaveManager = true;

    [Header("Confirmation")]
    [Tooltip("Assign your ConfirmDialog panel here.")]
    public ConfirmDialog confirmDialog;

    void Awake()
    {
        if (autoBootstrapSaveManager) EnsureSaveManager();
    }

    void OnEnable()
    {
        WireButtons();
        WireDeleteButtons();
        RefreshUI();
    }

    void OnDisable()
    {
        // remove listeners to avoid duplicate subscriptions on re-enable
        if (playContinueButton) playContinueButton.onClick.RemoveListener(OnClick_PlayContinue);
        if (loadSlot1Button) loadSlot1Button.onClick.RemoveListener(OnClick_Load1);
        if (loadSlot2Button) loadSlot2Button.onClick.RemoveListener(OnClick_Load2);
        if (loadSlot3Button) loadSlot3Button.onClick.RemoveListener(OnClick_Load3);
        if (newGameButton) newGameButton.onClick.RemoveListener(OnClick_NewGame);

        if (deleteSlot1Button) deleteSlot1Button.onClick.RemoveAllListeners();
        if (deleteSlot2Button) deleteSlot2Button.onClick.RemoveAllListeners();
        if (deleteSlot3Button) deleteSlot3Button.onClick.RemoveAllListeners();
    }

    private void WireButtons()
    {
        if (playContinueButton) { playContinueButton.onClick.RemoveAllListeners(); playContinueButton.onClick.AddListener(OnClick_PlayContinue); }
        if (loadSlot1Button) { loadSlot1Button.onClick.RemoveAllListeners(); loadSlot1Button.onClick.AddListener(OnClick_Load1); }
        if (loadSlot2Button) { loadSlot2Button.onClick.RemoveAllListeners(); loadSlot2Button.onClick.AddListener(OnClick_Load2); }
        if (loadSlot3Button) { loadSlot3Button.onClick.RemoveAllListeners(); loadSlot3Button.onClick.AddListener(OnClick_Load3); }
        if (newGameButton) { newGameButton.onClick.RemoveAllListeners(); newGameButton.onClick.AddListener(OnClick_NewGame); }
    }

    private void WireDeleteButtons()
    {
        if (deleteSlot1Button)
        {
            deleteSlot1Button.onClick.RemoveAllListeners();
            deleteSlot1Button.onClick.AddListener(() => ConfirmDelete(1));
        }
        if (deleteSlot2Button)
        {
            deleteSlot2Button.onClick.RemoveAllListeners();
            deleteSlot2Button.onClick.AddListener(() => ConfirmDelete(2));
        }
        if (deleteSlot3Button)
        {
            deleteSlot3Button.onClick.RemoveAllListeners();
            deleteSlot3Button.onClick.AddListener(() => ConfirmDelete(3));
        }
    }

    // === Button handlers ===
    private void OnClick_PlayContinue()
    {
        int slot = GetMostRecentSlot();
        if (slot > 0 && SaveManagerAvailable())
        {
            Debug.Log($"[MainMenu] Continue -> Loading most recent slot {slot}");
            SaveManager.Instance.LoadFromSlot(slot);
        }
        else
        {
            Debug.Log("[MainMenu] Continue -> No saves. Starting Level1 fresh.");
            StartLevel1Fresh();
        }
    }

    private void OnClick_Load1() => TryLoadSlot(1);
    private void OnClick_Load2() => TryLoadSlot(2);
    private void OnClick_Load3() => TryLoadSlot(3);

    private void OnClick_NewGame()
    {
        StartLevel1Fresh();
    }

    private void ConfirmDelete(int slot)
    {
        if (confirmDialog != null)
        {
            // Show confirmation popup
            confirmDialog.Show(
                $"Delete Save Slot {slot}? This cannot be undone.",
                onYes: () =>
                {
                    SaveManager.Instance?.DeleteSlot(slot);
                    RefreshUI();
                },
                onNo: null
            );
        }
        else
        {
            // Fallback if dialog not assigned
            Debug.LogWarning("[MainMenu] ConfirmDialog not assigned; deleting without confirmation.");
            SaveManager.Instance?.DeleteSlot(slot);
            RefreshUI();
        }
    }

    // === Helpers ===
    private void TryLoadSlot(int slot)
    {
        if (!SaveManagerAvailable())
        {
            Debug.LogWarning("[MainMenu] No SaveManager available. Creating one now.");
            if (!EnsureSaveManager()) return;
        }

        if (SaveManager.Instance.SlotExists(slot))
        {
            Debug.Log($"[MainMenu] Loading slot {slot}…");
            SaveManager.Instance.LoadFromSlot(slot);
        }
        else
        {
            Debug.Log($"[MainMenu] Slot {slot} is empty.");
        }
    }

    private void StartLevel1Fresh()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!SceneInBuild(level1SceneName))
        {
            Debug.LogError($"[MainMenu] Scene '{level1SceneName}' not in Build Settings.");
            return;
        }
        SceneManager.LoadScene(level1SceneName, LoadSceneMode.Single);
    }

    private void RefreshUI()
    {
        bool hasSM = SaveManagerAvailable();

        int mostRecent = hasSM ? GetMostRecentSlot() : 0;
        bool hasAnySave = mostRecent > 0;

        if (playContinueLabel)
        {
            if (hasAnySave && SaveManager.Instance.GetSlotSavedTime(mostRecent).HasValue)
            {
                var t = SaveManager.Instance.GetSlotSavedTime(mostRecent).Value.ToLocalTime();
                playContinueLabel.text = $"Continue • Slot {mostRecent} • {t:g}";
            }
            else
            {
                playContinueLabel.text = "Play • No save found";
            }
        }

        SetSlotUI(1, loadSlot1Button, slot1Label, hasSM);
        SetSlotUI(2, loadSlot2Button, slot2Label, hasSM);
        SetSlotUI(3, loadSlot3Button, slot3Label, hasSM);

        // Optionally disable delete buttons if the slot is empty
        if (deleteSlot1Button) deleteSlot1Button.interactable = hasSM && SaveManager.Instance.SlotExists(1);
        if (deleteSlot2Button) deleteSlot2Button.interactable = hasSM && SaveManager.Instance.SlotExists(2);
        if (deleteSlot3Button) deleteSlot3Button.interactable = hasSM && SaveManager.Instance.SlotExists(3);
    }

    private void SetSlotUI(int slot, UnityEngine.UI.Button btn, TMP_Text label, bool hasSM)
    {
        bool exists = hasSM && SaveManager.Instance.SlotExists(slot);

        if (btn) btn.interactable = !disableLoadButtonsIfEmpty || exists;

        if (label)
        {
            if (hasSM)
            {
                var t = SaveManager.Instance.GetSlotSavedTime(slot);
                label.text = exists && t.HasValue ? $"{t.Value.ToLocalTime():g}" : "Empty";
            }
            else
            {
                label.text = "Empty";
            }
        }
    }

    private int GetMostRecentSlot()
    {
        if (!SaveManagerAvailable()) return 0;

        DateTime latest = DateTime.MinValue; int slot = 0;
        var t1 = SaveManager.Instance.GetSlotSavedTime(1);
        var t2 = SaveManager.Instance.GetSlotSavedTime(2);
        var t3 = SaveManager.Instance.GetSlotSavedTime(3);

        if (t1.HasValue && t1.Value > latest) { latest = t1.Value; slot = 1; }
        if (t2.HasValue && t2.Value > latest) { latest = t2.Value; slot = 2; }
        if (t3.HasValue && t3.Value > latest) { latest = t3.Value; slot = 3; }
        return slot;
    }

    private static bool SceneInBuild(string name)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            var sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (string.Equals(sceneName, name, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static bool SaveManagerAvailable() => SaveManager.Instance != null;

    private static bool EnsureSaveManager()
    {
        if (SaveManager.Instance != null) return true;

        var existing = GameObject.FindFirstObjectByType<SaveManager>(FindObjectsInactive.Include);
        if (existing != null) return true;

        var go = GameObject.Find("SaveSystem") ?? new GameObject("SaveSystem");
        if (!go.GetComponent<SaveManager>()) go.AddComponent<SaveManager>();
        return SaveManager.Instance != null;
    }
}
