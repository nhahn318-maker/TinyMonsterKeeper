using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CookingPotController : MonoBehaviour, IPointerClickHandler {
    [Header("Visual")]
    [SerializeField] private SpriteRenderer potRenderer;
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Sprite cookingSprite;
    [SerializeField] private Sprite doneSprite;

    [Header("Effects")]
    [SerializeField] private GameObject cookingBubblesObject;
    [SerializeField] private GameObject readyBubbleObject;

    [Header("Cooking")]
    [SerializeField] private float cookDuration = 30f;

    private bool isCooking;
    private bool isDone;

    private void Awake()
    {
        if (potRenderer == null)
            potRenderer = GetComponent<SpriteRenderer>();

        SetEmptyVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
  /*      if (CameraDragPan2D.LastDragEndFrame == Time.frameCount)
            return;*/

        if (isCooking)
        {
            Debug.Log("Pot is cooking...");
            return;
        }

        if (isDone)
        {
            CollectCookResult();
            return;
        }

        if (CookingPotPanelUI.Instance != null)
        {
            CookingPotPanelUI.Instance.Show(this);
        }
        else
        {
            Debug.LogWarning("CookingPotPanelUI is missing!");
        }
    }

    public bool TryStartCooking(List<ItemData> ingredients)
    {
        if (isCooking || isDone)
            return false;

        if (ingredients == null || ingredients.Count != 3)
        {
            Debug.LogWarning("Need exactly 3 foods to cook!");
            return false;
        }

        if (!CanConsumeIngredients(ingredients))
        {
            Debug.LogWarning("Not enough food!");
            return false;
        }

        ConsumeIngredients(ingredients);
        StartCoroutine(CookingRoutine());

        return true;
    }

    private IEnumerator CookingRoutine()
    {
        isCooking = true;
        isDone = false;

        SetCookingVisual();

        yield return new WaitForSeconds(cookDuration);

        isCooking = false;
        isDone = true;

        SetDoneVisual();

        Debug.Log("Cooking done! Tap pot to attract visitor.");
    }

    private bool CanConsumeIngredients(List<ItemData> ingredients)
    {
        if (InventoryManager.Instance == null)
            return false;

        Dictionary<string, int> needAmounts = new Dictionary<string, int>();
        Dictionary<string, ItemData> itemById = new Dictionary<string, ItemData>();

        foreach (ItemData item in ingredients)
        {
            if (item == null)
                return false;

            if (!needAmounts.ContainsKey(item.itemId))
                needAmounts[item.itemId] = 0;

            needAmounts[item.itemId]++;
            itemById[item.itemId] = item;
        }

        foreach (var pair in needAmounts)
        {
            ItemData item = itemById[pair.Key];
            int haveAmount = InventoryManager.Instance.GetItemAmount(item);

            if (haveAmount < pair.Value)
                return false;
        }

        return true;
    }

    private void ConsumeIngredients(List<ItemData> ingredients)
    {
        Dictionary<string, int> needAmounts = new Dictionary<string, int>();
        Dictionary<string, ItemData> itemById = new Dictionary<string, ItemData>();

        foreach (ItemData item in ingredients)
        {
            if (!needAmounts.ContainsKey(item.itemId))
                needAmounts[item.itemId] = 0;

            needAmounts[item.itemId]++;
            itemById[item.itemId] = item;
        }

        foreach (var pair in needAmounts)
        {
            ItemData item = itemById[pair.Key];
            InventoryManager.Instance.RemoveItem(item, pair.Value);
        }
    }

    private void CollectCookResult()
    {
        isDone = false;

        SetEmptyVisual();

        // Sau này chỗ này sẽ gọi VisitorManager.
        Debug.Log("Visitor should appear here!");
    }

    private void SetEmptyVisual()
    {
        if (potRenderer != null && emptySprite != null)
            potRenderer.sprite = emptySprite;

        if (cookingBubblesObject != null)
            cookingBubblesObject.SetActive(false);

        if (readyBubbleObject != null)
            readyBubbleObject.SetActive(false);
    }

    private void SetCookingVisual()
    {
        if (potRenderer != null && cookingSprite != null)
            potRenderer.sprite = cookingSprite;

        if (cookingBubblesObject != null)
            cookingBubblesObject.SetActive(true);

        if (readyBubbleObject != null)
            readyBubbleObject.SetActive(false);
    }

    private void SetDoneVisual()
    {
        if (potRenderer != null)
        {
            if (doneSprite != null)
                potRenderer.sprite = doneSprite;
            else if (cookingSprite != null)
                potRenderer.sprite = cookingSprite;
        }

        if (cookingBubblesObject != null)
            cookingBubblesObject.SetActive(false);

        if (readyBubbleObject != null)
            readyBubbleObject.SetActive(true);
    }
}