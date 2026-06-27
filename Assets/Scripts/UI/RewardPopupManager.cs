using UnityEngine;

public class RewardPopupManager : MonoBehaviour {
    public static RewardPopupManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform popupLayer;
    [SerializeField] private CoinRewardPopupUI coinPopupPrefab;

    private Camera mainCamera;

    private Camera CanvasCamera
    {
        get
        {
            if (canvas == null) return null;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (popupLayer == null)
            popupLayer = transform.parent as RectTransform;

        mainCamera = Camera.main;
    }

    public void ShowCoinPopup(Vector3 worldPosition, int amount)
    {
        if (coinPopupPrefab == null)
        {
            Debug.LogWarning("Coin popup prefab is missing!");
            return;
        }

        if (popupLayer == null)
        {
            Debug.LogWarning("PopupLayer is missing!");
            return;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogWarning("Main Camera is missing!");
            return;
        }

        Vector2 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            popupLayer,
            screenPosition,
            CanvasCamera,
            out Vector2 localPoint
        );

        CoinRewardPopupUI popup = Instantiate(coinPopupPrefab, popupLayer);
        popup.GetComponent<RectTransform>().anchoredPosition = localPoint;
        popup.Show(amount);
    }
}