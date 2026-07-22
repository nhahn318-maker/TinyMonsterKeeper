using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public int version = 1;
    public int coin;
    public List<ItemAmountSave> inventory = new List<ItemAmountSave>();
    public List<MonsterCollectionSave> monsterCollection = new List<MonsterCollectionSave>();
    public List<string> gardenMonsters = new List<string>();
    public List<string> unlockedFogZones = new List<string>();
    public List<string> discoveredRecipes = new List<string>();
    public List<string> failedMixes = new List<string>();
    public long lastSavedAtUnix;

    public static GameSaveData CreateNew()
    {
        return new GameSaveData
        {
            version = 1,
            lastSavedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    public bool HasAnyGameplayData()
    {
        return coin > 0
            || HasEntries(inventory)
            || HasEntries(monsterCollection)
            || HasEntries(gardenMonsters)
            || HasEntries(unlockedFogZones)
            || HasEntries(discoveredRecipes)
            || HasEntries(failedMixes);
    }

    private static bool HasEntries<T>(List<T> values)
    {
        return values != null && values.Count > 0;
    }
}

[Serializable]
public class ItemAmountSave
{
    public string itemId;
    public int amount;

    public ItemAmountSave()
    {
    }

    public ItemAmountSave(string itemId, int amount)
    {
        this.itemId = itemId;
        this.amount = amount;
    }
}

[Serializable]
public class MonsterCollectionSave
{
    public string monsterId;
    public int count;

    public MonsterCollectionSave()
    {
    }

    public MonsterCollectionSave(string monsterId, int count)
    {
        this.monsterId = monsterId;
        this.count = count;
    }
}
