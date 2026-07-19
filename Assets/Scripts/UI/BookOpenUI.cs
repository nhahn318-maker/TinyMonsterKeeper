using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BookOpenUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image bookImage;
    [SerializeField] private Sprite[] openFrames;
    [SerializeField] private Sprite[] pageTurnFrames;
    [SerializeField] private Sprite[] pageTurnBackFrames;
    [SerializeField] private RectTransform sourceTransform;
    [SerializeField] private GameObject[] cardsRoots;
    [SerializeField] private int totalPages = 10;
    [SerializeField] private MonsterData[] monsters;
    [SerializeField] private Sprite lockedCardSprite;
    [SerializeField] private Sprite unlockedCardSprite;
    [SerializeField] private CameraMapDragController cameraDragController;
    [SerializeField] private bool lockCameraDragWhileOpen = true;
    [SerializeField] private Vector2 monsterIconSize = new Vector2(96f, 96f);
    [SerializeField] private Vector2 monsterIconOffset = new Vector2(0f, 10f);
    [SerializeField] private float flyDuration = 0.28f;
    [SerializeField] private float openDuration = 0.45f;
    [SerializeField] private float pageTurnDuration = 0.35f;
    [SerializeField] private float swipeThreshold = 80f;
    [SerializeField] private float verticalSwipeTolerance = 0.65f;

    private Coroutine openRoutine;
    private Coroutine pageRoutine;
    private RectTransform bookTransform;
    private RectTransform bookParent;
    private Vector2 targetAnchoredPosition;
    private Vector2 targetSizeDelta;
    private Vector2 swipeStartPosition;
    private bool isTrackingSwipe;
    private readonly List<CardView> cardViews = new List<CardView>();
    private int currentPage;
    private bool lockedCameraDrag;

    public static bool IsOpen { get; private set; }

    private void Awake()
    {
        if (bookImage != null)
        {
            bookTransform = bookImage.rectTransform;
            bookParent = bookTransform.parent as RectTransform;
            targetAnchoredPosition = bookTransform.anchoredPosition;
            targetSizeDelta = bookTransform.sizeDelta;
        }

        CacheCardViews();
        Hide();
    }

    private void OnEnable()
    {
        MonsterCollectionManager.MonsterUnlocked += HandleMonsterUnlocked;
    }

    private void OnDisable()
    {
        MonsterCollectionManager.MonsterUnlocked -= HandleMonsterUnlocked;
        IsOpen = false;
        UnlockCameraDragIfNeeded();
    }

    private void Update()
    {
        HandleSwipeInput();
    }

    public void Show()
    {
        if (panelRoot == null)
            return;

        IsOpen = true;
        LockCameraDragIfNeeded();
        panelRoot.SetActive(true);

        if (openRoutine != null)
            StopCoroutine(openRoutine);

        currentPage = Mathf.Clamp(currentPage, 0, GetLastPageIndex());
        openRoutine = StartCoroutine(OpenRoutine());
    }

    public void Hide()
    {
        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
            openRoutine = null;
        }

        if (pageRoutine != null)
        {
            StopCoroutine(pageRoutine);
            pageRoutine = null;
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);

        IsOpen = false;
        isTrackingSwipe = false;
        SetCardsRootsActive(false);
        UnlockCameraDragIfNeeded();
    }

    public void OnClickNextPage()
    {
        if (currentPage >= GetLastPageIndex())
            return;

        StartPageTurn(currentPage + 1, pageTurnFrames);
    }

    public void OnClickPreviousPage()
    {
        if (currentPage <= 0)
            return;

        StartPageTurn(currentPage - 1, pageTurnBackFrames);
    }

    private void HandleSwipeInput()
    {
        if (!IsOpen || panelRoot == null || !panelRoot.activeInHierarchy)
            return;

        if (openRoutine != null || pageRoutine != null)
            return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            BeginSwipe(Input.mousePosition);
            return;
        }

        if (Input.GetMouseButtonUp(0))
            EndSwipe(Input.mousePosition);
#else
        if (Input.touchCount <= 0)
            return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Began)
        {
            BeginSwipe(touch.position);
            return;
        }

        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            EndSwipe(touch.position);
#endif
    }

    private void BeginSwipe(Vector2 screenPosition)
    {
        swipeStartPosition = screenPosition;
        isTrackingSwipe = true;
    }

    private void EndSwipe(Vector2 screenPosition)
    {
        if (!isTrackingSwipe)
            return;

        isTrackingSwipe = false;
        TryTurnPageFromSwipe(screenPosition - swipeStartPosition);
    }

    private void TryTurnPageFromSwipe(Vector2 swipeDelta)
    {
        if (Mathf.Abs(swipeDelta.x) < swipeThreshold)
            return;

        if (Mathf.Abs(swipeDelta.y) > Mathf.Abs(swipeDelta.x) * verticalSwipeTolerance)
            return;

        if (swipeDelta.x < 0f)
            OnClickNextPage();
        else
            OnClickPreviousPage();
    }

    private IEnumerator OpenRoutine()
    {
        if (bookImage == null || openFrames == null || openFrames.Length == 0)
            yield break;

        bookImage.sprite = openFrames[0];

        yield return FlyFromSource();
        yield return PlayOpenAnimation();

        SetCardsRootsActive(true);
        RefreshCards();

        openRoutine = null;
    }

    private IEnumerator FlyFromSource()
    {
        if (bookTransform == null || sourceTransform == null || bookParent == null || flyDuration <= 0f)
            yield break;

        Vector2 startPosition = targetAnchoredPosition;
        Vector2 startSize = targetSizeDelta * 0.35f;

        Vector2 sourceScreenPosition = RectTransformUtility.WorldToScreenPoint(null, sourceTransform.position);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(bookParent, sourceScreenPosition, null, out Vector2 localPoint))
            startPosition = localPoint;

        if (sourceTransform.rect.size.sqrMagnitude > 0f)
            startSize = sourceTransform.rect.size;

        bookTransform.anchoredPosition = RoundVector2(startPosition);
        bookTransform.sizeDelta = RoundVector2(startSize);

        float elapsed = 0f;
        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flyDuration);
            float eased = t * t * (3f - 2f * t);

            bookTransform.anchoredPosition = RoundVector2(Vector2.LerpUnclamped(startPosition, targetAnchoredPosition, eased));
            bookTransform.sizeDelta = RoundVector2(Vector2.LerpUnclamped(startSize, targetSizeDelta, eased));

            yield return null;
        }

        bookTransform.anchoredPosition = RoundVector2(targetAnchoredPosition);
        bookTransform.sizeDelta = RoundVector2(targetSizeDelta);
    }

    private IEnumerator PlayOpenAnimation()
    {
        if (bookImage == null || openFrames == null || openFrames.Length == 0)
            yield break;

        float frameDelay = openDuration / openFrames.Length;

        for (int i = 0; i < openFrames.Length; i++)
        {
            bookImage.sprite = openFrames[i];
            yield return new WaitForSeconds(frameDelay);
        }

        bookImage.sprite = openFrames[openFrames.Length - 1];
    }

    private void StartPageTurn(int pageIndex, Sprite[] turnFrames)
    {
        if (pageRoutine != null)
            StopCoroutine(pageRoutine);

        pageRoutine = StartCoroutine(PageTurnRoutine(pageIndex, turnFrames));
    }

    private IEnumerator PageTurnRoutine(int pageIndex, Sprite[] turnFrames)
    {
        SetCardsRootsActive(false);

        if (bookImage != null && turnFrames != null && turnFrames.Length > 0)
        {
            float frameDelay = pageTurnDuration / turnFrames.Length;

            for (int i = 0; i < turnFrames.Length; i++)
            {
                bookImage.sprite = turnFrames[i];
                yield return new WaitForSeconds(frameDelay);
            }
        }

        if (bookImage != null && openFrames != null && openFrames.Length > 0)
            bookImage.sprite = openFrames[openFrames.Length - 1];

        currentPage = Mathf.Clamp(pageIndex, 0, GetLastPageIndex());
        RefreshCards();
        SetCardsRootsActive(true);

        pageRoutine = null;
    }

    private static Vector2 RoundVector2(Vector2 value)
    {
        return new Vector2(Mathf.Round(value.x), Mathf.Round(value.y));
    }

    private void SetCardsRootsActive(bool isActive)
    {
        if (cardsRoots == null)
            return;

        for (int i = 0; i < cardsRoots.Length; i++)
        {
            if (cardsRoots[i] != null)
                cardsRoots[i].SetActive(isActive);
        }
    }

    private void LockCameraDragIfNeeded()
    {
        if (!lockCameraDragWhileOpen || lockedCameraDrag)
            return;

        if (cameraDragController == null)
            cameraDragController = CameraMapDragController.Instance;

        if (cameraDragController == null)
            return;

        cameraDragController.SetInputLocked(true);
        lockedCameraDrag = true;
    }

    private void UnlockCameraDragIfNeeded()
    {
        if (!lockedCameraDrag)
            return;

        if (cameraDragController != null)
            cameraDragController.SetInputLocked(false);

        lockedCameraDrag = false;
    }

    private void HandleMonsterUnlocked(MonsterData monsterData)
    {
        if (panelRoot != null && panelRoot.activeInHierarchy)
            RefreshCards();
    }

    private void RefreshCards()
    {
        if (cardViews.Count == 0)
            CacheCardViews();

        int pageCapacity = Mathf.Max(1, cardViews.Count);
        int startIndex = currentPage * pageCapacity;

        for (int i = 0; i < cardViews.Count; i++)
        {
            int monsterIndex = startIndex + i;
            MonsterData monsterData = monsters != null && monsterIndex < monsters.Length ? monsters[monsterIndex] : null;
            bool isUnlocked = MonsterCollectionManager.IsUnlocked(monsterData);

            cardViews[i].Set(monsterData, isUnlocked, lockedCardSprite, unlockedCardSprite, monsterIconSize, monsterIconOffset);
        }
    }

    private void CacheCardViews()
    {
        cardViews.Clear();

        if (cardsRoots == null)
            return;

        for (int i = 0; i < cardsRoots.Length; i++)
        {
            if (cardsRoots[i] == null)
                continue;

            Image[] images = cardsRoots[i].GetComponentsInChildren<Image>(true);
            for (int imageIndex = 0; imageIndex < images.Length; imageIndex++)
            {
                Image image = images[imageIndex];
                if (image == null || image.gameObject == cardsRoots[i])
                    continue;

                cardViews.Add(new CardView(image));
            }
        }

        if (lockedCardSprite == null && cardViews.Count > 0)
            lockedCardSprite = cardViews[0].Background.sprite;
    }

    private int GetLastPageIndex()
    {
        int configuredPageCount = Mathf.Max(1, totalPages);
        return configuredPageCount - 1;
    }

    private sealed class CardView
    {
        private readonly Image background;
        private Image monsterIcon;

        public CardView(Image background)
        {
            this.background = background;
        }

        public Image Background => background;

        public void Set(MonsterData monsterData, bool isUnlocked, Sprite lockedSprite, Sprite unlockedSprite, Vector2 iconSize, Vector2 iconOffset)
        {
            if (background == null)
                return;

            background.sprite = isUnlocked && unlockedSprite != null ? unlockedSprite : lockedSprite;
            background.enabled = true;

            Image icon = GetOrCreateIcon();
            if (icon == null)
                return;

            icon.sprite = isUnlocked && monsterData != null ? monsterData.icon : null;
            icon.enabled = isUnlocked && monsterData != null && monsterData.icon != null;
            icon.raycastTarget = false;

            RectTransform iconTransform = icon.rectTransform;
            iconTransform.sizeDelta = BookOpenUI.RoundVector2(iconSize);
            iconTransform.anchoredPosition = BookOpenUI.RoundVector2(iconOffset);
        }

        private Image GetOrCreateIcon()
        {
            if (monsterIcon != null)
                return monsterIcon;

            Transform existing = background.transform.Find("MonsterIcon");
            if (existing != null)
            {
                monsterIcon = existing.GetComponent<Image>();
                if (monsterIcon != null)
                    return monsterIcon;
            }

            GameObject iconObject = new GameObject("MonsterIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(background.transform, false);

            monsterIcon = iconObject.GetComponent<Image>();
            monsterIcon.preserveAspect = true;

            RectTransform iconTransform = monsterIcon.rectTransform;
            iconTransform.anchorMin = new Vector2(0.5f, 0.5f);
            iconTransform.anchorMax = new Vector2(0.5f, 0.5f);
            iconTransform.pivot = new Vector2(0.5f, 0.5f);

            return monsterIcon;
        }
    }
}
