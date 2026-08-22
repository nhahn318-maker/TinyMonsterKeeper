using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class MonsterUIPanel : MonoBehaviour
{
    private const string PlayCooldownPrefix = "TinyMonsterKeeper.MonsterPlayCooldown.";

    public static MonsterUIPanel Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform panelContainer;
    [SerializeField] private RectTransform infoPanel;
    [SerializeField] private RectTransform actionMenuPanel;
    [SerializeField] private GameTextDatabase textDatabase;

    [Header("Info Panel Elements")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image friendshipBarFill;
    [SerializeField] private TextMeshProUGUI friendshipText;
    [SerializeField] private TextMeshProUGUI feedCostText;


    [Header("Feed Settings")]
    [SerializeField] private ItemData berryItemData;

    [Header("Play Settings")]
    [SerializeField] private int playFriendshipGain = 15;
    [SerializeField] private float playCooldownSeconds = 3600f;
    [SerializeField] private GameTextKey playCooldownMessageKey = GameTextKey.MonsterPlayCooldown;
    [SerializeField] private string playCooldownMessageFallback = "You can play with this monster again in {0}.";



    [Header("HUD Warning")]
    [SerializeField] private HUDItemCounterUI berryHUDCounter;

    [Header("Position")]
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, 100f);


    private TinyMonsterTouch selectedMonster;
    private RectTransform canvasRect;

    private Camera CanvasCamera => canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
        ? canvas.worldCamera
        : null;

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

        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

        Hide(false);
    }

    private void Update()
    {
        if (selectedMonster == null || !panelContainer.gameObject.activeSelf)
            return;

        if (WasPointerPressedOutsidePanel())
        {
            Hide(true);
        }
    }

    public void Show(TinyMonsterTouch monster)
    {
        if (monster == null || canvas == null || canvasRect == null || panelContainer == null)
            return;

        if (selectedMonster != null && selectedMonster != monster && selectedMonster.Controller != null)
        {
            selectedMonster.Controller.ResumeAfterMenu();
        }

        selectedMonster = monster;
        UpdateInfo(monster);

        if (selectedMonster.Controller != null)
        {
            selectedMonster.Controller.PauseForMenu();
        }

        Vector2 screenPosition = Camera.main.WorldToScreenPoint(monster.transform.position);
        screenPosition += screenOffset;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            CanvasCamera,
            out Vector2 localPoint
        );

        panelContainer.anchoredPosition = localPoint;
        panelContainer.gameObject.SetActive(true);
    }

    public void Hide(bool resumeMonster = true)
    {
        if (panelContainer != null)
            panelContainer.gameObject.SetActive(false);

        if (resumeMonster && selectedMonster != null && selectedMonster.Controller != null)
        {
            selectedMonster.Controller.ResumeAfterMenu();
        }

        selectedMonster = null;
    }

    public void OnClickFeed()
    {
        if (selectedMonster == null) return;

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager is missing!");
            return;
        }

        if (berryItemData == null)
        {
            Debug.LogWarning("Berry ItemData is missing on MonsterUIPanel!");
            return;
        }

        TinyMonsterTouch monster = selectedMonster;

        int berryCost = monster.BerryCostPerFeed;
        int friendshipGain = monster.FeedFriendshipGain;

        int currentBerry = InventoryManager.Instance.GetItemAmount(berryItemData);

        if (currentBerry < berryCost)
        {
            if (berryHUDCounter != null)
            {
                berryHUDCounter.PlayWarningFlash();
            }

            Debug.Log($"Không đủ berry! {monster.MonsterName} cần {berryCost}, hiện có {currentBerry}");
            return;
        }

        bool removed = InventoryManager.Instance.RemoveItem(berryItemData, berryCost);

        if (!removed)
        {
            if (berryHUDCounter != null)
            {
                berryHUDCounter.PlayWarningFlash();
            }

            Debug.Log("Remove berry failed!");
            return;
        }

        monster.AddFriendship(friendshipGain);

        UpdateInfo(monster);

        Hide(false);

        if (monster.Controller != null)
        {
            monster.Controller.ResumeAfterMenu();
            monster.Controller.PlayHappy();
        }

        Debug.Log($"Feed {monster.MonsterName}. Used {berryCost} berry. Berry left: {InventoryManager.Instance.GetItemAmount(berryItemData)}. Current Friendship: {monster.Friendship}");
    }

    public void OnClickPlay()
    {
        if (selectedMonster == null) return;

        TinyMonsterTouch monster = selectedMonster;
        double remainingSeconds = GetPlayCooldownRemaining(monster);
        if (remainingSeconds > 0d)
        {
            ShowNotice(GetText(playCooldownMessageKey, playCooldownMessageFallback, FormatDuration(remainingSeconds)));
            Hide(true);
            return;
        }

        monster.AddFriendship(playFriendshipGain);
        SaveNextPlayTime(monster);

        UpdateInfo(monster);

        Hide(false);

        if (monster.Controller != null)
        {
            monster.Controller.ResumeAfterMenu();
            monster.Controller.PlayHappy();
        }

        Debug.Log($"Play with {monster.MonsterName}. Current Friendship: {monster.Friendship}");
    }

    private double GetPlayCooldownRemaining(TinyMonsterTouch monster)
    {
        if (monster != null && monster.Controller != null)
        {
            long controllerNextPlayUnixSeconds = monster.Controller.NextPlayAtUnix;
            if (controllerNextPlayUnixSeconds <= 0L)
            {
                string legacyTicks = PlayerPrefs.GetString(GetPlayCooldownKey(monster), string.Empty);
                long.TryParse(legacyTicks, out controllerNextPlayUnixSeconds);
                if (controllerNextPlayUnixSeconds > 0L)
                    monster.Controller.SetNextPlayAtUnix(controllerNextPlayUnixSeconds);
            }

            return Math.Max(0d, controllerNextPlayUnixSeconds - DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        string key = GetPlayCooldownKey(monster);
        if (string.IsNullOrEmpty(key))
            return 0d;

        string savedTicks = PlayerPrefs.GetString(key, string.Empty);
        if (!long.TryParse(savedTicks, out long nextPlayUnixSeconds))
            return 0d;

        long nowUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return Math.Max(0d, nextPlayUnixSeconds - nowUnixSeconds);
    }

    private void SaveNextPlayTime(TinyMonsterTouch monster)
    {
        long nextPlayAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            + Mathf.Max(0, Mathf.RoundToInt(playCooldownSeconds));

        if (monster != null && monster.Controller != null)
        {
            monster.Controller.SetNextPlayAtUnix(nextPlayAtUnixSeconds);
            return;
        }

        string key = GetPlayCooldownKey(monster);
        if (string.IsNullOrEmpty(key))
            return;

        PlayerPrefs.SetString(key, nextPlayAtUnixSeconds.ToString());
        PlayerPrefs.Save();
    }

    private string GetPlayCooldownKey(TinyMonsterTouch monster)
    {
        if (monster == null)
            return string.Empty;

        string monsterId = MonsterCollectionManager.GetMonsterId(monster.Data);
        if (string.IsNullOrWhiteSpace(monsterId))
            monsterId = monster.MonsterName;

        return string.IsNullOrWhiteSpace(monsterId)
            ? string.Empty
            : PlayCooldownPrefix + monsterId.Trim();
    }

    private string FormatDuration(double totalSeconds)
    {
        TimeSpan duration = TimeSpan.FromSeconds(Math.Ceiling(totalSeconds));

        if (duration.TotalHours >= 1d)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";

        if (duration.TotalMinutes >= 1d)
            return $"{duration.Minutes}m {duration.Seconds}s";

        return $"{duration.Seconds}s";
    }

    private void ShowNotice(string message)
    {
        if (FogUnlockConfirmDialogUI.Instance != null)
        {
            FogUnlockConfirmDialogUI.Instance.ShowMessage(message);
            return;
        }

        Debug.Log(message);
    }

    private string GetText(GameTextKey key, string fallback, params object[] args)
    {
        if (textDatabase != null)
            return textDatabase.Get(key, fallback, args);

        return args == null || args.Length == 0 ? fallback : string.Format(fallback, args);
    }

    private void UpdateInfo(TinyMonsterTouch monster)
    {
        if (nameText != null)
        {
            nameText.text = monster.MonsterName;
        }

        if (friendshipBarFill != null)
        {
            float fillAmount = (float)monster.Friendship / monster.MaxFriendship;
            friendshipBarFill.fillAmount = fillAmount;
        }

        if (friendshipText != null)
        {
            friendshipText.text = $"{monster.Friendship}/{monster.MaxFriendship}";
        }

        if (feedCostText != null)
        {
            feedCostText.text = $"x{monster.BerryCostPerFeed}";
        }
    }
    private bool WasPointerPressedOutsidePanel()
    {
        if (Input.GetMouseButtonDown(0))
            return !IsScreenPointInsidePanel(Input.mousePosition);

        if (Input.touchCount <= 0)
            return false;

        Touch touch = Input.GetTouch(0);
        return touch.phase == TouchPhase.Began && !IsScreenPointInsidePanel(touch.position);
    }

    private bool IsScreenPointInsidePanel(Vector2 screenPoint)
    {
        // Check nếu click vào panel container
        if (RectTransformUtility.RectangleContainsScreenPoint(panelContainer, screenPoint, CanvasCamera))
            return true;

        // Check nếu click vào info panel
        if (infoPanel != null && RectTransformUtility.RectangleContainsScreenPoint(infoPanel, screenPoint, CanvasCamera))
            return true;

        // Check nếu click vào action menu panel
        if (actionMenuPanel != null && RectTransformUtility.RectangleContainsScreenPoint(actionMenuPanel, screenPoint, CanvasCamera))
            return true;

        return false;
    }
}
