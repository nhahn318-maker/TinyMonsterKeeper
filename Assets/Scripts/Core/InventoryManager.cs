using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour {
    [Serializable]
    private class StartingItemStack
    {
        public ItemData itemData;
        public int amount;
    }

    public static InventoryManager Instance { get; private set; }

    [SerializeField] private List<StartingItemStack> startingItems = new List<StartingItemStack>();

    public event Action OnInventoryChanged;

    private Dictionary<string, int> itemAmounts = new Dictionary<string, int>();
    private Dictionary<string, ItemData> itemDataById = new Dictionary<string, ItemData>();
    private List<ItemData> knownItems = new List<ItemData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ApplyStartingItems();
    }

    private void ApplyStartingItems()
    {
        for (int i = 0; i < startingItems.Count; i++)
        {
            StartingItemStack stack = startingItems[i];
            if (stack == null || stack.itemData == null || stack.amount <= 0)
                continue;

            RegisterItem(stack.itemData);
            itemAmounts[stack.itemData.itemId] = stack.amount;
        }

        if (startingItems.Count > 0)
            OnInventoryChanged?.Invoke();
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
        RegisterItem(itemData);

        if (itemAmounts.ContainsKey(id))
        {
            itemAmounts[id] += amount;
        }
        else
        {
            itemAmounts.Add(id, amount);
        }

        OnInventoryChanged?.Invoke();

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

    public List<ItemData> GetItemsWithAmount()
    {
        List<ItemData> items = new List<ItemData>();

        for (int i = 0; i < knownItems.Count; i++)
        {
            ItemData itemData = knownItems[i];

            if (itemData != null && GetItemAmount(itemData) > 0)
                items.Add(itemData);
        }

        return items;
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

        OnInventoryChanged?.Invoke();

        Debug.Log($"Removed {amount} {itemData.itemName}. Total: {itemAmounts[id]}");

        return true;
    }

    public void ApplySavedItems(List<ItemAmountSave> savedItems, ItemData[] itemDatabase)
    {
        itemAmounts.Clear();
        itemDataById.Clear();
        knownItems.Clear();

        if (itemDatabase != null)
        {
            for (int i = 0; i < itemDatabase.Length; i++)
                RegisterItem(itemDatabase[i]);
        }

        if (savedItems != null)
        {
            for (int i = 0; i < savedItems.Count; i++)
            {
                ItemAmountSave savedItem = savedItems[i];
                if (savedItem == null || string.IsNullOrWhiteSpace(savedItem.itemId))
                    continue;

                itemAmounts[savedItem.itemId] = Mathf.Max(0, savedItem.amount);
            }
        }

        OnInventoryChanged?.Invoke();
    }

    public List<ItemAmountSave> ExportItemAmounts()
    {
        List<ItemAmountSave> result = new List<ItemAmountSave>();

        foreach (KeyValuePair<string, int> entry in itemAmounts)
        {
            if (string.IsNullOrWhiteSpace(entry.Key) || entry.Value <= 0)
                continue;

            result.Add(new ItemAmountSave(entry.Key, entry.Value));
        }

        return result;
    }

    private void RegisterItem(ItemData itemData)
    {
        if (itemData == null || string.IsNullOrEmpty(itemData.itemId))
            return;

        if (!itemDataById.ContainsKey(itemData.itemId))
            knownItems.Add(itemData);

        itemDataById[itemData.itemId] = itemData;
    }
}
