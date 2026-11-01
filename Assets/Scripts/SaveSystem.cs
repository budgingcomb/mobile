using System;
using System.IO;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int[] memoryStates = new int[7];   // 4 = воспоминание возвращено
    public bool[] holoSeen = new bool[7];     // голограмму уже смотрели (можно пропускать)
    public float forgetting = 0.2f;
}

// Сохранение в JSON в persistentDataPath (раздел 8.7 ГДД)
public static class SaveSystem
{
    public static SaveData Data = new SaveData();
    static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");
    public static bool HasSave => File.Exists(FilePath);

    public static void Load()
    {
        Data = new SaveData();
        if (!File.Exists(FilePath)) return;
        try
        {
            var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            if (d != null && d.memoryStates != null && d.memoryStates.Length == 7 && d.holoSeen != null && d.holoSeen.Length == 7)
                Data = d;
        }
        catch (Exception e) { Debug.LogWarning("Save load failed: " + e.Message); }
    }

    public static void Save()
    {
        try { File.WriteAllText(FilePath, JsonUtility.ToJson(Data)); }
        catch (Exception e) { Debug.LogWarning("Save failed: " + e.Message); }
    }

    public static void Delete()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
        Data = new SaveData();
    }
}
