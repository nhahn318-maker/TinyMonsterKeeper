using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public int version = 2;
    public int coin;
    public List<ItemAmountSave> inventory = new List<ItemAmountSave>();
    public List<MonsterCollectionSave> monsterCollection = new List<MonsterCollectionSave>();
    public List<string> gardenMonsters = new List<string>();
    public List<GardenMonsterInstanceSave> gardenMonsterInstances = new List<GardenMonsterInstanceSave>();
    public List<string> unlockedFogZones = new List<string>();
    public List<string> discoveredRecipes = new List<string>();
    public List<string> failedMixes = new List<string>();
    public CookingSaveState cooking = new CookingSaveState();
    public List<ResourceNodeTimerSave> resourceNodeTimers = new List<ResourceNodeTimerSave>();
    public long lastSavedAtUnix;
    public bool forceApplyEmptyState;

    public static GameSaveData CreateNew()
    {
        return new GameSaveData
        {
            version = 2,
            lastSavedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    public bool HasAnyGameplayData()
    {
        return coin > 0
            || HasEntries(inventory)
            || HasEntries(monsterCollection)
            || HasEntries(gardenMonsters)
            || HasEntries(gardenMonsterInstances)
            || HasEntries(unlockedFogZones)
            || HasEntries(discoveredRecipes)
            || HasEntries(failedMixes)
            || (cooking != null && (cooking.isCooking || cooking.isDone))
            || HasEntries(resourceNodeTimers);
    }

    private static bool HasEntries<T>(List<T> values)
    {
        return values != null && values.Count > 0;
    }
}

[Serializable]
public class CookingSaveState
{
    public bool isCooking;
    public bool isDone;
    public string recipeId;
    public string monsterId;
    public long completeAtUnix;
}

[Serializable]
public class ResourceNodeTimerSave
{
    public string nodeId;
    public bool isReady;
    public long readyAtUnix;
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

[Serializable]
public class GardenMonsterInstanceSave
{
    public string monsterId;
    public float x;
    public float y;
    public float z;
    public int storedCoin;
    public bool hasPosition;
    public int friendship;
    public long nextPlayAtUnix;
    public long nextCoinAtUnix;

    public GardenMonsterInstanceSave()
    {
    }

    public GardenMonsterInstanceSave(string monsterId, UnityEngine.Vector3 position, int storedCoin)
    {
        this.monsterId = monsterId;
        x = position.x;
        y = position.y;
        z = position.z;
        this.storedCoin = storedCoin;
        hasPosition = true;
    }

    public UnityEngine.Vector3 GetPosition()
    {
        return new UnityEngine.Vector3(x, y, z);
    }
}
