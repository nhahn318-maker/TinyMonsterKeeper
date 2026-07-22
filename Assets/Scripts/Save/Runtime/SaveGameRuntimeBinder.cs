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

    private SaveManager saveManager;
    private bool isApplyingSave;

    private IEnumerator Start()
    {
        while (SaveSystemBootstrap.SaveManager == null || !SaveSystemBootstrap.SaveManager.IsReady)
            yield return null;

        saveManager = SaveSystemBootstrap.SaveManager;

        if (loadSaveOnStart && saveManager.HasExistingSave && ShouldApplyLoadedSave(saveManager.CurrentSave))
            ApplySaveToGame();

        CaptureCurrentGameState();
        BindAutosaveEvents();
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
}
