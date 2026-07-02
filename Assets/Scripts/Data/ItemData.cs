using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "TinyMonsterKeeper/Item Data")]
public class ItemData : ScriptableObject {
    [Header("Basic Info")]
    public string itemId;
    public string itemName;
    public Sprite icon;

    [Header("UI")]
    public Vector2 cookingIconSize = new Vector2(24f, 24f);

    [Header("Gameplay")]
    public int friendshipValue = 1;

    public bool HasCustomCookingIconSize()
    {
        return cookingIconSize.x > 0f && cookingIconSize.y > 0f;
    }
}
