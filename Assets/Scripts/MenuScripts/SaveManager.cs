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
            Debug.Log($"[SaveManager] ?? Saved slot {slot} -> {SaveSystem.GetSlotPath(slot)}. " +
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

        // Ensure a clean gameplay state before loading
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
            Debug.LogError($"[SaveManager] JSON parse failed (slot {slot}). Delete old saves and try again. {e}");
            return;
        }

        if (data == null)
        {
            Debug.LogError("[SaveManager] ? Parsed data is null. File may be corrupt. Try deleting it.");
            return;
        }

        Debug.Log($"[SaveManager] ? Loading slot {slot}: scene='{data.sceneName}', entries={data.entries?.Length ?? 0}");
        StartCoroutine(LoadSceneAndRestore(data));
    }

    public void DeleteSlot(int slot)
    {
        slot = Mathf.Clamp(slot, 1, 3);
        SaveSystem.Delete(slot);
        Debug.Log($"[SaveManager] ?? Deleted slot {slot}");
    }

    public bool SlotExists(int slot) => SaveSystem.Exists(Mathf.Clamp(slot, 1, 3));

    public DateTime? GetSlotSavedTime(int slot)
    {
        var json = SaveSystem.Read(Mathf.Clamp(slot, 1, 3));
        if (string.IsNullOrEmpty(json)) return null;
        var data = JsonUtility.FromJson<SaveFile>(json);
        return DateTimeOffset.FromUnixTimeSeconds(data.savedUnixTime).DateTime;
    }

    // ===== Capture =====
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
                    if (stateObj == null)
                    {
                        Debug.Log($"[SaveManager] (skip) {ent.name}:{s.GetType().Name} returned null");
                        continue;
                    }

                    string json = JsonUtility.ToJson(stateObj, false);
                    er.components.Add(new ComponentRecord
                    {
                        type = s.GetType().AssemblyQualifiedName,
                        json = json
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveManager] Capture error on {ent.name} ({s.GetType().Name}): {e}");
                }
            }

            if (er.components.Count > 0) list.Add(er);
        }

        file.entries = list.ToArray();
        Debug.Log($"[SaveManager] Capture complete: entries={file.entries.Length}");
        return file;
    }

    // ===== LoadSceneAndRestore =====
    private System.Collections.IEnumerator LoadSceneAndRestore(SaveFile data)
    {
        // === 1. Scene swap if needed ===
        var current = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(data.sceneName) && current != data.sceneName)
        {
            if (!SceneExistsInBuildSettings(data.sceneName))
            {
                Debug.LogError($"[SaveManager] ? Scene '{data.sceneName}' is not in Build Settings.");
                yield break;
            }

            Debug.Log($"[SaveManager] Loading scene '{data.sceneName}'...");
            var op = SceneManager.LoadSceneAsync(data.sceneName, LoadSceneMode.Single);
            while (!op.isDone)
                yield return null;
            yield return null; // wait one extra frame after scene load
        }

        // Reset to a clean baseline
        GameplayStateReset.ResetToGameplay();

        // === 2. Restore entities ===
        var lookup = new Dictionary<string, SaveableEntity>();
        foreach (var e in GameObject.FindObjectsByType<SaveableEntity>(FindObjectsSortMode.None))
            lookup[e.UniqueId] = e;

        foreach (var entry in data.entries)
        {
            if (!lookup.TryGetValue(entry.id, out var ent)) continue;

            var saveables = ent.GetComponents<ISaveable>();
            foreach (var comp in entry.components)
            {
                foreach (var s in saveables)
                {
                    if (s.GetType().AssemblyQualifiedName == comp.type)
                    {
                        var stateType = s.GetType().GetNestedType("State", BindingFlags.Public | BindingFlags.NonPublic);
                        if (stateType == null) continue;
                        var stateObj = JsonUtility.FromJson(comp.json, stateType);
                        s.RestoreState(stateObj);
                    }
                }
            }
        }

        Debug.Log("[SaveManager] ? Entities restored. Waiting for Player...");

        // === 3. Wait for Player to exist ===
        GameObject player = null;
        float timeout = 5f;
        while (player == null && timeout > 0f)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        // === 4. Resume normal gameplay immediately ===
        Time.timeScale = 1f;
        PauseMenu.Paused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (player != null)
        {
#if ENABLE_INPUT_SYSTEM
            var pi = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (pi != null)
            {
                pi.enabled = true;
                var map = pi.actions?.FindActionMap("Player", false);
                if (map != null && pi.currentActionMap != map)
                    pi.SwitchCurrentActionMap("Player");
            }
#endif

            foreach (var mb in player.GetComponents<MonoBehaviour>())
            {
                if (mb != null && !mb.enabled)
                    mb.enabled = true;
            }

            Debug.Log("[SaveManager] ?? Player active — gameplay resumed normally.");
        }

        // Hide pause menu if it's still active
        var pm = UnityEngine.Object.FindFirstObjectByType<PauseMenu>(FindObjectsInactive.Include);
        if (pm != null && pm.PauseMenuScreen != null)
            pm.PauseMenuScreen.SetActive(false);

        // === 5. Fix: If we loaded the Main Menu, ensure cursor is visible ===
        if (data.sceneName.Equals("MainMenu", StringComparison.OrdinalIgnoreCase))
        {
            Time.timeScale = 1f;
            PauseMenu.Paused = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("[SaveManager] ?? Cursor re-enabled for Main Menu scene.");
        }

#if UNITY_EDITOR
        Debug.Log("[SaveManager] ? Game loaded UNPAUSED — cursor locked, ready to play.");
#endif
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
        public string name;
        public List<ComponentRecord> components;
    }

    [Serializable]
    private class ComponentRecord
    {
        public string type;
        public string json;
    }
}
