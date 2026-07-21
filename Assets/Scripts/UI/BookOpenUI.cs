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
    [SerializeField] private GameObject detailRoot;
    [SerializeField] private Image detailCardImage;
    [SerializeField] private Image detailBadgeImage;
    [SerializeField] private Image[] detailStarImages;
    [SerializeField] private Sprite bronzeBadgeSprite;
    [SerializeField] private Sprite silverBadgeSprite;
    [SerializeField] private Sprite goldBadgeSprite;
    [SerializeField] private Sprite bronzeStarSprite;
    [SerializeField] private Sprite silverStarSprite;
    [SerializeField] private Sprite goldStarSprite;
    [SerializeField] private int totalPages = 2;
    [SerializeField] private MonsterData[] monsters;
    [SerializeField] private MonsterData defaultSelectedMonster;
    [SerializeField] private bool showDefaultDetailOnOpen = true;
    [SerializeField] private Sprite lockedCardSprite;
    [SerializeField] private Sprite unlockedCardSprite;
    [SerializeField] private CameraMapDragController cameraDragController;
    [SerializeField] private bool lockCameraDragWhileOpen = true;
    [SerializeField] private Vector2 monsterIconSize = new Vector2(96f, 96f);
    [SerializeField] private Vector2 monsterIconOffset = new Vector2(0f, 10f);
    [SerializeField] private Vector2 detailMonsterIconSize = new Vector2(288f, 288f);
    [SerializeField] private Vector2 detailMonsterIconOffset = new Vector2(0f, 10f);
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
    private DetailView detailView;

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
        CacheDetailView();
        Hide();
    }

    private void OnEnable()
    {
        MonsterCollectionManager.MonsterCollectionChanged += HandleMonsterCollectionChanged;
    }

    private void OnDisable()
    {
        MonsterCollectionManager.MonsterCollectionChanged -= HandleMonsterCollectionChanged;
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

        currentPage = showDefaultDetailOnOpen && defaultSelectedMonster != null
            ? GetPageIndexForMonster(defaultSelectedMonster)
            : Mathf.Clamp(currentPage, 0, GetLastPageIndex());

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
        SetDetailRootActive(false);
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
        SetDetailRootActive(true);
        RefreshCards();
        ShowInitialDetail();

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
        ShowInitialDetail();

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

    private void SetDetailRootActive(bool isActive)
    {
        if (detailRoot != null)
            detailRoot.SetActive(isActive);
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

    private void HandleMonsterCollectionChanged(MonsterData monsterData, int count)
    {
        if (panelRoot != null && panelRoot.activeInHierarchy)
        {
            RefreshCards();

            if (detailView != null && detailView.MonsterData == monsterData)
                ShowDetail(monsterData);
        }
    }

    private void RefreshCards()
    {
        if (cardViews.Count == 0)
            CacheCardViews();

        int pageCapacity = GetPageCapacity();
        int startIndex = currentPage * pageCapacity;

        for (int i = 0; i < cardViews.Count; i++)
        {
            int monsterIndex = startIndex + i;
            MonsterData monsterData = monsters != null && monsterIndex < monsters.Length ? monsters[monsterIndex] : null;
            int unlockCount = GetDisplayUnlockCount(monsterData);
            bool isUnlocked = unlockCount > 0;

            cardViews[i].SetVisible(true);
            cardViews[i].Set(monsterData, monsterIndex, isUnlocked, lockedCardSprite, unlockedCardSprite, monsterIconSize, monsterIconOffset, OnClickCard);
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

            Transform root = cardsRoots[i].transform;
            for (int childIndex = 0; childIndex < root.childCount; childIndex++)
            {
                Transform child = root.GetChild(childIndex);
                if (IsInsideDetailRoot(child))
                    continue;

                Image image = child.GetComponent<Image>();
                if (image == null)
                    continue;

                CardView cardView = new CardView(image);
                cardViews.Add(cardView);
            }
        }

        if (lockedCardSprite == null && cardViews.Count > 0)
            lockedCardSprite = cardViews[0].Background.sprite;
    }

    private void CacheDetailView()
    {
        if (detailCardImage == null && detailRoot != null)
        {
            Image[] images = detailRoot.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].gameObject != detailRoot)
                {
                    detailCardImage = images[i];
                    break;
                }
            }
        }

        if (detailView == null && detailCardImage != null)
            detailView = new DetailView(detailCardImage);

        if ((detailStarImages == null || detailStarImages.Length == 0) && detailRoot != null)
        {
            Transform starsRoot = detailRoot.transform.Find("Stars");
            if (starsRoot != null)
                detailStarImages = starsRoot.GetComponentsInChildren<Image>(true);
        }
    }

    private bool IsInsideDetailRoot(Transform target)
    {
        return detailRoot != null && target != null && target.IsChildOf(detailRoot.transform);
    }

    private void OnClickCard(CardView cardView)
    {
        if (cardView == null || !cardView.IsUnlocked)
            return;

        ShowDetail(cardView.MonsterData);
    }

    private void ShowDetail(MonsterData monsterData)
    {
        if (monsterData == null)
        {
            HideDetail();
            return;
        }

        if (detailView == null)
            CacheDetailView();

        int unlockCount = GetDisplayUnlockCount(monsterData);
        if (unlockCount <= 0)
        {
            HideDetail();
            return;
        }

        BadgeTier tier = GetBadgeTier(unlockCount);
        int activeStars = GetActiveStarsInTier(unlockCount);

        if (detailView != null)
            detailView.Set(monsterData, unlockedCardSprite, detailMonsterIconSize, detailMonsterIconOffset);

        if (detailBadgeImage != null)
        {
            detailBadgeImage.sprite = GetBadgeSprite(tier);
            detailBadgeImage.enabled = detailBadgeImage.sprite != null;
        }

        if (detailStarImages != null)
        {
            Sprite starSprite = GetStarSprite(tier);
            for (int i = 0; i < detailStarImages.Length; i++)
            {
                if (detailStarImages[i] == null)
                    continue;

                bool isActive = i < activeStars && starSprite != null;
                detailStarImages[i].gameObject.SetActive(isActive);
                detailStarImages[i].sprite = starSprite;
                detailStarImages[i].enabled = isActive;
            }
        }
    }

    private void HideDetail()
    {
        if (detailView == null)
            CacheDetailView();

        if (detailView != null)
            detailView.Hide();

        if (detailBadgeImage != null)
            detailBadgeImage.enabled = false;

        if (detailStarImages == null)
            return;

        for (int i = 0; i < detailStarImages.Length; i++)
        {
            if (detailStarImages[i] != null)
                detailStarImages[i].gameObject.SetActive(false);
        }
    }

    private int GetLastPageIndex()
    {
        int configuredPageCount = Mathf.Max(1, totalPages);
        return configuredPageCount - 1;
    }

    private int GetPageCapacity()
    {
        return Mathf.Max(1, cardViews.Count);
    }

    private int GetPageIndexForMonster(MonsterData monsterData)
    {
        if (monsterData == null || monsters == null)
            return 0;

        int pageCapacity = GetPageCapacity();
        for (int i = 0; i < monsters.Length; i++)
        {
            if (IsSameMonster(monsters[i], monsterData))
                return Mathf.Clamp(i / pageCapacity, 0, GetLastPageIndex());
        }

        Debug.LogWarning($"Default book monster is not in the BookOpenUI monster list: {monsterData.name}");
        return 0;
    }

    private void ShowInitialDetail()
    {
        if (!showDefaultDetailOnOpen)
        {
            HideDetail();
            return;
        }

        MonsterData monsterData = defaultSelectedMonster != null ? defaultSelectedMonster : GetFirstUnlockedMonsterOnCurrentPage();
        if (monsterData != null && GetDisplayUnlockCount(monsterData) > 0)
            ShowDetail(monsterData);
        else
            HideDetail();
    }

    private MonsterData GetFirstUnlockedMonsterOnCurrentPage()
    {
        int pageCapacity = GetPageCapacity();
        int startIndex = currentPage * pageCapacity;

        for (int i = 0; i < pageCapacity; i++)
        {
            int monsterIndex = startIndex + i;
            MonsterData monsterData = monsters != null && monsterIndex < monsters.Length ? monsters[monsterIndex] : null;
            if (GetDisplayUnlockCount(monsterData) > 0)
                return monsterData;
        }

        return null;
    }

    private int GetDisplayUnlockCount(MonsterData monsterData)
    {
        int count = MonsterCollectionManager.GetUnlockCount(monsterData);
        if (IsSameMonster(monsterData, defaultSelectedMonster))
            return Mathf.Max(1, count);

        return count;
    }

    private static bool IsSameMonster(MonsterData left, MonsterData right)
    {
        if (left == null || right == null)
            return false;

        string leftId = MonsterCollectionManager.GetMonsterId(left);
        string rightId = MonsterCollectionManager.GetMonsterId(right);
        return !string.IsNullOrEmpty(leftId) && leftId == rightId;
    }

    private static BadgeTier GetBadgeTier(int unlockCount)
    {
        if (unlockCount >= 7)
            return BadgeTier.Gold;

        if (unlockCount >= 4)
            return BadgeTier.Silver;

        return BadgeTier.Bronze;
    }

    private static int GetActiveStarsInTier(int unlockCount)
    {
        return Mathf.Clamp(((Mathf.Max(1, unlockCount) - 1) % 3) + 1, 1, 3);
    }

    private Sprite GetBadgeSprite(BadgeTier tier)
    {
        switch (tier)
        {
            case BadgeTier.Silver:
                return silverBadgeSprite != null ? silverBadgeSprite : bronzeBadgeSprite;
            case BadgeTier.Gold:
                return goldBadgeSprite != null ? goldBadgeSprite : silverBadgeSprite;
            default:
                return bronzeBadgeSprite;
        }
    }

    private Sprite GetStarSprite(BadgeTier tier)
    {
        switch (tier)
        {
            case BadgeTier.Silver:
                return silverStarSprite != null ? silverStarSprite : bronzeStarSprite;
            case BadgeTier.Gold:
                return goldStarSprite != null ? goldStarSprite : silverStarSprite;
            default:
                return bronzeStarSprite;
        }
    }

    private enum BadgeTier
    {
        Bronze,
        Silver,
        Gold
    }

    private sealed class CardView
    {
        private readonly Image background;
        private Image monsterIcon;
        private Button button;

        public CardView(Image background)
        {
            this.background = background;
        }

        public Image Background => background;
        public MonsterData MonsterData { get; private set; }
        public bool IsUnlocked { get; private set; }

        public void SetVisible(bool isVisible)
        {
            if (background != null)
                background.gameObject.SetActive(isVisible);
        }

        public void Set(MonsterData monsterData, int monsterIndex, bool isUnlocked, Sprite lockedSprite, Sprite unlockedSprite, Vector2 iconSize, Vector2 iconOffset, System.Action<CardView> onClick)
        {
            if (background == null)
                return;

            MonsterData = monsterData;
            IsUnlocked = isUnlocked;
            background.sprite = isUnlocked && unlockedSprite != null ? unlockedSprite : lockedSprite;
            background.enabled = true;
            background.raycastTarget = true;

            Button cardButton = GetOrCreateButton();
            if (cardButton != null)
            {
                cardButton.interactable = isUnlocked;
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(() => onClick?.Invoke(this));
            }

            Image icon = GetOrCreateIcon();
            if (icon == null)
                return;

            icon.sprite = isUnlocked && monsterData != null ? monsterData.icon : null;
            icon.enabled = isUnlocked && monsterData != null && monsterData.icon != null;
            icon.raycastTarget = false;
            icon.transform.SetAsLastSibling();

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

        private Button GetOrCreateButton()
        {
            if (button != null)
                return button;

            button = background.GetComponent<Button>();
            if (button == null)
                button = background.gameObject.AddComponent<Button>();

            button.transition = Selectable.Transition.None;
            return button;
        }
    }

    private sealed class DetailView
    {
        private readonly Image background;
        private Image monsterIcon;

        public DetailView(Image background)
        {
            this.background = background;
        }

        public MonsterData MonsterData { get; private set; }

        public void Set(MonsterData monsterData, Sprite cardSprite, Vector2 iconSize, Vector2 iconOffset)
        {
            if (background == null)
                return;

            MonsterData = monsterData;
            background.gameObject.SetActive(true);
            background.enabled = true;
            background.sprite = cardSprite != null ? cardSprite : background.sprite;

            Image icon = GetOrCreateIcon();
            if (icon == null)
                return;

            icon.sprite = monsterData != null ? monsterData.icon : null;
            icon.enabled = monsterData != null && monsterData.icon != null;
            icon.raycastTarget = false;

            RectTransform iconTransform = icon.rectTransform;
            iconTransform.sizeDelta = BookOpenUI.RoundVector2(iconSize);
            iconTransform.anchoredPosition = BookOpenUI.RoundVector2(iconOffset);
        }

        public void Hide()
        {
            MonsterData = null;

            if (background != null)
                background.gameObject.SetActive(false);
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
