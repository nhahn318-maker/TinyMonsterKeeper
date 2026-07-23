using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CookingPotController : MonoBehaviour, IPointerClickHandler {
    [Header("Visual")]
    [SerializeField] private SpriteRenderer potRenderer;
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Sprite cookingSprite;
    [SerializeField] private Sprite[] cookingAnimationFrames;
    [SerializeField] private Sprite doneSprite;
    [SerializeField] private float cookingAnimationFps = 6f;

    [Header("Effects")]
    [SerializeField] private GameObject cookingBubblesObject;
    [SerializeField] private GameObject readyBubbleObject;
    [SerializeField] private GameObject progressBarObject;
    [SerializeField] private Transform progressFillTransform;

    [Header("Attraction")]
    [SerializeField] private AromaAnimation aromaAnimation;
    [SerializeField] private float monsterSpawnDelay = 0.45f;
    [SerializeField] private Transform monsterSpawnPoint;
    [SerializeField] private Vector3 monsterSpawnOffset = new Vector3(1.2f, -0.6f, 0f);
    [SerializeField] private Collider2D monsterGardenBounds;

    [Header("Camera Focus")]
    [SerializeField] private Camera focusCamera;
    [SerializeField] private float monsterFocusDuration = 5f;
    [SerializeField] private float monsterFocusOrthographicSize = 2.4f;
    [SerializeField] private Vector3 monsterFocusOffset = new Vector3(0f, 0.35f, 0f);
    [SerializeField] private float cameraFocusTransitionDuration = 0.45f;

    [Header("Notice")]
    [SerializeField] private GameObject noticeLayer;
    [SerializeField] private TextMeshProUGUI noticeText;
    [SerializeField] private GameTextDatabase textDatabase;
    [SerializeField] private GameTextKey monsterNoticeKey = GameTextKey.MonsterJoined;
    [SerializeField] private GameTextKey monsterAlreadyInGardenKey = GameTextKey.MonsterAlreadyInGarden;
    [SerializeField] private GameTextKey monsterStarIncreasedKey = GameTextKey.MonsterStarIncreased;
    [SerializeField] private string monsterNoticeFormat = "{0} joined your garden!";
    [SerializeField] private string monsterAlreadyInGardenFallback = "{0} is already in your garden!";
    [SerializeField] private string monsterStarIncreasedFallback = "{0} gained a {1} star!";

    [Header("Star Gain Feedback")]
    [SerializeField] private Sprite bronzeStarSprite;
    [SerializeField] private Sprite silverStarSprite;
    [SerializeField] private Sprite goldStarSprite;
    [SerializeField] private Vector3 starPopupOffset = new Vector3(0f, 0.85f, 0f);
    [SerializeField] private float starPopupDuration = 1.25f;
    [SerializeField] private float starPopupRiseDistance = 0.35f;
    [SerializeField] private float starPopupStartScale = 0.55f;
    [SerializeField] private float starPopupEndScale = 1f;
    [SerializeField] private int starPopupSortingOrderOffset = 20;

    [Header("Recipes")]
    [SerializeField] private CookingRecipeData[] recipes;

    [Header("Click")]
    [SerializeField] private Collider2D clickCollider;

    private bool isCooking;
    private bool isDone;
    private Vector3 progressFillInitialScale;
    private Vector3 progressFillInitialLocalPosition;
    private float currentCookDuration;
    private bool isAttracting;
    private Coroutine cameraFocusRoutine;
    private Coroutine noticeRoutine;
    private CookingRecipeData activeRecipe;
    private int lastHandledClickFrame = -1;
    private Button[] noticeButtons;

    private void Awake()
    {
        if (potRenderer == null)
            potRenderer = GetComponent<SpriteRenderer>();

        if (clickCollider == null)
            clickCollider = GetComponent<Collider2D>();

        if (progressFillTransform != null)
        {
            progressFillInitialScale = progressFillTransform.localScale;
            progressFillInitialLocalPosition = progressFillTransform.localPosition;
        }

        if (aromaAnimation == null)
            aromaAnimation = GetComponentInChildren<AromaAnimation>(true);

        if (focusCamera == null)
            focusCamera = Camera.main;

        CacheNoticeButtons();
        HideMonsterNotice();
        SetEmptyVisual();
    }

    private void Update()
    {
        HandleDirectWorldClick();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HandlePotClick();
    }

    private void HandleDirectWorldClick()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
            TryHandleDirectWorldClick(Input.mousePosition);
#else
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
                TryHandleDirectWorldClick(touch.position);
        }
#endif
    }

    private void TryHandleDirectWorldClick(Vector2 screenPosition)
    {
        if (lastHandledClickFrame == Time.frameCount)
            return;

        if (BookOpenUI.IsOpen)
            return;

        if (CookingPotPanelUI.Instance != null && CookingPotPanelUI.Instance.IsOpen)
            return;

        if (focusCamera == null || clickCollider == null)
            return;

        Vector3 worldPosition = focusCamera.ScreenToWorldPoint(screenPosition);
        Vector2 clickPoint = new Vector2(worldPosition.x, worldPosition.y);

        if (!clickCollider.OverlapPoint(clickPoint))
            return;

        HandlePotClick();
    }

    private void HandlePotClick()
    {
        if (lastHandledClickFrame == Time.frameCount)
            return;

        lastHandledClickFrame = Time.frameCount;

        if (isCooking)
        {
            Debug.Log("Pot is cooking...");
            return;
        }

        if (isAttracting)
        {
            Debug.Log("Aroma is attracting a monster...");
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

        if (ingredients == null || ingredients.Count == 0)
        {
            Debug.LogWarning("Need foods to cook!");
            return false;
        }

        if (!TryResolveRecipe(ingredients, out CookingRecipeData recipe))
        {
            Debug.LogWarning($"No cooking recipe matched these ingredients. Ingredients: {FormatIngredients(ingredients)}. Recipes: {FormatKnownRecipes()}");
            return false;
        }

        if (!CanConsumeIngredients(ingredients))
        {
            Debug.LogWarning("Not enough food!");
            return false;
        }

        ConsumeIngredients(ingredients);
        StartCoroutine(CookingRoutine(recipe));

        return true;
    }

    private IEnumerator CookingRoutine(CookingRecipeData recipe)
    {
        isCooking = true;
        isDone = false;
        activeRecipe = recipe;
        currentCookDuration = Mathf.Max(0.1f, recipe.cookDuration);

        SetCookingVisual();

        float elapsed = 0f;
        UpdateProgressBar(0f);

        while (elapsed < currentCookDuration)
        {
            elapsed += Time.deltaTime;
            UpdateCookingAnimation(elapsed);
            UpdateProgressBar(elapsed / currentCookDuration);
            yield return null;
        }

        isCooking = false;
        isDone = true;
        UpdateProgressBar(1f);

        SetDoneVisual();

        Debug.Log($"{recipe.resultName} is done! Tap pot to attract visitor.");
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
        currentCookDuration = 0f;

        SetEmptyVisual();
        StartCoroutine(AttractMonsterRoutine());
    }

    private IEnumerator AttractMonsterRoutine()
    {
        isAttracting = true;

        if (aromaAnimation != null)
            aromaAnimation.Play();

        yield return new WaitForSeconds(monsterSpawnDelay);

        SpawnRandomMonster();

        float remainingAromaTime = aromaAnimation != null ? Mathf.Max(0f, aromaAnimation.Duration - monsterSpawnDelay) : 0f;
        if (remainingAromaTime > 0f)
            yield return new WaitForSeconds(remainingAromaTime);

        activeRecipe = null;
        isAttracting = false;
    }

    private void SpawnRandomMonster()
    {
        if (activeRecipe == null)
        {
            Debug.LogWarning("No active cooking recipe to attract monster.");
            return;
        }

        GameObject prefab = activeRecipe.GetRandomAttractedMonsterPrefab();
        if (prefab == null)
        {
            Debug.LogWarning($"No attracted monster prefab assigned for recipe: {activeRecipe.resultName}");
            return;
        }

        if (IsDuplicateMonsterResult(prefab))
        {
            string existingName = GetPrefabMonsterName(prefab);
            MonsterData existingData = GetPrefabMonsterData(prefab);
            int unlockCount = 0;
            if (existingData != null && MonsterCollectionManager.Unlock(existingData))
            {
                unlockCount = MonsterCollectionManager.GetUnlockCount(existingData);
                Debug.Log($"Increased monster card stars: {existingName}");
            }

            TinyMonsterController existingMonster = FindExistingMonster(existingData, prefab);
            string tierName = GetStarTierName(unlockCount);
            string notice = GetText(monsterStarIncreasedKey, monsterStarIncreasedFallback, existingName, tierName);

            if (existingMonster != null)
            {
                ShowStarGainFeedback(existingMonster.transform, unlockCount);
                FocusCameraOnTargetWithNotice(existingMonster.transform, notice);
            }
            else
            {
                ShowTemporaryNotice(GetText(monsterAlreadyInGardenKey, monsterAlreadyInGardenFallback, existingName), 2f);
            }

            Debug.Log($"{existingName} is already in garden. Spawn skipped.");
            return;
        }

        Vector3 spawnPosition = ResolveMonsterSpawnPosition();

        GameObject monster = Instantiate(prefab, spawnPosition, Quaternion.identity);
        ConfigureSpawnedMonster(monster);
        UnlockSpawnedMonster(monster);
        SaveSpawnedMonster(monster);
        string monsterName = GetMonsterName(monster);
        FocusCameraOnMonster(monster.transform, monsterName);

        Debug.Log($"Spawned attracted monster: {monster.name}");
    }

    private TinyMonsterController FindExistingMonster(MonsterData monsterData, GameObject prefab)
    {
        string targetId = monsterData != null
            ? MonsterCollectionManager.GetMonsterId(monsterData)
            : string.Empty;

        TinyMonsterController prefabController = prefab != null ? prefab.GetComponent<TinyMonsterController>() : null;
        if (string.IsNullOrEmpty(targetId) && prefabController != null && prefabController.Data != null)
            targetId = MonsterCollectionManager.GetMonsterId(prefabController.Data);

        TinyMonsterController[] monsters = FindObjectsOfType<TinyMonsterController>();
        for (int i = 0; i < monsters.Length; i++)
        {
            TinyMonsterController monster = monsters[i];
            if (monster == null)
                continue;

            if (!string.IsNullOrEmpty(targetId) && monster.Data != null && MonsterCollectionManager.GetMonsterId(monster.Data) == targetId)
                return monster;

            if (prefab != null && monster.name.Replace("(Clone)", string.Empty).Trim() == prefab.name)
                return monster;
        }

        return null;
    }

    private bool IsDuplicateMonsterResult(GameObject prefab)
    {
        MonsterData monsterData = GetPrefabMonsterData(prefab);
        if (monsterData != null && MonsterCollectionManager.IsUnlocked(monsterData))
            return true;

        return IsMonsterAlreadyInGarden(prefab);
    }

    private bool IsMonsterAlreadyInGarden(GameObject prefab)
    {
        TinyMonsterController prefabController = prefab.GetComponent<TinyMonsterController>();
        string prefabId = prefabController != null && prefabController.Data != null
            ? prefabController.Data.id
            : prefab.name;

        TinyMonsterController[] monsters = FindObjectsOfType<TinyMonsterController>();
        for (int i = 0; i < monsters.Length; i++)
        {
            TinyMonsterController monster = monsters[i];
            if (monster == null)
                continue;

            if (prefabController != null && prefabController.Data != null)
            {
                if (monster.Data != null && monster.Data.id == prefabId)
                    return true;
            }
            else if (monster.name.Replace("(Clone)", string.Empty).Trim() == prefabId)
            {
                return true;
            }
        }

        return false;
    }

    private string GetPrefabMonsterName(GameObject prefab)
    {
        if (prefab == null)
            return "Monster";

        TinyMonsterController controller = prefab.GetComponent<TinyMonsterController>();
        if (controller != null)
            return controller.MonsterName;

        return prefab.name;
    }

    private MonsterData GetPrefabMonsterData(GameObject prefab)
    {
        if (prefab == null)
            return null;

        TinyMonsterController controller = prefab.GetComponent<TinyMonsterController>();
        return controller != null ? controller.Data : null;
    }

    private void ConfigureSpawnedMonster(GameObject monster)
    {
        if (monster == null)
            return;

        TinyMonsterNavRoam navRoam = monster.GetComponent<TinyMonsterNavRoam>();
        if (navRoam != null && monsterGardenBounds != null)
            navRoam.SetGardenBounds(monsterGardenBounds);
    }

    private void UnlockSpawnedMonster(GameObject monster)
    {
        if (monster == null)
            return;

        TinyMonsterController controller = monster.GetComponent<TinyMonsterController>();
        if (controller == null || controller.Data == null)
            return;

        if (MonsterCollectionManager.Unlock(controller.Data))
            Debug.Log($"Unlocked monster card: {controller.MonsterName}");
    }

    private void SaveSpawnedMonster(GameObject monster)
    {
        if (monster == null || GardenMonsterSaveManager.Instance == null)
            return;

        TinyMonsterController controller = monster.GetComponent<TinyMonsterController>();
        if (controller == null || controller.Data == null)
            return;

        GardenMonsterSaveManager.Instance.SaveMonsterInGarden(controller);
    }

    private string GetMonsterName(GameObject monster)
    {
        if (monster == null)
            return "Monster";

        TinyMonsterController controller = monster.GetComponent<TinyMonsterController>();
        if (controller != null)
            return controller.MonsterName;

        return monster.name.Replace("(Clone)", string.Empty).Trim();
    }

    private void FocusCameraOnMonster(Transform target, string monsterName)
    {
        FocusCameraOnTargetWithNotice(target, GetText(monsterNoticeKey, monsterNoticeFormat, monsterName));
    }

    private void FocusCameraOnTargetWithNotice(Transform target, string noticeMessage)
    {
        if (focusCamera == null || target == null)
            return;

        if (cameraFocusRoutine != null)
        {
            StopCoroutine(cameraFocusRoutine);
            HideMonsterNotice();
        }

        cameraFocusRoutine = StartCoroutine(FocusCameraRoutine(target, noticeMessage));
    }

    private IEnumerator FocusCameraRoutine(Transform target, string noticeMessage)
    {
        CameraMapDragController cameraDrag = focusCamera.GetComponent<CameraMapDragController>();
        if (cameraDrag != null)
            cameraDrag.SetInputLocked(true);

        Vector3 originalPosition = focusCamera.transform.position;
        float originalOrthographicSize = focusCamera.orthographicSize;

        Vector3 focusPosition = target.position + monsterFocusOffset;
        focusPosition.z = originalPosition.z;

        ShowNoticeMessage(noticeMessage);
        yield return MoveCameraRoutine(originalPosition, focusPosition, originalOrthographicSize, monsterFocusOrthographicSize);
        yield return new WaitForSeconds(monsterFocusDuration);
        yield return MoveCameraRoutine(focusCamera.transform.position, originalPosition, focusCamera.orthographicSize, originalOrthographicSize);
        HideMonsterNotice();

        if (cameraDrag != null)
        {
            cameraDrag.SnapInsideBounds();
            cameraDrag.SetInputLocked(false);
        }

        cameraFocusRoutine = null;
    }

    private void ShowMonsterNotice(string monsterName)
    {
        ShowNoticeMessage(GetText(monsterNoticeKey, monsterNoticeFormat, monsterName));
    }

    private void ShowStarGainFeedback(Transform target, int unlockCount)
    {
        Sprite starSprite = GetStarSprite(unlockCount);
        if (target == null || starSprite == null)
            return;

        GameObject popup = new GameObject("StarGainPopup");
        popup.transform.position = target.position + starPopupOffset;
        popup.transform.localScale = Vector3.one * Mathf.Max(0.01f, starPopupStartScale);

        SpriteRenderer spriteRenderer = popup.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = starSprite;
        spriteRenderer.sortingOrder = GetPopupSortingOrder(target);

        StartCoroutine(StarPopupRoutine(popup, spriteRenderer));
    }

    private IEnumerator StarPopupRoutine(GameObject popup, SpriteRenderer spriteRenderer)
    {
        if (popup == null || spriteRenderer == null)
            yield break;

        Vector3 startPosition = popup.transform.position;
        Vector3 endPosition = startPosition + Vector3.up * Mathf.Max(0f, starPopupRiseDistance);
        float duration = Mathf.Max(0.01f, starPopupDuration);
        float elapsed = 0f;
        Color startColor = spriteRenderer.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);

            popup.transform.position = Vector3.Lerp(startPosition, endPosition, eased);
            popup.transform.localScale = Vector3.one * Mathf.Lerp(starPopupStartScale, starPopupEndScale, eased);

            Color color = startColor;
            color.a = Mathf.Lerp(1f, 0f, Mathf.Clamp01((t - 0.65f) / 0.35f));
            spriteRenderer.color = color;

            yield return null;
        }

        Destroy(popup);
    }

    private int GetPopupSortingOrder(Transform target)
    {
        int sortingOrder = starPopupSortingOrderOffset;
        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                sortingOrder = Mathf.Max(sortingOrder, renderers[i].sortingOrder + starPopupSortingOrderOffset);
        }

        return sortingOrder;
    }

    private Sprite GetStarSprite(int unlockCount)
    {
        if (unlockCount >= 7)
            return goldStarSprite != null ? goldStarSprite : silverStarSprite;

        if (unlockCount >= 4)
            return silverStarSprite != null ? silverStarSprite : bronzeStarSprite;

        return bronzeStarSprite;
    }

    private string GetStarTierName(int unlockCount)
    {
        if (unlockCount >= 7)
            return "gold";

        if (unlockCount >= 4)
            return "silver";

        return "bronze";
    }

    private void ShowNoticeMessage(string message)
    {
        if (noticeText != null)
            noticeText.text = message;

        SetNoticeButtonsVisible(false);

        if (noticeLayer != null)
            noticeLayer.SetActive(true);
    }

    private void ShowTemporaryNotice(string message, float duration)
    {
        if (noticeRoutine != null)
            StopCoroutine(noticeRoutine);

        noticeRoutine = StartCoroutine(TemporaryNoticeRoutine(message, duration));
    }

    private IEnumerator TemporaryNoticeRoutine(string message, float duration)
    {
        ShowNoticeMessage(message);
        yield return new WaitForSeconds(duration);
        HideMonsterNotice();
        noticeRoutine = null;
    }

    private void HideMonsterNotice()
    {
        if (noticeRoutine != null)
        {
            StopCoroutine(noticeRoutine);
            noticeRoutine = null;
        }

        if (noticeLayer != null)
            noticeLayer.SetActive(false);
    }

    private void CacheNoticeButtons()
    {
        if (noticeLayer == null)
            return;

        noticeButtons = noticeLayer.GetComponentsInChildren<Button>(true);
    }

    private void SetNoticeButtonsVisible(bool visible)
    {
        if (noticeButtons == null || noticeButtons.Length == 0)
            CacheNoticeButtons();

        if (noticeButtons == null)
            return;

        for (int i = 0; i < noticeButtons.Length; i++)
        {
            if (noticeButtons[i] != null)
                noticeButtons[i].gameObject.SetActive(visible);
        }
    }

    private string GetText(GameTextKey key, string fallback, params object[] args)
    {
        if (textDatabase != null)
            return textDatabase.Get(key, fallback, args);

        return args == null || args.Length == 0 ? fallback : string.Format(fallback, args);
    }

    private IEnumerator MoveCameraRoutine(Vector3 fromPosition, Vector3 toPosition, float fromSize, float toSize)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, cameraFocusTransitionDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);

            focusCamera.transform.position = Vector3.Lerp(fromPosition, toPosition, eased);
            focusCamera.orthographicSize = Mathf.Lerp(fromSize, toSize, eased);

            yield return null;
        }

        focusCamera.transform.position = toPosition;
        focusCamera.orthographicSize = toSize;
    }

    private Vector3 ResolveMonsterSpawnPosition()
    {
        Vector3 desiredPosition = monsterSpawnPoint != null
            ? monsterSpawnPoint.position
            : transform.position + monsterSpawnOffset;

        if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            return hit.position;

        return desiredPosition;
    }

    private void SetEmptyVisual()
    {
        if (potRenderer != null && emptySprite != null)
            potRenderer.sprite = emptySprite;

        if (cookingBubblesObject != null)
            cookingBubblesObject.SetActive(false);

        if (readyBubbleObject != null)
            readyBubbleObject.SetActive(false);

        if (progressBarObject != null)
            progressBarObject.SetActive(false);

        UpdateProgressBar(0f);
    }

    private void SetCookingVisual()
    {
        if (potRenderer != null && cookingSprite != null)
            potRenderer.sprite = cookingSprite;

        if (cookingBubblesObject != null)
            cookingBubblesObject.SetActive(false);

        if (readyBubbleObject != null)
            readyBubbleObject.SetActive(false);

        if (progressBarObject != null)
            progressBarObject.SetActive(true);

        UpdateCookingAnimation(0f);
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

        if (progressBarObject != null)
            progressBarObject.SetActive(false);
    }

    private bool TryResolveRecipe(List<ItemData> ingredients, out CookingRecipeData recipe)
    {
        recipe = null;

        if (recipes == null)
            return false;

        for (int i = 0; i < recipes.Length; i++)
        {
            CookingRecipeData candidate = recipes[i];
            if (candidate == null)
                continue;

            if (!candidate.Matches(ingredients))
                continue;

            recipe = candidate;
            return true;
        }

        return false;
    }

    private string FormatIngredients(List<ItemData> ingredients)
    {
        if (ingredients == null || ingredients.Count == 0)
            return "none";

        List<string> ids = new List<string>();

        for (int i = 0; i < ingredients.Count; i++)
        {
            ItemData item = ingredients[i];
            ids.Add(item != null ? $"{item.itemName}({item.itemId})" : "null");
        }

        return string.Join(", ", ids);
    }

    private string FormatKnownRecipes()
    {
        List<string> recipeNames = new List<string>();
        AddRecipeNames(recipeNames, recipes);

        return recipeNames.Count > 0 ? string.Join(", ", recipeNames) : "none";
    }

    private void AddRecipeNames(List<string> recipeNames, CookingRecipeData[] recipeList)
    {
        if (recipeList == null)
            return;

        for (int i = 0; i < recipeList.Length; i++)
        {
            CookingRecipeData recipe = recipeList[i];
            if (recipe == null)
                continue;

            string recipeSummary = $"{recipe.resultName}: {recipe.GetRequirementSummary()}";
            if (!recipeNames.Contains(recipeSummary))
                recipeNames.Add(recipeSummary);
        }
    }

    private void UpdateProgressBar(float normalizedProgress)
    {
        if (progressFillTransform == null)
            return;

        float clamped = Mathf.Clamp01(normalizedProgress);
        Vector3 scale = progressFillInitialScale;
        scale.x *= clamped;
        progressFillTransform.localScale = scale;

        Vector3 position = progressFillInitialLocalPosition;
        float offsetX = (progressFillInitialScale.x - scale.x) * 0.5f;
        position.x -= offsetX;
        progressFillTransform.localPosition = position;
    }

    private void UpdateCookingAnimation(float elapsedTime)
    {
        if (potRenderer == null)
            return;

        if (cookingAnimationFrames == null || cookingAnimationFrames.Length == 0)
        {
            if (cookingSprite != null)
                potRenderer.sprite = cookingSprite;
            return;
        }

        int frameIndex = Mathf.FloorToInt(elapsedTime * Mathf.Max(0.1f, cookingAnimationFps));
        frameIndex %= cookingAnimationFrames.Length;

        Sprite frameSprite = cookingAnimationFrames[frameIndex];
        if (frameSprite != null)
            potRenderer.sprite = frameSprite;
    }
}
