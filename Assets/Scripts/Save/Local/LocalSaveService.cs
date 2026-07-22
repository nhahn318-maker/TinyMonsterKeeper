using System.Threading.Tasks;
using UnityEngine;

public class LocalSaveService : ISaveService
{
    private const string SaveKey = "TinyMonsterKeeper.LocalGameSave";

    public bool IsReady { get; private set; }
    public string UserId => "local";

    public Task InitializeAsync()
    {
        IsReady = true;
        return Task.CompletedTask;
    }

    public Task<GameSaveData> LoadAsync()
    {
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return Task.FromResult<GameSaveData>(null);

        try
        {
            return Task.FromResult(JsonUtility.FromJson<GameSaveData>(json));
        }
        catch
        {
            Debug.LogWarning("Local save data is invalid. A new save will be created.");
            return Task.FromResult<GameSaveData>(null);
        }
    }

    public Task SaveAsync(GameSaveData saveData)
    {
        if (saveData == null)
            return Task.CompletedTask;

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saveData));
        PlayerPrefs.Save();
        return Task.CompletedTask;
    }
}
