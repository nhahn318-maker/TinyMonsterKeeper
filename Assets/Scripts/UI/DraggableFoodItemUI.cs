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

        if (iconImage != null && itemData != null)
            iconImage.sprite = itemData.icon;
    }

    public void Init(CookingPotPanelUI panel)
    {
        owner = panel;
        Refresh();
    }

    public void Refresh()
    {
        if (itemData == null)
            return;

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