using UnityEngine;

public class SaveSlotUI : MonoBehaviour
{
    // Support up to 4 if you're using quicksave slot 4; set per-button in Inspector.
    [Range(1, 4)] public int slot = 1;

    [Header("Confirmation")]
    [Tooltip("Show a confirmation popup before deleting this slot.")]
    public bool requireConfirmation = true;

    [Tooltip("Drag your ConfirmDialog here. If left empty, we'll try to find one in the scene.")]
    public ConfirmDialog confirmDialog;

    [Tooltip("Custom confirmation message (optional). Leave empty to use the default.")]
    [TextArea]
    public string customConfirmMessage;

    void Awake()
    {
        if (!confirmDialog)
        {
            // Unity 6 API: include inactive so we can find a dialog that's hidden via CanvasGroup.
            confirmDialog = GameObject.FindFirstObjectByType<ConfirmDialog>(FindObjectsInactive.Include);
        }
    }

    // Hook these to your UI Buttons (OnClick) in the Inspector.
    public void OnClick_Save() => SaveManager.Instance.SaveToSlot(slot);
    public void OnClick_Load() => SaveManager.Instance.LoadFromSlot(slot);

    public void OnClick_Delete()
    {
        // If we don't want confirmation or there's no dialog, delete immediately (previous behavior).
        if (!requireConfirmation || confirmDialog == null)
        {
            SaveManager.Instance.DeleteSlot(slot);
            return;
        }

        // Build message
        string msg = string.IsNullOrWhiteSpace(customConfirmMessage)
            ? $"Delete Save {slot}? "
            : customConfirmMessage;

        // Show dialog; only delete if the user confirms.
        confirmDialog.Show(
            msg,
            onYes: () =>
            {
                SaveManager.Instance.DeleteSlot(slot);
            },
            onNo: null
        );
    }
}
