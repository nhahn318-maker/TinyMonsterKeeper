using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CookingPotPanelUI : MonoBehaviour {
    public static CookingPotPanelUI Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Drag")]
    [SerializeField] private RectTransform dragLayer;

    [Header("Slots")]
    [SerializeField] private CookingIngredientSlotUI[] slots;

    [Header("Food Items")]
    [SerializeField] private DraggableFoodItemUI[] foodItems;
    [SerializeField] private Transform foodItemContainer;

    [Header("Buttons")]
    [SerializeField] private Button cookButton;

    private readonly ItemData[] selectedFoods = new ItemData[3];
    private CookingPotController currentPot;
    private Canvas rootCanvas;
    private RectTransform panelRootRect;
    private RectTransform[] panelHitAreas;
    private int openedFrame = -1;

    public RectTransform DragLayer => dragLayer;
    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        Instance = this;
        rootCanvas = GetComponentInParent<Canvas>();
        panelRootRect = panelRoot != null ? panelRoot.GetComponent<RectTransform>() : null;
        RefreshPanelHitAreas();
        ResolveFoodItemSlots();

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].Init(this, i);
        }

        Hide();
    }

    private void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf)
            return;

        if (openedFrame == Time.frameCount)
            return;

        if (Input.GetMouseButtonDown(0))
            HideIfOutsidePanel(Input.mousePosition);

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began)
                HideIfOutsidePanel(touch.position);
        }
    }

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
    }

    public void Show(CookingPotController pot)
    {
        currentPot = pot;
        ClearSelectedFoods();

        if (panelRoot != null)
            panelRoot.SetActive(true);

        openedFrame = Time.frameCount;

        RebuildFoodItems();
        RefreshPanelHitAreas();
        RefreshUI();
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void HideIfOutsidePanel(Vector2 screenPosition)
    {
        if (IsPointerInsidePanel(screenPosition))
            return;

        Hide();
    }

    private bool IsPointerInsidePanel(Vector2 screenPosition)
    {
        if (panelHitAreas == null || panelHitAreas.Length == 0)
            return false;

        Camera eventCamera = null;
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = rootCanvas.worldCamera;

        for (int i = 0; i < panelHitAreas.Length; i++)
        {
            RectTransform hitArea = panelHitAreas[i];

            if (hitArea == null || !hitArea.gameObject.activeInHierarchy)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(hitArea, screenPosition, eventCamera))
                return true;
        }

        return false;
    }

    private void RefreshPanelHitAreas()
    {
        panelHitAreas = panelRootRect != null
            ? panelRootRect.GetComponentsInChildren<RectTransform>(true)
            : null;
    }

    public void OnClickCook()
    {
        if (currentPot == null)
            return;

        if (!CanCook())
            return;

        List<ItemData> ingredients = new List<ItemData>();

        for (int i = 0; i < selectedFoods.Length; i++)
            ingredients.Add(selectedFoods[i]);

        bool started = currentPot.TryStartCooking(ingredients);

        if (started)
        {
            ClearSelectedFoods();
            Hide();
        }
    }

    public bool TryPlaceFoodInFirstEmptySlot(ItemData itemData)
    {
        for (int i = 0; i < selectedFoods.Length; i++)
        {
            if (selectedFoods[i] == null)
                return TryPlaceFoodInSlot(i, itemData);
        }

        Debug.Log("No empty cooking slot.");
        return false;
    }

    public bool TryPlaceFoodInSlot(int slotIndex, ItemData itemData)
    {
        if (slotIndex < 0 || slotIndex >= selectedFoods.Length)
            return false;

        if (itemData == null)
            return false;

        if (selectedFoods[slotIndex] != null)
            return false;

        if (GetAvailableAmount(itemData) <= 0)
            return false;

        selectedFoods[slotIndex] = itemData;
        RefreshUI();

        return true;
    }

    public void RemoveFoodAtSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= selectedFoods.Length)
            return;

        selectedFoods[slotIndex] = null;
        RefreshUI();
    }

    public int GetSelectedAmount(ItemData itemData)
    {
        if (itemData == null)
            return 0;

        int count = 0;

        for (int i = 0; i < selectedFoods.Length; i++)
        {
            if (selectedFoods[i] != null &&
                selectedFoods[i].itemId == itemData.itemId)
            {
                count++;
            }
        }

        return count;
    }

    public int GetAvailableAmount(ItemData itemData)
    {
        if (itemData == null)
            return 0;

        if (InventoryManager.Instance == null)
            return 0;

        int total = InventoryManager.Instance.GetItemAmount(itemData);
        int selected = GetSelectedAmount(itemData);

        return Mathf.Max(0, total - selected);
    }

    private bool CanCook()
    {
        for (int i = 0; i < selectedFoods.Length; i++)
        {
            if (selectedFoods[i] == null)
                return false;
        }

        return true;
    }

    private void ClearSelectedFoods()
    {
        for (int i = 0; i < selectedFoods.Length; i++)
            selectedFoods[i] = null;
    }

    private void RefreshUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].SetFood(selectedFoods[i]);
        }

        if (foodItems != null)
        {
            for (int i = 0; i < foodItems.Length; i++)
            {
                DraggableFoodItemUI foodItem = foodItems[i];

                if (foodItem != null)
                    foodItem.Refresh();
            }
        }

        if (cookButton != null)
            cookButton.interactable = CanCook();
    }

    private void HandleInventoryChanged()
    {
        if (panelRoot == null || !panelRoot.activeSelf)
            return;

        RebuildFoodItems();
        RefreshUI();
    }

    private void ResolveFoodItemSlots()
    {
        if (foodItemContainer == null && foodItems != null && foodItems.Length > 0 && foodItems[0] != null)
            foodItemContainer = foodItems[0].transform.parent;

        if (foodItemContainer != null)
        {
            DraggableFoodItemUI[] discoveredFoodItems = foodItemContainer.GetComponentsInChildren<DraggableFoodItemUI>(true);

            if (foodItems == null || discoveredFoodItems.Length > foodItems.Length)
                foodItems = discoveredFoodItems;
        }

        if (foodItems == null)
            return;

        for (int i = 0; i < foodItems.Length; i++)
        {
            if (foodItems[i] == null)
                continue;

            foodItems[i].Init(this);
            foodItems[i].SetItemData(null);
            foodItems[i].gameObject.SetActive(true);
        }
    }

    private void RebuildFoodItems()
    {
        if (InventoryManager.Instance == null || foodItems == null || foodItems.Length == 0)
            return;

        List<ItemData> inventoryItems = InventoryManager.Instance.GetItemsWithAmount();

        for (int i = 0; i < foodItems.Length; i++)
        {
            DraggableFoodItemUI foodItem = foodItems[i];

            if (foodItem == null)
                continue;

            ItemData itemData = i < inventoryItems.Count ? inventoryItems[i] : null;
            foodItem.SetItemData(itemData);
        }
    }
}
