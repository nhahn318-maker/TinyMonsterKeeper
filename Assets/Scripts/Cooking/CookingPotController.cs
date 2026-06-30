using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class CookingPotController : MonoBehaviour, IPointerClickHandler {
    [System.Serializable]
    private struct CookingRecipe
    {
        public string resultName;
        public string requiredItemId;
        public int requiredCount;
        public float cookDuration;
    }

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
    [SerializeField] private GameObject[] monsterPrefabs;
    [SerializeField] private Transform monsterSpawnPoint;
    [SerializeField] private Vector3 monsterSpawnOffset = new Vector3(1.2f, -0.6f, 0f);
    [SerializeField] private Collider2D monsterGardenBounds;

    [Header("Camera Focus")]
    [SerializeField] private Camera focusCamera;
    [SerializeField] private float monsterFocusDuration = 5f;
    [SerializeField] private float monsterFocusOrthographicSize = 2.4f;
    [SerializeField] private Vector3 monsterFocusOffset = new Vector3(0f, 0.35f, 0f);
    [SerializeField] private float cameraFocusTransitionDuration = 0.45f;

    [Header("Recipes")]
    [SerializeField] private CookingRecipe berrySoupRecipe = new CookingRecipe
    {
        resultName = "Berry Soup",
        requiredItemId = "berry",
        requiredCount = 3,
        cookDuration = 10f
    };

    private bool isCooking;
    private bool isDone;
    private Vector3 progressFillInitialScale;
    private Vector3 progressFillInitialLocalPosition;
    private float currentCookDuration;
    private bool isAttracting;
    private Coroutine cameraFocusRoutine;

    private void Awake()
    {
        if (potRenderer == null)
            potRenderer = GetComponent<SpriteRenderer>();

        if (progressFillTransform != null)
        {
            progressFillInitialScale = progressFillTransform.localScale;
            progressFillInitialLocalPosition = progressFillTransform.localPosition;
        }

        if (aromaAnimation == null)
            aromaAnimation = GetComponentInChildren<AromaAnimation>(true);

        if (focusCamera == null)
            focusCamera = Camera.main;

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

        if (!TryResolveRecipe(ingredients, out CookingRecipe recipe))
        {
            Debug.LogWarning("No cooking recipe matched these ingredients.");
            return false;
        }

        ConsumeIngredients(ingredients);
        StartCoroutine(CookingRoutine(recipe));

        return true;
    }

    private IEnumerator CookingRoutine(CookingRecipe recipe)
    {
        isCooking = true;
        isDone = false;
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

        isAttracting = false;
    }

    private void SpawnRandomMonster()
    {
        if (monsterPrefabs == null || monsterPrefabs.Length == 0)
        {
            Debug.LogWarning("No monster prefab assigned for cooking attraction.");
            return;
        }

        GameObject prefab = monsterPrefabs[Random.Range(0, monsterPrefabs.Length)];
        if (prefab == null)
        {
            Debug.LogWarning("Selected monster prefab is missing.");
            return;
        }

        Vector3 spawnPosition = ResolveMonsterSpawnPosition();

        GameObject monster = Instantiate(prefab, spawnPosition, Quaternion.identity);
        ConfigureSpawnedMonster(monster);
        FocusCameraOnMonster(monster.transform);

        Debug.Log($"Spawned attracted monster: {monster.name}");
    }

    private void ConfigureSpawnedMonster(GameObject monster)
    {
        if (monster == null)
            return;

        TinyMonsterNavRoam navRoam = monster.GetComponent<TinyMonsterNavRoam>();
        if (navRoam != null && monsterGardenBounds != null)
            navRoam.SetGardenBounds(monsterGardenBounds);
    }

    private void FocusCameraOnMonster(Transform target)
    {
        if (focusCamera == null || target == null)
            return;

        if (cameraFocusRoutine != null)
            StopCoroutine(cameraFocusRoutine);

        cameraFocusRoutine = StartCoroutine(FocusCameraRoutine(target));
    }

    private IEnumerator FocusCameraRoutine(Transform target)
    {
        Vector3 originalPosition = focusCamera.transform.position;
        float originalOrthographicSize = focusCamera.orthographicSize;

        Vector3 focusPosition = target.position + monsterFocusOffset;
        focusPosition.z = originalPosition.z;

        yield return MoveCameraRoutine(originalPosition, focusPosition, originalOrthographicSize, monsterFocusOrthographicSize);
        yield return new WaitForSeconds(monsterFocusDuration);
        yield return MoveCameraRoutine(focusCamera.transform.position, originalPosition, focusCamera.orthographicSize, originalOrthographicSize);

        cameraFocusRoutine = null;
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

    private bool TryResolveRecipe(List<ItemData> ingredients, out CookingRecipe recipe)
    {
        recipe = default;

        if (ingredients == null || ingredients.Count != berrySoupRecipe.requiredCount)
            return false;

        for (int i = 0; i < ingredients.Count; i++)
        {
            if (ingredients[i] == null || ingredients[i].itemId != berrySoupRecipe.requiredItemId)
                return false;
        }

        recipe = berrySoupRecipe;
        return true;
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
