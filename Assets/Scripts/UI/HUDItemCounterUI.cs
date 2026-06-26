using TMPro;
using UnityEngine;

public class HUDItemCounterUI : MonoBehaviour {
    [SerializeField] private ItemData itemData;
    [SerializeField] private TMP_Text countText;

    private int lastAmount = -1;

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (InventoryManager.Instance == null) return;
        if (itemData == null) return;
        if (countText == null) return;

        int amount = InventoryManager.Instance.GetItemAmount(itemData);

        if (amount == lastAmount) return;

        lastAmount = amount;
        countText.text = amount.ToString();
    }
}