using System;
using UnityEngine;

public static class MonsterCollectionManager
{
    private const string SavePrefix = "TinyMonsterKeeper.MonsterCollection.";

    public static event Action<MonsterData> MonsterUnlocked;

    public static bool IsUnlocked(MonsterData monsterData)
    {
        string id = GetMonsterId(monsterData);
        return !string.IsNullOrEmpty(id) && PlayerPrefs.GetInt(SavePrefix + id, 0) == 1;
    }

    public static bool Unlock(MonsterData monsterData)
    {
        string id = GetMonsterId(monsterData);
        if (string.IsNullOrEmpty(id) || IsUnlocked(monsterData))
            return false;

        PlayerPrefs.SetInt(SavePrefix + id, 1);
        PlayerPrefs.Save();

        MonsterUnlocked?.Invoke(monsterData);
        return true;
    }

    private static string GetMonsterId(MonsterData monsterData)
    {
        if (monsterData == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(monsterData.id))
            return monsterData.id.Trim();

        return monsterData.name;
    }
}
