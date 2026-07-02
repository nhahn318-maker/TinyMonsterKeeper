using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableFoodItemUI : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerClickHandler {
    [Header("Data")]
    [SerializeField] private ItemData itemData;

    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Button button;

    [Header("Drag")]
    [SerializeField] private Vector2 dragIconSize = new Vector2(64f, 64f);

    private CookingPotPanelUI owner;
    private GameObject dragIconObject;
    private RectTransform dragIconRect;
    private bool isDragging;

    public ItemData ItemData => itemData;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        ApplyItemVisuals();
    }

    public void Init(CookingPotPanelUI panel)
    {
        owner = panel;
        Refresh();
    }

    public void SetItemData(ItemData newItemData)
    {
        itemData = newItemData;

        ApplyItemVisuals();

        Refresh();
    }

    public void Refresh()
    {
        if (itemData == null)
        {
            if (countText != null)
                countText.text = string.Empty;

            if (button != null)
                button.interactable = false;

            return;
        }

        int amount = 0;

        if (owner != null)
            amount = owner.GetAvailableAmount(itemData);
        else if (InventoryManager.Instance != null)
            amount = InventoryManager.Instance.GetItemAmount(itemData);

        if (countText != null)
            countText.text = "x" + amount;

        if (button != null)
            button.interactable = amount > 0;
    }

    private void ApplyItemVisuals()
    {
        bool hasItem = itemData != null;

        if (iconImage != null)
        {
            iconImage.sprite = hasItem ? itemData.icon : null;
            iconImage.enabled = hasItem;

            if (hasItem && itemData.HasCustomCookingIconSize())
                iconImage.rectTransform.sizeDelta = itemData.cookingIconSize;
        }

        if (countText != null)
            countText.enabled = hasItem;

        if (button != null && !hasItem)
            button.interactable = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (owner == null)
            return;

        if (itemData == null)
            return;

        if (owner.GetAvailableAmount(itemData) <= 0)
            return;

        isDragging = true;
        CreateDragIcon(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        if (dragIconRect != null)
            dragIconRect.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        if (dragIconObject != null)
            Destroy(dragIconObject);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging)
            return;

        if (owner == null)
            return;

        if (itemData == null)
            return;

        owner.TryPlaceFoodInFirstEmptySlot(itemData);
    }

    private void CreateDragIcon(Vector2 screenPosition)
    {
        if (owner.DragLayer == null)
            return;

        dragIconObject = new GameObject("DraggingFoodIcon");
        dragIconObject.transform.SetParent(owner.DragLayer, false);

        Image dragImage = dragIconObject.AddComponent<Image>();
        dragImage.sprite = itemData.icon;
        dragImage.raycastTarget = false;

        dragIconRect = dragIconObject.GetComponent<RectTransform>();
        dragIconRect.sizeDelta = dragIconSize;
        dragIconRect.position = screenPosition;
    }
}
