using UnityEngine;

[CreateAssetMenu(fileName = "NewMonsterData", menuName = "TinyMonsterKeeper/Monster Data")]
public class MonsterData : ScriptableObject {
    [Header("Basic Info")]
    public string id; // Ví dụ: M001
    public string monsterName;
    public Sprite icon;
    public GameObject prefab;

    [Header("Preferences")]
    public string favoriteFoodId; // ID món ăn ưa thích
    public string favoriteToyId;  // ID đồ chơi ưa thích

    [Header("Feed Settings")]
    public int berryCostPerFeed = 1;
    public int feedFriendshipGain = 10;

    [Header("Unlock Conditions")]
    public int unlockAppealCost;
    public int unlockFriendshipCost;
    public string unlockRequiredItemId;
}