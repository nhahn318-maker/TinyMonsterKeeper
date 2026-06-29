using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CookingIngredientSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler {
    [Header("Slot")]
    [SerializeField] private int slotIndex;

    [Header("UI")]
    [SerializeField] private Image foodIcon;
    [SerializeField] private GameObject plusIcon;

    private CookingPotPanelUI owner;
    private bool hasFood;

    public void Init(CookingPotPanelUI panel, int index)
    {
        owner = panel;
        slotIndex = index;
    }

    public void SetFood(ItemData itemData)
    {
        hasFood = itemData != null;

        if (foodIcon != null)
        {
            foodIcon.enabled = itemData != null;
            foodIcon.sprite = itemData != null ? itemData.icon : null;
        }

        if (plusIcon != null)
            plusIcon.SetActive(itemData == null);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        DraggableFoodItemUI draggedFood =
            eventData.pointerDrag.GetComponent<DraggableFoodItemUI>();

        if (draggedFood == null)
            return;

        if (owner == null)
            return;

        owner.TryPlaceFoodInSlot(slotIndex, draggedFood.ItemData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!hasFood)
            return;

        if (owner != null)
            owner.RemoveFoodAtSlot(slotIndex);
    }
}