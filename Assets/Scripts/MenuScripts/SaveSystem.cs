using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private const string FilePrefix = "slot_";
    private const string FileExt = ".json";

    public static string GetSlotPath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, $"{FilePrefix}{slot}{FileExt}");
    }

    public static void Write(int slot, string json)
    {
        var path = GetSlotPath(slot);
        var dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, json);
    }

    public static string Read(int slot)
    {
        var path = GetSlotPath(slot);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    public static bool Exists(int slot) => File.Exists(GetSlotPath(slot));

    public static void Delete(int slot)
    {
        var path = GetSlotPath(slot);
        if (File.Exists(path)) File.Delete(path);
    }
}
