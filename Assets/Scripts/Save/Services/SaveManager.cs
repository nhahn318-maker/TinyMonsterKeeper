using System;
using System.Threading.Tasks;
using UnityEngine;

public class SaveManager
{
    private readonly ISaveService localService = new LocalSaveService();
    private readonly ISaveService cloudService;

    public GameSaveData CurrentSave { get; private set; }
    public bool IsReady { get; private set; }
    public bool HasExistingSave { get; private set; }
    public bool IsCloudReady => cloudService != null && cloudService.IsReady;
    public string UserId => IsCloudReady ? cloudService.UserId : localService.UserId;

    public event Action<GameSaveData> SaveLoaded;
    public event Action<GameSaveData> SaveChanged;

    public SaveManager(ISaveService cloudService)
    {
        this.cloudService = cloudService;
    }

    public async Task InitializeAsync()
    {
        await localService.InitializeAsync();

        GameSaveData localSave = await localService.LoadAsync();
        GameSaveData cloudSave = null;

        if (cloudService != null)
        {
            try
            {
                await cloudService.InitializeAsync();
                if (cloudService.IsReady)
                    cloudSave = await cloudService.LoadAsync();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Cloud save initialization failed. Local save will be used. " + exception.Message);
            }
        }

        HasExistingSave = localSave != null || cloudSave != null;
        CurrentSave = ChooseNewestSave(localSave, cloudSave) ?? GameSaveData.CreateNew();
        IsReady = true;

        await SaveAsync();
        SaveLoaded?.Invoke(CurrentSave);
    }

    public async Task SaveAsync()
    {
        if (CurrentSave == null)
            CurrentSave = GameSaveData.CreateNew();

        CurrentSave.lastSavedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await localService.SaveAsync(CurrentSave);

        if (cloudService != null && cloudService.IsReady)
        {
            try
            {
                await cloudService.SaveAsync(CurrentSave);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Cloud save failed. Local save is still available. " + exception.Message);
            }
        }

        SaveChanged?.Invoke(CurrentSave);
    }

    public async void SaveSoon()
    {
        try
        {
            await SaveAsync();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Save failed: " + exception.Message);
        }
    }

    private static GameSaveData ChooseNewestSave(GameSaveData localSave, GameSaveData cloudSave)
    {
        if (localSave == null)
            return cloudSave;

        if (cloudSave == null)
            return localSave;

        return cloudSave.lastSavedAtUnix >= localSave.lastSavedAtUnix ? cloudSave : localSave;
    }
}
