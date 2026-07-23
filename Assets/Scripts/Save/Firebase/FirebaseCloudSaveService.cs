using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

public class FirebaseCloudSaveService : ISaveService
{
    private readonly FirebaseAccountAuthService authService = new FirebaseAccountAuthService();
    private FirebaseFirestore firestore;

    public bool IsReady { get; private set; }
    public string UserId => authService.UserId;

    public async Task InitializeAsync()
    {
        IsReady = await authService.InitializeAndSignInAsync();
        if (!IsReady)
            return;

        firestore = FirebaseFirestore.DefaultInstance;
    }

    public async Task<GameSaveData> LoadAsync()
    {
        if (!IsReady || firestore == null)
            return null;

        DocumentSnapshot snapshot = await GetSaveDocument().GetSnapshotAsync();
        if (!snapshot.Exists)
            return null;

        return FromFirestoreDictionary(snapshot.ToDictionary());
    }

    public async Task SaveAsync(GameSaveData saveData)
    {
        if (!IsReady || firestore == null || saveData == null)
            return;

        saveData.lastSavedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await GetSaveDocument().SetAsync(ToFirestoreDictionary(saveData), SetOptions.MergeAll);
    }

    private DocumentReference GetSaveDocument()
    {
        return firestore.Collection("users").Document(UserId).Collection("save").Document("main");
    }

    private static Dictionary<string, object> ToFirestoreDictionary(GameSaveData saveData)
    {
        return new Dictionary<string, object>
        {
            { "version", saveData.version },
            { "coin", saveData.coin },
            { "inventory", ItemListToMap(saveData.inventory) },
            { "monsterCollection", MonsterListToMap(saveData.monsterCollection) },
            { "gardenMonsters", CleanStringList(saveData.gardenMonsters) },
            { "gardenMonsterInstances", GardenMonsterInstancesToList(saveData.gardenMonsterInstances) },
            { "unlockedFogZones", CleanStringList(saveData.unlockedFogZones) },
            { "discoveredRecipes", CleanStringList(saveData.discoveredRecipes) },
            { "failedMixes", CleanStringList(saveData.failedMixes) },
            { "lastSavedAtUnix", saveData.lastSavedAtUnix },
            { "forceApplyEmptyState", saveData.forceApplyEmptyState }
        };
    }

    private static GameSaveData FromFirestoreDictionary(Dictionary<string, object> data)
    {
        GameSaveData saveData = GameSaveData.CreateNew();
        saveData.version = ReadInt(data, "version", 1);
        saveData.coin = ReadInt(data, "coin", 0);
        saveData.inventory = MapToItemList(ReadDictionary(data, "inventory"));
        saveData.monsterCollection = MapToMonsterList(ReadDictionary(data, "monsterCollection"));
        saveData.gardenMonsters = ReadStringList(data, "gardenMonsters");
        saveData.gardenMonsterInstances = ReadGardenMonsterInstances(data, "gardenMonsterInstances");
        saveData.unlockedFogZones = ReadStringList(data, "unlockedFogZones");
        saveData.discoveredRecipes = ReadStringList(data, "discoveredRecipes");
        saveData.failedMixes = ReadStringList(data, "failedMixes");
        saveData.lastSavedAtUnix = ReadLong(data, "lastSavedAtUnix", 0);
        saveData.forceApplyEmptyState = ReadBool(data, "forceApplyEmptyState", false);
        return saveData;
    }

    private static Dictionary<string, object> ItemListToMap(List<ItemAmountSave> items)
    {
        Dictionary<string, object> map = new Dictionary<string, object>();
        if (items == null)
            return map;

        for (int i = 0; i < items.Count; i++)
        {
            ItemAmountSave item = items[i];
            if (item == null || string.IsNullOrWhiteSpace(item.itemId))
                continue;

            map[item.itemId] = Mathf.Max(0, item.amount);
        }

        return map;
    }

    private static Dictionary<string, object> MonsterListToMap(List<MonsterCollectionSave> monsters)
    {
        Dictionary<string, object> map = new Dictionary<string, object>();
        if (monsters == null)
            return map;

        for (int i = 0; i < monsters.Count; i++)
        {
            MonsterCollectionSave monster = monsters[i];
            if (monster == null || string.IsNullOrWhiteSpace(monster.monsterId))
                continue;

            map[monster.monsterId] = Mathf.Max(0, monster.count);
        }

        return map;
    }

    private static List<ItemAmountSave> MapToItemList(Dictionary<string, object> map)
    {
        List<ItemAmountSave> items = new List<ItemAmountSave>();
        foreach (KeyValuePair<string, object> entry in map)
            items.Add(new ItemAmountSave(entry.Key, ConvertToInt(entry.Value, 0)));

        return items;
    }

    private static List<MonsterCollectionSave> MapToMonsterList(Dictionary<string, object> map)
    {
        List<MonsterCollectionSave> monsters = new List<MonsterCollectionSave>();
        foreach (KeyValuePair<string, object> entry in map)
            monsters.Add(new MonsterCollectionSave(entry.Key, ConvertToInt(entry.Value, 0)));

        return monsters;
    }

    private static List<Dictionary<string, object>> GardenMonsterInstancesToList(List<GardenMonsterInstanceSave> instances)
    {
        List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
        if (instances == null)
            return result;

        for (int i = 0; i < instances.Count; i++)
        {
            GardenMonsterInstanceSave instance = instances[i];
            if (instance == null || string.IsNullOrWhiteSpace(instance.monsterId))
                continue;

            result.Add(new Dictionary<string, object>
            {
                { "monsterId", instance.monsterId },
                { "x", instance.x },
                { "y", instance.y },
                { "z", instance.z },
                { "storedCoin", Mathf.Max(0, instance.storedCoin) },
                { "hasPosition", instance.hasPosition }
            });
        }

        return result;
    }

    private static List<string> CleanStringList(List<string> values)
    {
        List<string> result = new List<string>();
        if (values == null)
            return result;

        for (int i = 0; i < values.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
                result.Add(values[i]);
        }

        return result;
    }

    private static List<string> ReadStringList(Dictionary<string, object> data, string key)
    {
        List<string> values = new List<string>();
        if (!data.TryGetValue(key, out object raw) || raw == null)
            return values;

        if (raw is IEnumerable enumerable)
        {
            foreach (object item in enumerable)
            {
                if (item != null)
                    values.Add(item.ToString());
            }
        }

        return values;
    }

    private static List<GardenMonsterInstanceSave> ReadGardenMonsterInstances(Dictionary<string, object> data, string key)
    {
        List<GardenMonsterInstanceSave> instances = new List<GardenMonsterInstanceSave>();
        if (!data.TryGetValue(key, out object raw) || raw == null)
            return instances;

        if (!(raw is IEnumerable enumerable))
            return instances;

        foreach (object item in enumerable)
        {
            Dictionary<string, object> map = ToObjectDictionary(item);
            if (map.Count == 0)
                continue;

            string monsterId = ReadString(map, "monsterId", string.Empty);
            if (string.IsNullOrWhiteSpace(monsterId))
                continue;

            instances.Add(new GardenMonsterInstanceSave
            {
                monsterId = monsterId,
                x = ReadFloat(map, "x", 0f),
                y = ReadFloat(map, "y", 0f),
                z = ReadFloat(map, "z", 0f),
                storedCoin = ReadInt(map, "storedCoin", 0),
                hasPosition = ReadBool(map, "hasPosition", true)
            });
        }

        return instances;
    }

    private static Dictionary<string, object> ToObjectDictionary(object raw)
    {
        Dictionary<string, object> result = new Dictionary<string, object>();
        if (raw == null)
            return result;

        if (raw is Dictionary<string, object> dictionary)
            return dictionary;

        if (raw is IDictionary genericDictionary)
        {
            foreach (DictionaryEntry entry in genericDictionary)
            {
                if (entry.Key != null)
                    result[entry.Key.ToString()] = entry.Value;
            }
        }

        return result;
    }

    private static Dictionary<string, object> ReadDictionary(Dictionary<string, object> data, string key)
    {
        Dictionary<string, object> result = new Dictionary<string, object>();
        if (!data.TryGetValue(key, out object raw) || raw == null)
            return result;

        if (raw is Dictionary<string, object> dictionary)
            return dictionary;

        if (raw is IDictionary genericDictionary)
        {
            foreach (DictionaryEntry entry in genericDictionary)
            {
                if (entry.Key != null)
                    result[entry.Key.ToString()] = entry.Value;
            }
        }

        return result;
    }

    private static int ReadInt(Dictionary<string, object> data, string key, int fallback)
    {
        return data.TryGetValue(key, out object raw) ? ConvertToInt(raw, fallback) : fallback;
    }

    private static long ReadLong(Dictionary<string, object> data, string key, long fallback)
    {
        if (!data.TryGetValue(key, out object raw) || raw == null)
            return fallback;

        try
        {
            return Convert.ToInt64(raw);
        }
        catch
        {
            return fallback;
        }
    }

    private static bool ReadBool(Dictionary<string, object> data, string key, bool fallback)
    {
        if (!data.TryGetValue(key, out object raw) || raw == null)
            return fallback;

        try
        {
            return Convert.ToBoolean(raw);
        }
        catch
        {
            return fallback;
        }
    }

    private static string ReadString(Dictionary<string, object> data, string key, string fallback)
    {
        return data.TryGetValue(key, out object raw) && raw != null ? raw.ToString() : fallback;
    }

    private static float ReadFloat(Dictionary<string, object> data, string key, float fallback)
    {
        if (!data.TryGetValue(key, out object raw) || raw == null)
            return fallback;

        try
        {
            return Convert.ToSingle(raw);
        }
        catch
        {
            return fallback;
        }
    }

    private static int ConvertToInt(object raw, int fallback)
    {
        if (raw == null)
            return fallback;

        try
        {
            return Convert.ToInt32(raw);
        }
        catch
        {
            return fallback;
        }
    }
}
