using System.Collections.Generic;
using UnityEngine;

public class GardenMonsterSaveManager : MonoBehaviour
{
    private const string SavePrefix = "TinyMonsterKeeper.GardenMonster.";

    public static GardenMonsterSaveManager Instance { get; private set; }
    public event System.Action GardenMonstersChanged;

    [SerializeField] private MonsterData[] monsters;
    [SerializeField] private Collider2D gardenBounds;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private Vector3 spawnCenter;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(2f, 2f);
    [SerializeField] private bool spawnDefaultUnlockedMonsters = true;

    private readonly HashSet<string> spawnedMonsterIds = new HashSet<string>();
    private readonly Dictionary<string, TinyMonsterController> trackedMonsters = new Dictionary<string, TinyMonsterController>();
    private readonly HashSet<TinyMonsterCoinProducer> trackedCoinProducers = new HashSet<TinyMonsterCoinProducer>();
    private bool cloudSaveControlsLoading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (!cloudSaveControlsLoading && SaveSystemBootstrap.SaveManager == null)
            LoadSavedMonstersFromPlayerPrefs();
    }

    private void OnDestroy()
    {
        foreach (TinyMonsterCoinProducer coinProducer in trackedCoinProducers)
        {
            if (coinProducer != null)
                coinProducer.StoredCoinChanged -= HandleMonsterStoredCoinChanged;
        }

        trackedCoinProducers.Clear();
    }

    public void SaveMonsterInGarden(MonsterData monsterData)
    {
        string id = MonsterCollectionManager.GetMonsterId(monsterData);
        if (string.IsNullOrEmpty(id))
            return;

        PlayerPrefs.SetInt(SavePrefix + id, 1);
        PlayerPrefs.Save();
        spawnedMonsterIds.Add(id);
        GardenMonstersChanged?.Invoke();
    }

    public void SaveMonsterInGarden(TinyMonsterController monster)
    {
        if (monster == null || monster.Data == null)
            return;

        TrackMonster(monster);
        SaveMonsterInGarden(monster.Data);
    }

    public void ApplySavedGardenMonsters(List<string> savedMonsterIds, bool includeDefaultUnlockedMonsters)
    {
        List<GardenMonsterInstanceSave> instances = new List<GardenMonsterInstanceSave>();
        if (savedMonsterIds != null)
        {
            for (int i = 0; i < savedMonsterIds.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(savedMonsterIds[i]))
                {
                    instances.Add(new GardenMonsterInstanceSave
                    {
                        monsterId = savedMonsterIds[i].Trim(),
                        storedCoin = 0,
                        hasPosition = false
                    });
                }
            }
        }

        ApplySavedGardenMonsters(instances, includeDefaultUnlockedMonsters);
    }

    public void ApplySavedGardenMonsters(List<GardenMonsterInstanceSave> savedMonsterInstances, bool includeDefaultUnlockedMonsters)
    {
        cloudSaveControlsLoading = true;
        ClearSpawnedMonsterSet();
        CacheExistingMonsters();

        Dictionary<string, GardenMonsterInstanceSave> targetInstances = new Dictionary<string, GardenMonsterInstanceSave>();
        if (savedMonsterInstances != null)
        {
            for (int i = 0; i < savedMonsterInstances.Count; i++)
            {
                GardenMonsterInstanceSave instance = savedMonsterInstances[i];
                if (instance == null || string.IsNullOrWhiteSpace(instance.monsterId))
                    continue;

                string id = instance.monsterId.Trim();
                if (!targetInstances.ContainsKey(id))
                    targetInstances[id] = instance;
            }
        }

        if (includeDefaultUnlockedMonsters && monsters != null)
        {
            for (int i = 0; i < monsters.Length; i++)
            {
                if (monsters[i] == null)
                    continue;

                string id = MonsterCollectionManager.GetMonsterId(monsters[i]);
                if (!string.IsNullOrEmpty(id) && MonsterCollectionManager.IsUnlocked(monsters[i]) && !targetInstances.ContainsKey(id))
                    targetInstances[id] = new GardenMonsterInstanceSave(id, GetSpawnPosition(), 0);
            }
        }

        ApplyExistingMonsterState(targetInstances);
        SpawnMissingMonsters(targetInstances);
    }

    public List<string> ExportGardenMonsterIds()
    {
        CacheExistingMonsters();
        return new List<string>(spawnedMonsterIds);
    }

    public List<GardenMonsterInstanceSave> ExportGardenMonsterInstances()
    {
        CacheExistingMonsters();
        List<GardenMonsterInstanceSave> instances = new List<GardenMonsterInstanceSave>();

        foreach (KeyValuePair<string, TinyMonsterController> entry in trackedMonsters)
        {
            TinyMonsterController monster = entry.Value;
            if (monster == null || monster.Data == null)
                continue;

            TinyMonsterCoinProducer coinProducer = monster.GetComponent<TinyMonsterCoinProducer>();
            int storedCoin = coinProducer != null ? coinProducer.StoredCoin : 0;
            instances.Add(new GardenMonsterInstanceSave(entry.Key, monster.transform.position, storedCoin));
        }

        return instances;
    }

    public MonsterData[] MonsterDatabase => monsters;

    public static List<string> ExportLegacySavedMonsterIds(MonsterData[] monsterDatabase)
    {
        List<string> savedMonsterIds = new List<string>();
        if (monsterDatabase == null)
            return savedMonsterIds;

        for (int i = 0; i < monsterDatabase.Length; i++)
        {
            MonsterData monsterData = monsterDatabase[i];
            string id = MonsterCollectionManager.GetMonsterId(monsterData);
            if (string.IsNullOrEmpty(id))
                continue;

            if (PlayerPrefs.GetInt(SavePrefix + id, 0) == 1)
                savedMonsterIds.Add(id);
        }

        return savedMonsterIds;
    }

    private void LoadSavedMonstersFromPlayerPrefs()
    {
        if (monsters == null)
            return;

        CacheExistingMonsters();
        HashSet<string> targetMonsterIds = new HashSet<string>();

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterData monsterData = monsters[i];
            if (monsterData == null || monsterData.prefab == null)
                continue;

            string id = MonsterCollectionManager.GetMonsterId(monsterData);
            if (string.IsNullOrEmpty(id) || spawnedMonsterIds.Contains(id))
                continue;

            bool shouldSpawn = PlayerPrefs.GetInt(SavePrefix + id, 0) == 1;
            if (!shouldSpawn && spawnDefaultUnlockedMonsters)
                shouldSpawn = MonsterCollectionManager.IsUnlocked(monsterData);

            if (shouldSpawn)
                targetMonsterIds.Add(id);
        }

        Dictionary<string, GardenMonsterInstanceSave> targetInstances = new Dictionary<string, GardenMonsterInstanceSave>();
        foreach (string id in targetMonsterIds)
            targetInstances[id] = new GardenMonsterInstanceSave(id, GetSpawnPosition(), 0);

        SpawnMissingMonsters(targetInstances);
    }

    private void ApplyExistingMonsterState(Dictionary<string, GardenMonsterInstanceSave> targetInstances)
    {
        if (targetInstances == null)
            return;

        foreach (KeyValuePair<string, GardenMonsterInstanceSave> entry in targetInstances)
        {
            if (!trackedMonsters.TryGetValue(entry.Key, out TinyMonsterController monster) || monster == null)
                continue;

            ApplyMonsterInstanceState(monster, entry.Value);
        }
    }

    private void SpawnMissingMonsters(Dictionary<string, GardenMonsterInstanceSave> targetInstances)
    {
        if (targetInstances == null || monsters == null)
            return;

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterData monsterData = monsters[i];
            if (monsterData == null || monsterData.prefab == null)
                continue;

            string id = MonsterCollectionManager.GetMonsterId(monsterData);
            if (string.IsNullOrEmpty(id) || spawnedMonsterIds.Contains(id))
                continue;

            if (!targetInstances.TryGetValue(id, out GardenMonsterInstanceSave instance))
                continue;

            SpawnMonster(monsterData, instance);
        }
    }

    private void CacheExistingMonsters()
    {
        TinyMonsterController[] existingMonsters = FindObjectsOfType<TinyMonsterController>();
        for (int i = 0; i < existingMonsters.Length; i++)
        {
            if (existingMonsters[i] == null || existingMonsters[i].Data == null)
                continue;

            string id = MonsterCollectionManager.GetMonsterId(existingMonsters[i].Data);
            if (string.IsNullOrEmpty(id))
                continue;

            spawnedMonsterIds.Add(id);
            TrackMonster(existingMonsters[i]);
        }
    }

    private void ClearSpawnedMonsterSet()
    {
        spawnedMonsterIds.Clear();
        trackedMonsters.Clear();
    }

    private void SpawnMonster(MonsterData monsterData, GardenMonsterInstanceSave instance = null)
    {
        Vector3 spawnPosition = instance != null && instance.hasPosition ? instance.GetPosition() : GetSpawnPosition();
        GameObject monster = Instantiate(monsterData.prefab, spawnPosition, Quaternion.identity, spawnParent);
        ConfigureMonster(monster);

        TinyMonsterController controller = monster.GetComponent<TinyMonsterController>();
        if (controller != null)
        {
            TrackMonster(controller);
            if (instance != null)
                ApplyMonsterInstanceState(controller, instance);
        }

        string id = MonsterCollectionManager.GetMonsterId(monsterData);
        if (!string.IsNullOrEmpty(id))
            spawnedMonsterIds.Add(id);
    }

    private void ApplyMonsterInstanceState(TinyMonsterController monster, GardenMonsterInstanceSave instance)
    {
        if (monster == null || instance == null)
            return;

        TinyMonsterNavRoam navRoam = monster.GetComponent<TinyMonsterNavRoam>();
        if (instance.hasPosition && navRoam != null)
            navRoam.WarpTo(instance.GetPosition());
        else if (instance.hasPosition)
            monster.transform.position = instance.GetPosition();

        TinyMonsterCoinProducer coinProducer = monster.GetComponent<TinyMonsterCoinProducer>();
        if (coinProducer != null)
            coinProducer.SetStoredCoin(instance.storedCoin);
    }

    private void TrackMonster(TinyMonsterController monster)
    {
        if (monster == null || monster.Data == null)
            return;

        string id = MonsterCollectionManager.GetMonsterId(monster.Data);
        if (string.IsNullOrEmpty(id))
            return;

        trackedMonsters[id] = monster;

        TinyMonsterCoinProducer coinProducer = monster.GetComponent<TinyMonsterCoinProducer>();
        if (coinProducer != null && !trackedCoinProducers.Contains(coinProducer))
        {
            trackedCoinProducers.Add(coinProducer);
            coinProducer.StoredCoinChanged += HandleMonsterStoredCoinChanged;
        }
    }

    private void HandleMonsterStoredCoinChanged()
    {
        GardenMonstersChanged?.Invoke();
    }

    private Vector3 GetSpawnPosition()
    {
        if (gardenBounds == null)
        {
            return transform.position + spawnCenter;
        }

        Bounds bounds = gardenBounds.bounds;
        Vector2 halfSize = spawnAreaSize * 0.5f;

        for (int i = 0; i < 20; i++)
        {
            Vector3 randomPoint = transform.position + spawnCenter + new Vector3(
                Random.Range(-halfSize.x, halfSize.x),
                Random.Range(-halfSize.y, halfSize.y),
                0f
            );

            randomPoint.z = 0f;
            if (gardenBounds.OverlapPoint(randomPoint))
                return randomPoint;
        }

        return bounds.center;
    }

    private void ConfigureMonster(GameObject monster)
    {
        if (monster == null)
            return;

        TinyMonsterNavRoam navRoam = monster.GetComponent<TinyMonsterNavRoam>();
        if (navRoam != null && gardenBounds != null)
            navRoam.SetGardenBounds(gardenBounds);
    }
}
