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

    public void ApplySavedGardenMonsters(List<string> savedMonsterIds, bool includeDefaultUnlockedMonsters)
    {
        cloudSaveControlsLoading = true;
        ClearSpawnedMonsterSet();
        CacheExistingMonsters();

        HashSet<string> targetMonsterIds = new HashSet<string>();
        if (savedMonsterIds != null)
        {
            for (int i = 0; i < savedMonsterIds.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(savedMonsterIds[i]))
                    targetMonsterIds.Add(savedMonsterIds[i].Trim());
            }
        }

        if (includeDefaultUnlockedMonsters && monsters != null)
        {
            for (int i = 0; i < monsters.Length; i++)
            {
                if (monsters[i] == null)
                    continue;

                string id = MonsterCollectionManager.GetMonsterId(monsters[i]);
                if (!string.IsNullOrEmpty(id) && MonsterCollectionManager.IsUnlocked(monsters[i]))
                    targetMonsterIds.Add(id);
            }
        }

        SpawnMissingMonsters(targetMonsterIds);
    }

    public List<string> ExportGardenMonsterIds()
    {
        CacheExistingMonsters();
        return new List<string>(spawnedMonsterIds);
    }

    public MonsterData[] MonsterDatabase => monsters;

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

        SpawnMissingMonsters(targetMonsterIds);
    }

    private void SpawnMissingMonsters(HashSet<string> monsterIds)
    {
        if (monsterIds == null || monsters == null)
            return;

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterData monsterData = monsters[i];
            if (monsterData == null || monsterData.prefab == null)
                continue;

            string id = MonsterCollectionManager.GetMonsterId(monsterData);
            if (string.IsNullOrEmpty(id) || spawnedMonsterIds.Contains(id))
                continue;

            if (!monsterIds.Contains(id))
                continue;

            SpawnMonster(monsterData);
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
            if (!string.IsNullOrEmpty(id))
                spawnedMonsterIds.Add(id);
        }
    }

    private void ClearSpawnedMonsterSet()
    {
        spawnedMonsterIds.Clear();
    }

    private void SpawnMonster(MonsterData monsterData)
    {
        GameObject monster = Instantiate(monsterData.prefab, GetSpawnPosition(), Quaternion.identity, spawnParent);
        ConfigureMonster(monster);

        string id = MonsterCollectionManager.GetMonsterId(monsterData);
        if (!string.IsNullOrEmpty(id))
            spawnedMonsterIds.Add(id);
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
