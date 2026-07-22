using System.Threading.Tasks;

public interface ISaveService
{
    bool IsReady { get; }
    string UserId { get; }
    Task InitializeAsync();
    Task<GameSaveData> LoadAsync();
    Task SaveAsync(GameSaveData saveData);
}
