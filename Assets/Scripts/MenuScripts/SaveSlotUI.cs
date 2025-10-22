using UnityEngine;

public class SaveSlotUI : MonoBehaviour
{
    [Range(1, 3)] public int slot = 1;

    // Hook these to your UI Buttons (OnClick) in the Inspector.
    public void OnClick_Save() => SaveManager.Instance.SaveToSlot(slot);
    public void OnClick_Load() => SaveManager.Instance.LoadFromSlot(slot);
    public void OnClick_Delete() => SaveManager.Instance.DeleteSlot(slot);
}
