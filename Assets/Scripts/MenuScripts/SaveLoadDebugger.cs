using System.Linq;
using UnityEngine;

public class SaveLoadDebugger : MonoBehaviour
{
    [Tooltip("Slot used by F5 (save) / F9 (load) / F8 (delete). Default = 4")]
    public int slot = 4;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log("[SLD] ?? F5 Save requested");
            SaveManager.Instance.SaveToSlot(slot);
            Notify("Saved Successfully");
            DumpSnapshot("[SLD] After SAVE");
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            Debug.Log("[SLD] ?? F9 Load requested");
            SaveManager.Instance.LoadFromSlot(slot);
            Notify("Loaded Successfully");
        }

        if (Input.GetKeyDown(KeyCode.F8))
        {
            Debug.Log("[SLD] ?? F8 Delete slot requested");
            SaveManager.Instance.DeleteSlot(slot);
            Notify("Deleted Save Slot");
        }
    }

    [ContextMenu("Dump Snapshot Now")]
    public void DumpSnapshotNow() => DumpSnapshot("[SLD] Snapshot");

    private void DumpSnapshot(string tag)
    {
        var ents = GameObject.FindObjectsByType<SaveableEntity>(FindObjectsSortMode.None);
        var count = ents.Length;
        var withSaveables = ents.Count(e => e.GetComponents<ISaveable>()?.Length > 0);
        Debug.Log($"{tag}: scene='{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}', " +
                  $"entities={count}, entitiesWithAdapters={withSaveables}");
        foreach (var e in ents)
        {
            var saveables = e.GetComponents<ISaveable>();
            Debug.Log($"[SLD] Entity '{e.name}' id={e.UniqueId} adapters={saveables.Length}");
            foreach (var s in saveables)
                Debug.Log($"       - {s.GetType().Name}");
        }
        Debug.Log($"[SLD] persistentDataPath: {Application.persistentDataPath}");
    }

    private void Notify(string msg)
    {
        var notifier = GameObject.FindFirstObjectByType<SaveNotificationUI>(FindObjectsInactive.Include);
        if (notifier != null)
            notifier.Show(msg);
    }
}
