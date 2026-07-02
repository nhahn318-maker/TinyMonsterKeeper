using System.Collections;
using UnityEngine;

public class HarvestNodeController : MonoBehaviour {
    [Header("Sprites")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite clickSprite;
    [SerializeField] private float clickDuration = 0.2f;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D harvestCollider;
    [SerializeField] private Camera mainCamera;

    [Header("Drop")]
    [SerializeField] private GameObject dropPrefab;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private Vector2 fallbackDropOffset = new Vector2(0f, -0.2f);
    [SerializeField] private ItemData itemData;
    [SerializeField] private int amount = 1;

    [Header("Respawn")]
    [SerializeField] private float respawnDuration = 10f;
    [SerializeField] private bool hideWhileRespawning = true;

    [Header("Click Priority")]
    [SerializeField] private LayerMask pickupLayer;

    private bool isClicking;
    private bool isAvailable = true;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (harvestCollider == null)
            harvestCollider = GetComponent<Collider2D>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (idleSprite == null && spriteRenderer != null)
            idleSprite = spriteRenderer.sprite;
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
            TryClick(Input.mousePosition);
#else
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
                TryClick(touch.position);
        }
#endif
    }

    private void TryClick(Vector2 screenPosition)
    {
        if (!isAvailable)
            return;

        if (isClicking)
            return;

        if (mainCamera == null || harvestCollider == null)
            return;

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
        Vector2 clickPoint = new Vector2(worldPosition.x, worldPosition.y);

        if (pickupLayer.value != 0 && Physics2D.OverlapPoint(clickPoint, pickupLayer) != null)
            return;

        if (!harvestCollider.OverlapPoint(clickPoint))
            return;

        StartCoroutine(ClickRoutine());
    }

    private IEnumerator ClickRoutine()
    {
        isClicking = true;
        isAvailable = false;

        if (harvestCollider != null)
            harvestCollider.enabled = false;

        if (spriteRenderer != null && clickSprite != null)
            spriteRenderer.sprite = clickSprite;

        SpawnDrop();

        yield return new WaitForSeconds(clickDuration);

        if (spriteRenderer != null && idleSprite != null)
            spriteRenderer.sprite = idleSprite;

        isClicking = false;

        if (hideWhileRespawning && spriteRenderer != null)
            spriteRenderer.enabled = false;

        yield return new WaitForSeconds(respawnDuration);

        Respawn();
    }

    private void Respawn()
    {
        isAvailable = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;

            if (idleSprite != null)
                spriteRenderer.sprite = idleSprite;
        }

        if (harvestCollider != null)
            harvestCollider.enabled = true;
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
}
