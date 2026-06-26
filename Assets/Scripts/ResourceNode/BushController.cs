using UnityEngine;
using System.Collections;

public class BushController : MonoBehaviour {
    public enum BushState {
        Normal,
        Flowering,
        Fruiting
    }

    [Header("Growth Settings")]
    [SerializeField] private float timeToFlower = 10f;
    [SerializeField] private float timeToFruit = 10f;

    [Header("Static Sprites")]
    [SerializeField] private Sprite spriteNormal;
    [SerializeField] private Sprite spriteFlowering;
    [SerializeField] private Sprite spriteFruiting;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [SerializeField] private string normalIdleAnim = "Bush_Normal_Idle";
    [SerializeField] private string floweringIdleAnim = "Bush_Flowering_Idle";
    [SerializeField] private string fruitingIdleAnim = "Bush_Fruiting_Idle";

    [SerializeField] private string normalClickAnim = "Bush_Normal_Click";
    [SerializeField] private string floweringClickAnim = "Bush_Flowering_Click";
    [SerializeField] private string fruitingClickAnim = "Bush_Fruiting_Click";

    [SerializeField] private float normalClickDuration = 0.25f;
    [SerializeField] private float floweringClickDuration = 0.25f;
    [SerializeField] private float fruitingClickDuration = 0.35f;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D bushCollider;
    [SerializeField] private Camera mainCamera;

    [Header("Harvest")]
    [SerializeField] private GameObject berryDropPrefab;
    [SerializeField] private Transform dropPoint;

    [Header("Fruit Data")]
    [SerializeField] private ItemData fruitData;
    [SerializeField] private int fruitAmount = 1;

    [Header("Click Priority")]
    [SerializeField] private LayerMask pickupLayer;


    private BushState currentState = BushState.Normal;
    private float timer;
    private bool isPlayingClickAnimation;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (bushCollider == null)
            bushCollider = GetComponent<Collider2D>();

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Start()
    {
        SetState(BushState.Normal);
    }

    private void Update()
    {
        HandleInput();

        if (isPlayingClickAnimation) return;
        if (currentState == BushState.Fruiting) return;

        timer += Time.deltaTime;

        if (currentState == BushState.Normal && timer >= timeToFlower)
        {
            SetState(BushState.Flowering);
        }
        else if (currentState == BushState.Flowering && timer >= timeToFruit)
        {
            SetState(BushState.Fruiting);
        }
    }

    private void HandleInput()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            TryClickBush(Input.mousePosition);
        }
#else
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                TryClickBush(touch.position);
            }
        }
#endif
    }

    private void TryClickBush(Vector2 screenPosition)
    {
        if (isPlayingClickAnimation) return;

        if (mainCamera == null)
        {
            Debug.LogWarning("Main Camera is missing!");
            return;
        }

        if (bushCollider == null)
        {
            Debug.LogWarning("Bush Collider2D is missing!");
            return;
        }

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
        Vector2 clickPoint = new Vector2(worldPosition.x, worldPosition.y);

        // Ưu tiên item/berry trước.
        // Nếu click trúng BerryDrop thì Bush không nhận click.
        Collider2D pickupHit = Physics2D.OverlapPoint(clickPoint, pickupLayer);
        if (pickupHit != null)
        {
            Debug.Log("Bush ignored click because pickup item is on top.");
            return;
        }

        if (bushCollider.OverlapPoint(clickPoint))
        {
            Debug.Log("CLICKED BUSH");
            StartCoroutine(ClickRoutine());
        }
    }

    private IEnumerator ClickRoutine()
    {
        isPlayingClickAnimation = true;

        BushState clickedState = currentState;

        string animName = GetClickAnimName(clickedState);
        float animDuration = GetClickAnimDuration(clickedState);

        Debug.Log($"Play bush animation: {animName}");

        if (animator != null && !string.IsNullOrEmpty(animName))
        {
            animator.Play(animName, 0, 0f);
        }

        yield return new WaitForSeconds(animDuration);

        if (clickedState == BushState.Fruiting && currentState == BushState.Fruiting)
        {
            Harvest();
        }
        else
        {
            ReturnToStaticSprite();
        }

        isPlayingClickAnimation = false;
    }

    private void Harvest()
    {
        Debug.Log("Bush harvested, berry dropped!");

        SpawnBerryDrop();

        SetState(BushState.Normal);
    }

    private void SpawnBerryDrop()
    {
        if (berryDropPrefab == null)
        {
            Debug.LogWarning("Berry Drop Prefab is missing!");
            return;
        }

        Vector3 spawnPos = dropPoint != null
            ? dropPoint.position
            : transform.position + Vector3.down * 0.25f;

        GameObject berryObj = Instantiate(berryDropPrefab, spawnPos, Quaternion.identity);

        BerryDropController berryDrop = berryObj.GetComponent<BerryDropController>();

        if (berryDrop != null)
        {
            berryDrop.Init(fruitData, fruitAmount);
        }
        else
        {
            Debug.LogWarning("BerryDropController is missing on BerryDrop prefab!");
        }
    }

    private void SetState(BushState newState)
    {
        currentState = newState;
        timer = 0f;

        ReturnToStaticSprite();

        Debug.Log($"Bush State: {currentState}");
    }

    private void ReturnToStaticSprite()
    {
        if (animator == null) return;

        switch (currentState)
        {
            case BushState.Normal:
                animator.Play(normalIdleAnim, 0, 0f);
                break;

            case BushState.Flowering:
                animator.Play(floweringIdleAnim, 0, 0f);
                break;

            case BushState.Fruiting:
                animator.Play(fruitingIdleAnim, 0, 0f);
                break;
        }
    }

    private string GetClickAnimName(BushState state)
    {
        switch (state)
        {
            case BushState.Normal:
                return normalClickAnim;

            case BushState.Flowering:
                return floweringClickAnim;

            case BushState.Fruiting:
                return fruitingClickAnim;

            default:
                return normalClickAnim;
        }
    }

    private float GetClickAnimDuration(BushState state)
    {
        switch (state)
        {
            case BushState.Normal:
                return normalClickDuration;

            case BushState.Flowering:
                return floweringClickDuration;

            case BushState.Fruiting:
                return fruitingClickDuration;

            default:
                return 0.25f;
        }
    }
}