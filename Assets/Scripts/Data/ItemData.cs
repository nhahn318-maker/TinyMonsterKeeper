using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "TinyMonsterKeeper/Item Data")]
public class ItemData : ScriptableObject {
    [Header("Basic Info")]
    public string itemId;
    public string itemName;
    public Sprite icon;

    [Header("Gameplay")]
    public int friendshipValue = 1;
}