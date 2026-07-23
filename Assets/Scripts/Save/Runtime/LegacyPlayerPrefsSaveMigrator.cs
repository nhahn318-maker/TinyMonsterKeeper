using System.Collections.Generic;
using UnityEngine;

public static class LegacyPlayerPrefsSaveMigrator
{
    private const string MigrationMarkerPrefix = "TinyMonsterKeeper.Save.LegacyPlayerPrefsMigrated.";

    private static readonly string[] LegacyCoinKeys =
    {
        "TinyMonsterKeeper.Coin",
        "TinyMonsterKeeper.Currency.Coin",
        "TinyMonsterKeeper.Player.Coin",
        "Coin",
        "coin"
    };

    private static readonly string[] LegacyInventoryPrefixes =
    {
        "TinyMonsterKeeper.Inventory.",
        "TinyMonsterKeeper.Item.",
        "Inventory."
    };

    public static bool TryMigrateInto(GameSaveData targetSave, ItemData[] itemDatabase, MonsterData[] monsterDatabase, string userId)
    {
        if (targetSave == null)
            return false;

        string markerKey = GetMarkerKey(userId);
        if (PlayerPrefs.GetInt(markerKey, 0) == 1)
            return false;

        bool migrated = false;
        migrated |= MigrateCoin(targetSave);
        migrated |= MigrateInventory(targetSave, itemDatabase);
        migrated |= MigrateMonsterCollection(targetSave, monsterDatabase);
        migrated |= MigrateGardenMonsters(targetSave, monsterDatabase);

        PlayerPrefs.SetInt(markerKey, 1);
        PlayerPrefs.Save();

        if (migrated)
            Debug.Log("Legacy PlayerPrefs save migrated into current save.");

        return migrated;
    }

    private static bool MigrateCoin(GameSaveData targetSave)
    {
        int bestCoin = targetSave.coin;
        bool foundLegacyCoin = false;

        for (int i = 0; i < LegacyCoinKeys.Length; i++)
        {
            string key = LegacyCoinKeys[i];
            if (!PlayerPrefs.HasKey(key))
                continue;

            bestCoin = Mathf.Max(bestCoin, PlayerPrefs.GetInt(key, 0));
            foundLegacyCoin = true;
        }

        if (!foundLegacyCoin || bestCoin == targetSave.coin)
            return false;

        targetSave.coin = bestCoin;
        return true;
    }

    private static bool MigrateInventory(GameSaveData targetSave, ItemData[] itemDatabase)
    {
        if (itemDatabase == null)
            return false;

        Dictionary<string, int> mergedItems = ToItemMap(targetSave.inventory);
        bool changed = false;

        for (int i = 0; i < itemDatabase.Length; i++)
        {
            ItemData itemData = itemDatabase[i];
            if (itemData == null || string.IsNullOrWhiteSpace(itemData.itemId))
                continue;

            for (int prefixIndex = 0; prefixIndex < LegacyInventoryPrefixes.Length; prefixIndex++)
            {
                string key = LegacyInventoryPrefixes[prefixIndex] + itemData.itemId;
                if (!PlayerPrefs.HasKey(key))
                    continue;

                int legacyAmount = Mathf.Max(0, PlayerPrefs.GetInt(key, 0));
                if (!mergedItems.TryGetValue(itemData.itemId, out int currentAmount) || legacyAmount > currentAmount)
                {
                    mergedItems[itemData.itemId] = legacyAmount;
                    changed = true;
                }
            }
        }

        if (!changed)
            return false;

        targetSave.inventory = ToItemList(mergedItems);
        return true;
    }

    private static bool MigrateMonsterCollection(GameSaveData targetSave, MonsterData[] monsterDatabase)
    {
        List<MonsterCollectionSave> legacyCounts = MonsterCollectionManager.ExportCounts(monsterDatabase);
        if (legacyCounts == null || legacyCounts.Count == 0)
            return false;

        Dictionary<string, int> mergedCounts = ToMonsterCountMap(targetSave.monsterCollection);
        bool changed = false;

        for (int i = 0; i < legacyCounts.Count; i++)
        {
            MonsterCollectionSave legacyCount = legacyCounts[i];
            if (legacyCount == null || string.IsNullOrWhiteSpace(legacyCount.monsterId))
                continue;

            int legacyAmount = Mathf.Max(0, legacyCount.count);
            if (!mergedCounts.TryGetValue(legacyCount.monsterId, out int currentAmount) || legacyAmount > currentAmount)
            {
                mergedCounts[legacyCount.monsterId] = legacyAmount;
                changed = true;
            }
        }

        if (!changed)
            return false;

        targetSave.monsterCollection = ToMonsterCountList(mergedCounts);
        return true;
    }

    private static bool MigrateGardenMonsters(GameSaveData targetSave, MonsterData[] monsterDatabase)
    {
        List<string> legacyMonsterIds = GardenMonsterSaveManager.ExportLegacySavedMonsterIds(monsterDatabase);
        if (legacyMonsterIds == null || legacyMonsterIds.Count == 0)
            return false;

        if (targetSave.gardenMonsters == null)
            targetSave.gardenMonsters = new List<string>();

        if (targetSave.gardenMonsterInstances == null)
            targetSave.gardenMonsterInstances = new List<GardenMonsterInstanceSave>();

        HashSet<string> existingIds = new HashSet<string>(targetSave.gardenMonsters);
        HashSet<string> existingInstanceIds = new HashSet<string>();
        for (int i = 0; i < targetSave.gardenMonsterInstances.Count; i++)
        {
            GardenMonsterInstanceSave instance = targetSave.gardenMonsterInstances[i];
            if (instance != null && !string.IsNullOrWhiteSpace(instance.monsterId))
                existingInstanceIds.Add(instance.monsterId);
        }

        bool changed = false;

        for (int i = 0; i < legacyMonsterIds.Count; i++)
        {
            string id = legacyMonsterIds[i];
            if (string.IsNullOrWhiteSpace(id))
                continue;

            if (!existingIds.Contains(id))
            {
                targetSave.gardenMonsters.Add(id);
                existingIds.Add(id);
                changed = true;
            }

            if (!existingInstanceIds.Contains(id))
            {
                targetSave.gardenMonsterInstances.Add(new GardenMonsterInstanceSave
                {
                    monsterId = id,
                    storedCoin = 0,
                    hasPosition = false
                });
                existingInstanceIds.Add(id);
                changed = true;
            }
        }

        return changed;
    }

    private static Dictionary<string, int> ToItemMap(List<ItemAmountSave> items)
    {
        Dictionary<string, int> result = new Dictionary<string, int>();
        if (items == null)
            return result;

        for (int i = 0; i < items.Count; i++)
        {
            ItemAmountSave item = items[i];
            if (item == null || string.IsNullOrWhiteSpace(item.itemId))
                continue;

            result[item.itemId] = Mathf.Max(0, item.amount);
        }

        return result;
    }

    private static List<ItemAmountSave> ToItemList(Dictionary<string, int> itemMap)
    {
        List<ItemAmountSave> result = new List<ItemAmountSave>();
        foreach (KeyValuePair<string, int> entry in itemMap)
        {
            if (!string.IsNullOrWhiteSpace(entry.Key) && entry.Value > 0)
                result.Add(new ItemAmountSave(entry.Key, entry.Value));
        }

        return result;
    }

    private static Dictionary<string, int> ToMonsterCountMap(List<MonsterCollectionSave> monsterCounts)
    {
        Dictionary<string, int> result = new Dictionary<string, int>();
        if (monsterCounts == null)
            return result;

        for (int i = 0; i < monsterCounts.Count; i++)
        {
            MonsterCollectionSave monsterCount = monsterCounts[i];
            if (monsterCount == null || string.IsNullOrWhiteSpace(monsterCount.monsterId))
                continue;

            result[monsterCount.monsterId] = Mathf.Max(0, monsterCount.count);
        }

        return result;
    }

    private static List<MonsterCollectionSave> ToMonsterCountList(Dictionary<string, int> monsterCountMap)
    {
        List<MonsterCollectionSave> result = new List<MonsterCollectionSave>();
        foreach (KeyValuePair<string, int> entry in monsterCountMap)
        {
            if (!string.IsNullOrWhiteSpace(entry.Key) && entry.Value > 0)
                result.Add(new MonsterCollectionSave(entry.Key, entry.Value));
        }

        return result;
    }

    private static string GetMarkerKey(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            userId = "local";

        return MigrationMarkerPrefix + userId.Trim();
    }
}
