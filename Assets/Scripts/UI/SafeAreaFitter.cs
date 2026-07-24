using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private enum FitMode
    {
        FullRect,
        TopBar
    }

    [SerializeField] private FitMode fitMode = FitMode.TopBar;
    [SerializeField] private bool fitLeft = true;
    [SerializeField] private bool fitRight = true;
    [SerializeField] private bool fitTop = true;
    [SerializeField] private bool fitBottom;
    [SerializeField] private Vector2 extraPadding = new Vector2(8f, 8f);

    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void Update()
    {
        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);

        if (safeArea != lastSafeArea || screenSize != lastScreenSize)
            ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        if (rectTransform == null || Screen.width <= 0 || Screen.height <= 0)
            return;

        Rect safeArea = Screen.safeArea;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        if (fitMode == FitMode.TopBar)
        {
            ApplyTopBar(safeArea, screenSize);
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            return;
        }

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= screenSize.x;
        anchorMin.y /= screenSize.y;
        anchorMax.x /= screenSize.x;
        anchorMax.y /= screenSize.y;

        if (!fitLeft)
            anchorMin.x = 0f;
        if (!fitBottom)
            anchorMin.y = 0f;
        if (!fitRight)
            anchorMax.x = 1f;
        if (!fitTop)
            anchorMax.y = 1f;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = new Vector2(fitLeft ? extraPadding.x : 0f, fitBottom ? extraPadding.y : 0f);
        rectTransform.offsetMax = new Vector2(fitRight ? -extraPadding.x : 0f, fitTop ? -extraPadding.y : 0f);

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

    private void ApplyTopBar(Rect safeArea, Vector2 screenSize)
    {
        float leftInset = fitLeft ? safeArea.xMin : 0f;
        float rightInset = fitRight ? screenSize.x - safeArea.xMax : 0f;
        float topInset = fitTop ? screenSize.y - safeArea.yMax : 0f;

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.offsetMin = new Vector2(leftInset + extraPadding.x, rectTransform.offsetMin.y);
        rectTransform.offsetMax = new Vector2(-(rightInset + extraPadding.x), rectTransform.offsetMax.y);
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, -(topInset + extraPadding.y));
    }
}
