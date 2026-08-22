using TMPro;
using UnityEngine;

/// <summary>
/// A harvest node whose world sprite remains unchanged while it regenerates.
/// </summary>
public sealed class StaticTimedHarvestNodeController : MonoBehaviour
{
    [Header("Growth")]
    [SerializeField] private float respawnDuration = 30f;
    [SerializeField] private string countdownFormat = "{0}s";

    [Header("Harvest")]
    [SerializeField] private GameObject dropPrefab;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private Vector2 fallbackDropOffset = new Vector2(0f, -0.2f);
    [SerializeField] private ItemData itemData;
    [SerializeField] private int amount = 1;

    [Header("References")]
    [SerializeField] private Collider2D harvestCollider;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TMP_Text growthCountdownText;
    [SerializeField] private bool createCountdownTextIfMissing = true;
    [SerializeField] private Vector3 countdownLocalOffset = new Vector3(0f, 0.55f, 0f);

    [Header("Ready Bubble")]
    [SerializeField] private GameObject readyBubbleObject;
    [SerializeField] private Sprite readyBubbleSprite;
    [SerializeField] private bool createReadyBubbleIfMissing = true;
    [SerializeField] private Vector3 readyBubbleLocalOffset = new Vector3(0f, 0.55f, 0f);
    [SerializeField] private float readyBubbleIconScale = 0.45f;

    [Header("Production Animation")]
    [SerializeField] private bool animateWhileGrowing;
    [SerializeField] private Animator productionAnimator;
    [SerializeField] private Sprite idleSprite;

    [Header("Click Priority")]
    [SerializeField] private LayerMask pickupLayer;

    private long readyAtUnix;
    private bool isReady;
    private MeshRenderer countdownRenderer;
    private SpriteRenderer readyBubbleRenderer;
    private SpriteRenderer readyBubbleIconRenderer;

    private void Awake()
    {
        if (harvestCollider == null)
            harvestCollider = GetComponent<Collider2D>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (productionAnimator == null)
            productionAnimator = GetComponent<Animator>();

        if (idleSprite == null && spriteRenderer != null)
            idleSprite = spriteRenderer.sprite;

        if (growthCountdownText == null)
            growthCountdownText = FindChildComponent<TMP_Text>("GrowthTimerText");

        if (growthCountdownText == null && createCountdownTextIfMissing)
            growthCountdownText = CreateCountdownText();

        if (growthCountdownText != null)
            countdownRenderer = growthCountdownText.GetComponent<MeshRenderer>();

        if (readyBubbleObject == null)
        {
            Transform existingBubble = FindChildTransform("ReadyBubble");
            readyBubbleObject = existingBubble != null ? existingBubble.gameObject : null;
        }

        if (readyBubbleObject == null && createReadyBubbleIfMissing)
            readyBubbleObject = CreateReadyBubble();

        CacheReadyBubbleRenderers();

        SetReady(false);
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i].name == childName)
                return components[i];
        }

        return null;
    }

    private Transform FindChildTransform(string childName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != transform && children[i].name == childName)
                return children[i];
        }

        return null;
    }

    private void Update()
    {
        HandleInput();

        if (!isReady)
        {
            if (TimedSaveUtility.NowUnix >= readyAtUnix)
                SetReady(true);
            else
                RefreshCountdown();
        }

        RefreshCountdownSorting();
    }

    private void HandleInput()
    {
        if (!isReady || BookOpenUI.IsOpen)
            return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
            TryHarvest(Input.mousePosition);
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            TryHarvest(Input.GetTouch(0).position);
#endif
    }

    private void TryHarvest(Vector2 screenPosition)
    {
        if (mainCamera == null || harvestCollider == null)
            return;

        Vector2 worldPoint = mainCamera.ScreenToWorldPoint(screenPosition);
        if (pickupLayer.value != 0 && Physics2D.OverlapPoint(worldPoint, pickupLayer) != null)
            return;

        if (!harvestCollider.OverlapPoint(worldPoint))
            return;

        SpawnDrop();
        SetReady(false);
    }

    private void SpawnDrop()
    {
        if (dropPrefab == null)
        {
            Debug.LogWarning($"{name}: Drop prefab is missing.");
            return;
        }

        Vector3 spawnPosition = dropPoint != null
            ? dropPoint.position
            : transform.position + (Vector3)fallbackDropOffset;

        GameObject dropObject = Instantiate(dropPrefab, spawnPosition, Quaternion.identity);
        BerryDropController drop = dropObject.GetComponent<BerryDropController>();
        if (drop != null)
            drop.Init(itemData, amount);
    }

    private void SetReady(bool ready, bool resetTimer = true)
    {
        isReady = ready;
        if (ready)
            readyAtUnix = 0L;
        else if (resetTimer)
            readyAtUnix = TimedSaveUtility.SecondsFromNow(respawnDuration);
        if (growthCountdownText != null)
            growthCountdownText.gameObject.SetActive(!ready);

        if (readyBubbleObject != null)
            readyBubbleObject.SetActive(ready);

        RefreshProductionAnimation(ready);

        RefreshCountdown();
    }

    private void RefreshProductionAnimation(bool ready)
    {
        if (!animateWhileGrowing || productionAnimator == null)
            return;

        productionAnimator.enabled = !ready;
        if (!ready)
        {
            productionAnimator.Play(0, 0, 0f);
            return;
        }

        if (spriteRenderer != null && idleSprite != null)
            spriteRenderer.sprite = idleSprite;
    }

    private void RefreshCountdown()
    {
        if (growthCountdownText == null || isReady)
            return;

        int seconds = Mathf.Max(0, Mathf.CeilToInt(readyAtUnix - TimedSaveUtility.NowUnix));
        growthCountdownText.text = string.Format(countdownFormat, seconds);
    }

    private TMP_Text CreateCountdownText()
    {
        GameObject textObject = new GameObject("GrowthTimerText", typeof(TextMeshPro));
        textObject.transform.SetParent(transform, false);
        textObject.transform.localPosition = countdownLocalOffset;

        TextMeshPro text = textObject.GetComponent<TextMeshPro>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 2f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        MeshRenderer textRenderer = text.GetComponent<MeshRenderer>();
        if (textRenderer != null)
            textRenderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 1;

        return text;
    }

    private GameObject CreateReadyBubble()
    {
        if (readyBubbleSprite == null)
            return null;

        GameObject bubbleObject = new GameObject("ReadyBubble", typeof(SpriteRenderer));
        bubbleObject.transform.SetParent(transform, false);
        bubbleObject.transform.localPosition = readyBubbleLocalOffset;

        SpriteRenderer bubbleRenderer = bubbleObject.GetComponent<SpriteRenderer>();
        bubbleRenderer.sprite = readyBubbleSprite;

        GameObject iconObject = new GameObject("ItemIcon", typeof(SpriteRenderer));
        iconObject.transform.SetParent(bubbleObject.transform, false);
        iconObject.transform.localScale = Vector3.one * readyBubbleIconScale;

        SpriteRenderer iconRenderer = iconObject.GetComponent<SpriteRenderer>();
        iconRenderer.sprite = itemData != null ? itemData.icon : null;

        return bubbleObject;
    }

    private void CacheReadyBubbleRenderers()
    {
        if (readyBubbleObject == null)
            return;

        readyBubbleRenderer = readyBubbleObject.GetComponent<SpriteRenderer>();
        if (readyBubbleObject.transform.childCount > 0)
            readyBubbleIconRenderer = readyBubbleObject.transform.GetChild(0).GetComponent<SpriteRenderer>();

        if (readyBubbleIconRenderer != null && itemData != null)
            readyBubbleIconRenderer.sprite = itemData.icon;
    }

    private void RefreshCountdownSorting()
    {
        if (countdownRenderer == null || spriteRenderer == null)
            return;

        countdownRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;

        if (readyBubbleRenderer != null)
            readyBubbleRenderer.sortingOrder = spriteRenderer.sortingOrder + 2;

        if (readyBubbleIconRenderer != null)
            readyBubbleIconRenderer.sortingOrder = spriteRenderer.sortingOrder + 3;
    }

    public ResourceNodeTimerSave ExportTimedState()
    {
        return new ResourceNodeTimerSave
        {
            nodeId = TimedSaveUtility.GetStableSceneKey(this, "staticHarvest"),
            isReady = isReady,
            readyAtUnix = readyAtUnix
        };
    }

    public void ApplyTimedState(ResourceNodeTimerSave state)
    {
        if (state == null)
            return;

        readyAtUnix = state.readyAtUnix;
        bool ready = state.isReady || (readyAtUnix > 0L && TimedSaveUtility.NowUnix >= readyAtUnix);
        SetReady(ready, false);
    }
}
