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

    [Header("Unlock Conditions")]
    public int unlockAppealCost;      // Điểm appeal cần thiết để mở khóa
    public int unlockFriendshipCost;  // Tổng điểm friendship cần thiết (nếu có)
    public string unlockRequiredItemId; // Item cụ thể cần sở hữu để mở (như Ao Nhỏ - D004)
}
