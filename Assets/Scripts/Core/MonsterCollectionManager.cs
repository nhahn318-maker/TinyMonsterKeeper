using System;
using System.Collections.Generic;
using UnityEngine;

public static class MonsterCollectionManager
{
    private const string SavePrefix = "TinyMonsterKeeper.MonsterCollection.";
    private static readonly string[] DefaultUnlockedMonsterIds = { "001" };

    public static event Action<MonsterData> MonsterUnlocked;
    public static event Action<MonsterData, int> MonsterCollectionChanged;

    public static bool IsUnlocked(MonsterData monsterData)
    {
        return GetUnlockCount(monsterData) > 0;
    }

    public static int GetUnlockCount(MonsterData monsterData)
    {
        string id = GetMonsterId(monsterData);
        if (string.IsNullOrEmpty(id))
            return 0;

        int savedCount = Mathf.Max(0, PlayerPrefs.GetInt(SavePrefix + id, 0));
        return IsDefaultUnlockedMonsterId(id) ? Mathf.Max(1, savedCount) : savedCount;
    }

    public static bool Unlock(MonsterData monsterData)
    {
        string id = GetMonsterId(monsterData);
        if (string.IsNullOrEmpty(id))
            return false;

        int count = GetUnlockCount(monsterData) + 1;
        PlayerPrefs.SetInt(SavePrefix + id, count);
        PlayerPrefs.Save();

        MonsterUnlocked?.Invoke(monsterData);
        MonsterCollectionChanged?.Invoke(monsterData, count);
        return true;
    }

    public static void ApplySavedCounts(MonsterData[] monsterDatabase, List<MonsterCollectionSave> savedCounts)
    {
        if (savedCounts != null)
        {
            for (int i = 0; i < savedCounts.Count; i++)
            {
                MonsterCollectionSave savedCount = savedCounts[i];
                if (savedCount == null || string.IsNullOrWhiteSpace(savedCount.monsterId))
                    continue;

                PlayerPrefs.SetInt(SavePrefix + savedCount.monsterId, Mathf.Max(0, savedCount.count));
            }
        }

        if (monsterDatabase != null)
        {
            for (int i = 0; i < monsterDatabase.Length; i++)
            {
                string id = GetMonsterId(monsterDatabase[i]);
                if (IsDefaultUnlockedMonsterId(id) && PlayerPrefs.GetInt(SavePrefix + id, 0) <= 0)
                    PlayerPrefs.SetInt(SavePrefix + id, 1);
            }
        }

        PlayerPrefs.Save();
    }

    public static List<MonsterCollectionSave> ExportCounts(MonsterData[] monsterDatabase)
    {
        List<MonsterCollectionSave> result = new List<MonsterCollectionSave>();
        if (monsterDatabase == null)
            return result;

        for (int i = 0; i < monsterDatabase.Length; i++)
        {
            MonsterData monsterData = monsterDatabase[i];
            string id = GetMonsterId(monsterData);
            if (string.IsNullOrEmpty(id))
                continue;

            int count = GetUnlockCount(monsterData);
            if (count > 0)
                result.Add(new MonsterCollectionSave(id, count));
        }

        return result;
    }

    public static string GetMonsterId(MonsterData monsterData)
    {
        if (monsterData == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(monsterData.id))
            return monsterData.id.Trim();

        return monsterData.name;
    }

    private static bool IsDefaultUnlockedMonsterId(string id)
    {
        for (int i = 0; i < DefaultUnlockedMonsterIds.Length; i++)
        {
            if (DefaultUnlockedMonsterIds[i] == id)
                return true;
        }

        return false;
    }
}
