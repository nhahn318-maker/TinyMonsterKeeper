using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour {
    public static InventoryManager Instance { get; private set; }

    private Dictionary<string, int> itemAmounts = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddItem(ItemData itemData, int amount)
    {
        if (itemData == null)
        {
            Debug.LogWarning("ItemData is null!");
            return;
        }

        if (amount <= 0) return;

        string id = itemData.itemId;

        if (itemAmounts.ContainsKey(id))
        {
            itemAmounts[id] += amount;
        }
        else
        {
            itemAmounts.Add(id, amount);
        }

        Debug.Log($"Added {amount} {itemData.itemName}. Total: {itemAmounts[id]}");
    }

    public int GetItemAmount(ItemData itemData)
    {
        if (itemData == null) return 0;

        if (itemAmounts.TryGetValue(itemData.itemId, out int amount))
        {
            return amount;
        }

        return 0;
    }

    public bool RemoveItem(ItemData itemData, int amount)
    {
        if (itemData == null) return false;
        if (amount <= 0) return false;

        string id = itemData.itemId;

        int currentAmount = GetItemAmount(itemData);

        if (currentAmount < amount)
        {
            Debug.Log("Not enough item: " + itemData.itemName);
            return false;
        }

        itemAmounts[id] = currentAmount - amount;

        Debug.Log($"Removed {amount} {itemData.itemName}. Total: {itemAmounts[id]}");

        return true;
    }
}