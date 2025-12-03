#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class OpenSavesFolder
{
    [MenuItem("Tools/Saves/Open Save Folder")]
    public static void OpenFolder()
    {
        var path = Application.persistentDataPath;
        Debug.Log($"Save folder: {path}");
        EditorUtility.RevealInFinder(path);
    }

    [MenuItem("Tools/Saves/Clear All Save Slots")]
    public static void ClearAll()
    {
        // matches our system
        string p = Application.persistentDataPath;
        string[] files = { "slot_1.json", "slot_2.json", "slot_3.json" };
        int count = 0;
        foreach (var f in files)
        {
            var fp = Path.Combine(p, f);
            if (File.Exists(fp)) { File.Delete(fp); count++; }
        }
        Debug.Log($"Deleted {count} save file(s) from {p}");
    }
}
#endif
