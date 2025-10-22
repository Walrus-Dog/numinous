using System.Collections.Generic;
using System.Reflection;
using System;
using UnityEngine.SceneManagement;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Tooltip("Optional: auto-load this slot on play (Editor only). Set 0 to disable.")]
    public int autoLoadSlotInEditor = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
        if (Application.isEditor && !Application.isPlaying) return;
        if (autoLoadSlotInEditor >= 1 && autoLoadSlotInEditor <= 3 && SaveSystem.Exists(autoLoadSlotInEditor))
        {
            Debug.Log($"[SaveManager] Editor auto-load slot {autoLoadSlotInEditor}");
            LoadFromSlot(autoLoadSlotInEditor);
        }
#endif
    }

    // ===== Public API =====
    public void SaveToSlot(int slot)
    {
        slot = Mathf.Clamp(slot, 1, 3);
        try
        {
            var data = Capture();
            var json = JsonUtility.ToJson(data, true);
            SaveSystem.Write(slot, json);
            Debug.Log($"[SaveManager] ? Saved slot {slot} -> {SaveSystem.GetSlotPath(slot)}. " +
                      $"scene='{data.sceneName}', entries={data.entries?.Length ?? 0}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] ? Save failed for slot {slot}: {e}");
        }
    }

    public void LoadFromSlot(int slot)
    {
        slot = Mathf.Clamp(slot, 1, 3);

        // Ensure we don't carry paused state across scenes
        GameplayStateReset.ResetToGameplay();

        var json = SaveSystem.Read(slot);
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning($"[SaveManager] ? No save found in slot {slot} at {SaveSystem.GetSlotPath(slot)}");
            return;
        }

        SaveFile data = null;
        try
        {
            data = JsonUtility.FromJson<SaveFile>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] ? JSON parse failed (slot {slot}). Delete old saves and try again. {e}");
            return;
        }

        if (data == null)
        {
            Debug.LogError("[SaveManager] ? Parsed data is null. File may be corrupt. Try deleting it.");
            return;
        }

        Debug.Log($"[SaveManager] ?? Loading slot {slot}: scene='{data.sceneName}', entries={data.entries?.Length ?? 0}");
        StartCoroutine(LoadSceneAndRestore(data));
    }

    public void DeleteSlot(int slot)
    {
        slot = Mathf.Clamp(slot, 1, 3);
        SaveSystem.Delete(slot);
        Debug.Log($"[SaveManager] Deleted slot {slot}");
    }

    public bool SlotExists(int slot) => SaveSystem.Exists(Mathf.Clamp(slot, 1, 3));

    public DateTime? GetSlotSavedTime(int slot)
    {
        var json = SaveSystem.Read(Mathf.Clamp(slot, 1, 3));
        if (string.IsNullOrEmpty(json)) return null;
        var data = JsonUtility.FromJson<SaveFile>(json);
        return DateTimeOffset.FromUnixTimeSeconds(data.savedUnixTime).DateTime;
    }

    // ===== Capture & Restore =====

    private SaveFile Capture()
    {
        var file = new SaveFile
        {
            version = "1.1",
            sceneName = SceneManager.GetActiveScene().name,
            savedUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };

        var ents = GameObject.FindObjectsByType<SaveableEntity>(FindObjectsSortMode.None);
        var list = new List<EntityRecord>();
        Debug.Log($"[SaveManager] Capturing {ents.Length} SaveableEntity objects in scene '{file.sceneName}'");

        foreach (var ent in ents)
        {
            var comps = ent.GetComponents<ISaveable>();
            if (comps == null || comps.Length == 0) continue;

            var er = new EntityRecord { id = ent.UniqueId, name = ent.name, components = new List<ComponentRecord>() };

            foreach (var s in comps)
            {
                try
                {
                    var stateObj = s.CaptureState();
                    if (stateObj == null) { Debug.Log($"[SaveManager] (skip) {ent.name}:{s.GetType().Name} returned null"); continue; }

                    string json = JsonUtility.ToJson(stateObj, false);
                    er.components.Add(new ComponentRecord
                    {
                        type = s.GetType().AssemblyQualifiedName,
                        json = json
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveManager] ? Capture error on {ent.name} ({s.GetType().Name}): {e}");
                }
            }

            if (er.components.Count > 0) list.Add(er);
        }

        file.entries = list.ToArray();
        Debug.Log($"[SaveManager] Capture complete: entries={file.entries.Length}");
        return file;
    }

    private System.Collections.IEnumerator LoadSceneAndRestore(SaveFile data)
    {
        // 1) Scene swap if needed
        var current = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(data.sceneName) && current != data.sceneName)
        {
            if (!SceneExistsInBuildSettings(data.sceneName))
            {
                Debug.LogError($"[SaveManager] ? Scene '{data.sceneName}' is not in Build Settings. " +
                               $"Add it (File -> Build Settings) or save again in a listed scene.");
                yield break;
            }

            Debug.Log($"[SaveManager] Loading scene '{data.sceneName}' (was '{current}')...");
            var op = SceneManager.LoadSceneAsync(data.sceneName, LoadSceneMode.Single);
            while (!op.isDone) yield return null;
            yield return null; // allow Awake/Start
        }
        else
        {
            Debug.Log($"[SaveManager] Scene already '{current}', no scene change.");
        }

        // 2) Fresh, unpaused state
        GameplayStateReset.ResetToGameplay();

        // 3) Build entity lookup
        var lookup = new Dictionary<string, SaveableEntity>();
        foreach (var e in GameObject.FindObjectsByType<SaveableEntity>(FindObjectsSortMode.None))
            lookup[e.UniqueId] = e;

        Debug.Log($"[SaveManager] Restoring entries={data.entries?.Length ?? 0}, scene has entities={lookup.Count}");

        int restoredEntities = 0, restoredComponents = 0, missingEntities = 0, missingComponents = 0;

        foreach (var entry in data.entries)
        {
            if (!lookup.TryGetValue(entry.id, out var ent))
            {
                missingEntities++;
                Debug.LogWarning($"[SaveManager] (missing entity) id={entry.id} nameHint='{entry.name}' components={entry.components?.Count ?? 0}");
                continue;
            }

            var saveables = ent.GetComponents<ISaveable>();
            var byType = new Dictionary<string, ISaveable>();
            foreach (var s in saveables)
                byType[s.GetType().AssemblyQualifiedName] = s;

            int thisCompRestored = 0;

            foreach (var comp in entry.components)
            {
                if (!byType.TryGetValue(comp.type, out var target))
                {
                    missingComponents++;
                    Debug.LogWarning($"[SaveManager] (missing component) on '{ent.name}' type='{comp.type}'");
                    continue;
                }

                try
                {
                    var adapterType = target.GetType();
                    var stateType = adapterType.GetNestedType("State", BindingFlags.Public | BindingFlags.NonPublic);
                    if (stateType == null)
                    {
                        Debug.LogWarning($"[SaveManager] {adapterType.Name} has no nested State type; skipping.");
                        continue;
                    }

                    var stateObj = JsonUtility.FromJson(comp.json, stateType);
                    target.RestoreState(stateObj);
                    restoredComponents++;
                    thisCompRestored++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveManager] ? Restore error on '{ent.name}' ({target.GetType().Name}): {e}");
                }
            }

            if (thisCompRestored > 0) restoredEntities++;
        }

        Debug.Log($"[SaveManager] ? Restore summary: entitiesRestored={restoredEntities}, " +
                  $"componentsRestored={restoredComponents}, missingEntities={missingEntities}, missingComponents={missingComponents}");
    }

    private bool SceneExistsInBuildSettings(string name)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var path = SceneUtility.GetScenePathByBuildIndex(i);
            var sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (string.Equals(sceneName, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // ===== File schema =====
    [Serializable]
    private class SaveFile
    {
        public string version;
        public string sceneName;
        public long savedUnixTime;
        public EntityRecord[] entries;
    }

    [Serializable]
    private class EntityRecord
    {
        public string id;
        public string name; // hint for logs
        public List<ComponentRecord> components;
    }

    [Serializable]
    private class ComponentRecord
    {
        public string type; // AssemblyQualifiedName of the ISaveable adapter
        public string json; // JSON of the adapter's nested State struct
    }
}
