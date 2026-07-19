using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class BerryDropController : MonoBehaviour {
    public static int LastPickupClickFrame { get; private set; } = -1;

    [Header("Item")]
    [SerializeField] private ItemData itemData;
    [SerializeField] private int amount = 1;

    [Header("Drop Animation")]
    [SerializeField] private float jumpHeight = 0.25f;
    [SerializeField] private float duration = 0.25f;

    [Header("Pickup")]
    [SerializeField] private Collider2D pickupCollider;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Camera mainCamera;

    [Header("Pickup Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string pickupAnim = "Berry_Pickup";
    [SerializeField] private Sprite pickupClickSprite;
    [SerializeField] private float pickupAnimDuration = 0.25f;
    [SerializeField] private bool addToInventoryBeforeAnimation = true;

    [Header("Debug / Test")]
    [SerializeField] private bool ignoreUIWhileTesting = true;

    private bool canPickUp;
    private bool pickedUp;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (pickupCollider == null)
            pickupCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Start()
    {
        StartCoroutine(DropRoutine());
    }

    private void Update()
    {
        HandleInput();
    }

    public void Init(ItemData newItemData, int newAmount)
    {
        itemData = newItemData;
        amount = newAmount;
    }

    private void HandleInput()
    {
        if (BookOpenUI.IsOpen)
            return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("BerryDrop sees mouse click");

            if (!canPickUp)
            {
                Debug.Log("Berry cannot pickup yet");
                return;
            }

            if (pickedUp)
            {
                Debug.Log("Berry already picked up");
                return;
            }

            if (!ignoreUIWhileTesting && IsPointerOverUI())
            {
                Debug.Log("Berry click blocked by UI");
                return;
            }

            TryPickUp(Input.mousePosition);
        }
#else
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                Debug.Log("BerryDrop sees touch");

                if (!canPickUp)
                {
                    Debug.Log("Berry cannot pickup yet");
                    return;
                }

                if (pickedUp)
                {
                    Debug.Log("Berry already picked up");
                    return;
                }

                if (!ignoreUIWhileTesting && IsPointerOverUI(touch.fingerId))
                {
                    Debug.Log("Berry touch blocked by UI");
                    return;
                }

                TryPickUp(touch.position);
            }
        }
#endif
    }

    private void TryPickUp(Vector2 screenPosition)
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("Main Camera is missing!");
            return;
        }

        if (pickupCollider == null)
        {
            Debug.LogWarning("Berry pickup collider is missing!");
            return;
        }

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPosition);
        Vector2 point = new Vector2(worldPos.x, worldPos.y);

        Debug.Log($"Try pick berry at world point: {point}");

        if (pickupCollider.OverlapPoint(point))
        {
            Debug.Log("Berry collider hit!");
            PickUp();
        }
        else
        {
            Debug.Log("Clicked but not inside berry collider");
        }
    }

    private void PickUp()
    {
        if (pickedUp) return;

        pickedUp = true;
        canPickUp = false;
        LastPickupClickFrame = Time.frameCount;

        if (pickupCollider != null)
            pickupCollider.enabled = false;

        StartCoroutine(PickUpRoutine());
    }

    private IEnumerator PickUpRoutine()
    {
        if (itemData == null)
        {
            Debug.LogWarning("Berry itemData is missing!");
            Destroy(gameObject);
            yield break;
        }

        if (addToInventoryBeforeAnimation)
        {
            AddToInventory();
        }

        if (animator != null && !string.IsNullOrEmpty(pickupAnim))
        {
            animator.Play(pickupAnim, 0, 0f);
        }
        else if (spriteRenderer != null && pickupClickSprite != null)
        {
            spriteRenderer.sprite = pickupClickSprite;
        }

        yield return new WaitForSeconds(pickupAnimDuration);

        if (!addToInventoryBeforeAnimation)
        {
            AddToInventory();
        }

        Destroy(gameObject);
    }

    private void AddToInventory()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(itemData, amount);
        }
        else
        {
            Debug.LogWarning("InventoryManager is missing!");
        }

        Debug.Log($"Picked up {amount} {itemData.itemName}");
    }

    private IEnumerator DropRoutine()
    {
        canPickUp = false;

        Vector3 start = transform.position;

        // Rơi lệch trái hoặc phải ngẫu nhiên
        float randomDir = Random.value < 0.5f ? -1f : 1f;

        Vector3 end = start + new Vector3(
            randomDir * Random.Range(0.35f, 0.6f), // bay ngang
            -Random.Range(0.15f, 0.3f),            // rơi xuống
            0f
        );

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);

            // Di chuyển từ start tới end
            Vector3 pos = Vector3.Lerp(start, end, p);

            // Tạo vòng cung bay lên rồi rơi xuống
            float arc = Mathf.Sin(p * Mathf.PI) * jumpHeight;
            pos.y += arc;

            transform.position = pos;

            yield return null;
        }

        transform.position = end;
        canPickUp = true;
    }

#if UNITY_EDITOR
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }
#else
    private bool IsPointerOverUI(int fingerId)
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject(fingerId);
    }
#endif
}
