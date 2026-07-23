using System.Collections;
using UnityEngine;

public class SaveGameRuntimeBinder : MonoBehaviour
{
    [Header("Data Lookup")]
    [SerializeField] private ItemData[] itemDatabase;
    [SerializeField] private MonsterData[] monsterDatabase;

    [Header("Sync")]
    [SerializeField] private bool loadSaveOnStart = true;
    [SerializeField] private bool autosaveOnChange = true;
    [SerializeField] private bool migrateLegacyPlayerPrefs = true;
    [SerializeField] private float gardenMonsterAutosaveInterval = 5f;

    private SaveManager saveManager;
    private bool isApplyingSave;

    private IEnumerator Start()
    {
        while (SaveSystemBootstrap.SaveManager == null || !SaveSystemBootstrap.SaveManager.IsReady)
            yield return null;

        saveManager = SaveSystemBootstrap.SaveManager;

        bool migratedLegacySave = migrateLegacyPlayerPrefs
            && LegacyPlayerPrefsSaveMigrator.TryMigrateInto(saveManager.CurrentSave, itemDatabase, monsterDatabase, saveManager.UserId);

        if (loadSaveOnStart && (saveManager.HasExistingSave || migratedLegacySave) && ShouldApplyLoadedSave(saveManager.CurrentSave))
            ApplySaveToGame();

        CaptureCurrentGameState();
        BindAutosaveEvents();
        if (autosaveOnChange && gardenMonsterAutosaveInterval > 0f)
            StartCoroutine(GardenMonsterAutosaveRoutine());

        saveManager.SaveSoon();
    }

    private void OnDestroy()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinChanged -= HandleCoinChanged;

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;

        MonsterCollectionManager.MonsterCollectionChanged -= HandleMonsterCollectionChanged;

        FogZoneManager fogZoneManager = FindObjectOfType<FogZoneManager>();
        if (fogZoneManager != null)
            fogZoneManager.FogZoneUnlocked -= HandleFogZoneUnlocked;

        if (GardenMonsterSaveManager.Instance != null)
            GardenMonsterSaveManager.Instance.GardenMonstersChanged -= HandleGardenMonstersChanged;
    }

    private void ApplySaveToGame()
    {
        if (saveManager == null || saveManager.CurrentSave == null)
            return;

        isApplyingSave = true;

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.SetCoin(saveManager.CurrentSave.coin);

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.ApplySavedItems(saveManager.CurrentSave.inventory, itemDatabase);

        MonsterCollectionManager.ApplySavedCounts(monsterDatabase, saveManager.CurrentSave.monsterCollection);

        FogZoneManager fogZoneManager = FindObjectOfType<FogZoneManager>();
        if (fogZoneManager != null)
            fogZoneManager.ApplyUnlockedZones(saveManager.CurrentSave.unlockedFogZones);

        if (GardenMonsterSaveManager.Instance != null)
        {
            if (saveManager.CurrentSave.gardenMonsterInstances != null && saveManager.CurrentSave.gardenMonsterInstances.Count > 0)
                GardenMonsterSaveManager.Instance.ApplySavedGardenMonsters(saveManager.CurrentSave.gardenMonsterInstances, true);
            else
                GardenMonsterSaveManager.Instance.ApplySavedGardenMonsters(saveManager.CurrentSave.gardenMonsters, true);
        }

        isApplyingSave = false;
    }

    private static bool ShouldApplyLoadedSave(GameSaveData saveData)
    {
        return saveData != null && (saveData.HasAnyGameplayData() || saveData.forceApplyEmptyState);
    }

    private void BindAutosaveEvents()
    {
        if (!autosaveOnChange)
            return;

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinChanged += HandleCoinChanged;

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;

        MonsterCollectionManager.MonsterCollectionChanged += HandleMonsterCollectionChanged;

        FogZoneManager fogZoneManager = FindObjectOfType<FogZoneManager>();
        if (fogZoneManager != null)
            fogZoneManager.FogZoneUnlocked += HandleFogZoneUnlocked;

        if (GardenMonsterSaveManager.Instance != null)
            GardenMonsterSaveManager.Instance.GardenMonstersChanged += HandleGardenMonstersChanged;
    }

    private void CaptureCurrentGameState()
    {
        if (saveManager == null || saveManager.CurrentSave == null)
            return;

        if (CurrencyManager.Instance != null)
            saveManager.CurrentSave.coin = CurrencyManager.Instance.Coin;

        if (InventoryManager.Instance != null)
            saveManager.CurrentSave.inventory = InventoryManager.Instance.ExportItemAmounts();

        saveManager.CurrentSave.monsterCollection = MonsterCollectionManager.ExportCounts(monsterDatabase);

        FogZoneManager fogZoneManager = FindObjectOfType<FogZoneManager>();
        if (fogZoneManager != null)
            saveManager.CurrentSave.unlockedFogZones = fogZoneManager.ExportUnlockedZoneIds();

        if (GardenMonsterSaveManager.Instance != null)
        {
            saveManager.CurrentSave.gardenMonsters = GardenMonsterSaveManager.Instance.ExportGardenMonsterIds();
            saveManager.CurrentSave.gardenMonsterInstances = GardenMonsterSaveManager.Instance.ExportGardenMonsterInstances();
        }
    }

    private void HandleCoinChanged(int coin)
    {
        if (isApplyingSave || saveManager == null || saveManager.CurrentSave == null)
            return;

        saveManager.CurrentSave.coin = coin;
        saveManager.SaveSoon();
    }

    private void HandleInventoryChanged()
    {
        if (isApplyingSave || saveManager == null || saveManager.CurrentSave == null || InventoryManager.Instance == null)
            return;

        saveManager.CurrentSave.inventory = InventoryManager.Instance.ExportItemAmounts();
        saveManager.SaveSoon();
    }

    private void HandleMonsterCollectionChanged(MonsterData monsterData, int count)
    {
        if (isApplyingSave || saveManager == null || saveManager.CurrentSave == null)
            return;

        saveManager.CurrentSave.monsterCollection = MonsterCollectionManager.ExportCounts(monsterDatabase);
        saveManager.SaveSoon();
    }

    private void HandleFogZoneUnlocked(string zoneId)
    {
        if (isApplyingSave || saveManager == null || saveManager.CurrentSave == null)
            return;

        FogZoneManager fogZoneManager = FindObjectOfType<FogZoneManager>();
        if (fogZoneManager != null)
            saveManager.CurrentSave.unlockedFogZones = fogZoneManager.ExportUnlockedZoneIds();

        saveManager.SaveSoon();
    }

    private void HandleGardenMonstersChanged()
    {
        if (isApplyingSave || saveManager == null || saveManager.CurrentSave == null || GardenMonsterSaveManager.Instance == null)
            return;

        saveManager.CurrentSave.gardenMonsters = GardenMonsterSaveManager.Instance.ExportGardenMonsterIds();
        saveManager.CurrentSave.gardenMonsterInstances = GardenMonsterSaveManager.Instance.ExportGardenMonsterInstances();
        saveManager.SaveSoon();
    }

    private IEnumerator GardenMonsterAutosaveRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(gardenMonsterAutosaveInterval);

        while (true)
        {
            yield return wait;

            if (isApplyingSave || saveManager == null || saveManager.CurrentSave == null || GardenMonsterSaveManager.Instance == null)
                continue;

            saveManager.CurrentSave.gardenMonsters = GardenMonsterSaveManager.Instance.ExportGardenMonsterIds();
            saveManager.CurrentSave.gardenMonsterInstances = GardenMonsterSaveManager.Instance.ExportGardenMonsterInstances();
            saveManager.SaveSoon();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveCurrentStateSoon();
    }

    private void OnApplicationQuit()
    {
        SaveCurrentStateSoon();
    }

    private void SaveCurrentStateSoon()
    {
        if (saveManager == null || saveManager.CurrentSave == null)
            return;

        CaptureCurrentGameState();
        saveManager.SaveSoon();
    }
}
