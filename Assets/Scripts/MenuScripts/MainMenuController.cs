using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// NOTE: We fully-qualify UGUI Button to avoid conflicts with UI Toolkit's Button.
public class MainMenuController : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Scene to start when choosing New Game.")]
    public string level1SceneName = "Level1";

    [Header("Buttons (UGUI)")]
    public UnityEngine.UI.Button continueButton;
    public UnityEngine.UI.Button loadSlot1Button;
    public UnityEngine.UI.Button loadSlot2Button;
    public UnityEngine.UI.Button loadSlot3Button;
    public UnityEngine.UI.Button newGameButton; // optional

    [Header("Optional labels next to buttons")]
    public TMP_Text continueLabel;
    public TMP_Text slot1Label;
    public TMP_Text slot2Label;
    public TMP_Text slot3Label;

    [Header("Options")]
    [Tooltip("If true, New Game clears all slots before loading Level1.")]
    public bool wipeSavesOnNewGame = false;

    [Tooltip("If true, disable Load buttons for empty slots. If false, loads remain clickable and log 'empty'.")]
    public bool disableLoadButtonsIfEmpty = false;

    [Tooltip("If true, will create a SaveManager at runtime if none exists.")]
    public bool autoBootstrapSaveManager = true;

    private void Awake()
    {
        if (autoBootstrapSaveManager) EnsureSaveManager();
    }

    private void OnEnable()
    {
        RefreshUI();
        WireButtons();
    }

    private void OnDisable()
    {
        if (continueButton) continueButton.onClick.RemoveListener(OnClick_Continue);
        if (loadSlot1Button) loadSlot1Button.onClick.RemoveListener(OnClick_Load1);
        if (loadSlot2Button) loadSlot2Button.onClick.RemoveListener(OnClick_Load2);
        if (loadSlot3Button) loadSlot3Button.onClick.RemoveListener(OnClick_Load3);
        if (newGameButton) newGameButton.onClick.RemoveListener(OnClick_NewGame);
    }

    private void WireButtons()
    {
        if (continueButton) { continueButton.onClick.RemoveAllListeners(); continueButton.onClick.AddListener(OnClick_Continue); }
        if (loadSlot1Button) { loadSlot1Button.onClick.RemoveAllListeners(); loadSlot1Button.onClick.AddListener(OnClick_Load1); }
        if (loadSlot2Button) { loadSlot2Button.onClick.RemoveAllListeners(); loadSlot2Button.onClick.AddListener(OnClick_Load2); }
        if (loadSlot3Button) { loadSlot3Button.onClick.RemoveAllListeners(); loadSlot3Button.onClick.AddListener(OnClick_Load3); }
        if (newGameButton) { newGameButton.onClick.RemoveAllListeners(); newGameButton.onClick.AddListener(OnClick_NewGame); }
    }

    // === Button handlers ===
    public void OnClick_Continue()
    {
        int slot = GetMostRecentSlot();
        if (slot > 0 && SaveManagerAvailable()) SaveManager.Instance.LoadFromSlot(slot);
        else StartLevel1Fresh();
    }

    public void OnClick_Load1() => TryLoadSlot(1);
    public void OnClick_Load2() => TryLoadSlot(2);
    public void OnClick_Load3() => TryLoadSlot(3);

    public void OnClick_NewGame()
    {
        if (wipeSavesOnNewGame && SaveManagerAvailable())
        {
            SaveSystem.Delete(1);
            SaveSystem.Delete(2);
            SaveSystem.Delete(3);
        }
        StartLevel1Fresh();
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
            Debug.Log($"[MainMenu] Loading slot {slot}...");
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

        if (continueButton) continueButton.interactable = hasAnySave;
        if (continueLabel)
        {
            if (hasAnySave && SaveManager.Instance.GetSlotSavedTime(mostRecent).HasValue)
            {
                var t = SaveManager.Instance.GetSlotSavedTime(mostRecent).Value.ToLocalTime();
                continueLabel.text = $"Slot {mostRecent} • {t:g}";
            }
            else continueLabel.text = "No save found";
        }

        SetSlotUI(1, loadSlot1Button, slot1Label, hasSM);
        SetSlotUI(2, loadSlot2Button, slot2Label, hasSM);
        SetSlotUI(3, loadSlot3Button, slot3Label, hasSM);
    }

    private void SetSlotUI(int slot, UnityEngine.UI.Button btn, TMP_Text label, bool hasSM)
    {
        bool exists = hasSM && SaveManager.Instance.SlotExists(slot);

        // Keep clickable unless you explicitly want to disable when empty
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

        // Try new Unity 6 API (includes inactive objects)
        var existing = GameObject.FindFirstObjectByType<SaveManager>(FindObjectsInactive.Include);
        if (existing != null) return true;

        // Create one if missing
        var go = GameObject.Find("SaveSystem") ?? new GameObject("SaveSystem");
        if (go.GetComponent<SaveManager>() == null) go.AddComponent<SaveManager>();
        return SaveManager.Instance != null;
    }
}
