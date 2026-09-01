using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TutorialObjectiveTracker : MonoBehaviour
{
    private const string StepKey = "tutorial.objectives.v1.step";
    private const string DoneKey = "tutorial.objectives.v1.done";
    private const string DailyDayKey = "daily.quest.day";
    private const string DailyProgressKey = "daily.quest.progress";
    private static readonly TutorialAction[] FallbackActions = {
        TutorialAction.HarvestResource, TutorialAction.CollectDrop,
        TutorialAction.OpenCooking, TutorialAction.StartCooking,
        TutorialAction.CollectCookedResult, TutorialAction.SummonMonster,
        TutorialAction.InteractMonster, TutorialAction.CollectCoin,
        TutorialAction.UnlockZone01
    };
    private static readonly string[] FallbackLabels = {
        "Harvest a Red Berry", "Collect the Red Berry",
        "Open the Cooking Pot", "Start cooking",
        "Collect your result", "Welcome your monster",
        "Tap a monster", "Collect a coin", "Unlock Zone01"
    };

    [Header("Scene UI - editable in GameplayScene")]
    [SerializeField] private TutorialObjectiveSequenceData objectiveSequence;
    [SerializeField] private DailyQuestSequenceData dailyQuestSequence;
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text goalLabel;
    [SerializeField] private Image dailyRewardImage;
    [SerializeField] private TMP_Text dailyRewardLabel;
    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Image completeImage;

    [Header("Objective Text Style")]
    [SerializeField] private Color normalTextColor = new Color(0.2f, 0.16f, 0.1f, 1f);
    [SerializeField] private Color normalOutlineColor = new Color(1f, 0.9f, 0.62f, 1f);
    [SerializeField] private Color completedTextColor = new Color(0.25f, 0.72f, 0.2f, 1f);
    [SerializeField] private Color completedOutlineColor = new Color(0.1f, 0.3f, 0.08f, 1f);
    [SerializeField, Range(0f, 1f)] private float outlineWidth = 0.2f;

    [Header("Scene Highlight - editable in GameplayScene")]
    [SerializeField] private SpriteRenderer highlightRing;
    [SerializeField] private SpriteRenderer highlightArrow;
    [SerializeField, Min(0.1f)] private float highlightDuration = 1.5f;

    private Sprite collapseSprite;
    private Sprite expandSprite;
    private Transform highlightTarget;
    private bool collapsed;
    private bool completingStep;
    private int step;
    private bool dailyMode;
    private int dailyQuestIndex;
    private int[] dailyProgress;
    private float nextTargetSearchTime;
    private float nextSavedZoneCheckTime;
    private float highlightVisibleUntil;
    private bool highlightShown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap() => SceneManager.sceneLoaded += CreateForGameplay;

    private static void CreateForGameplay(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "GameplayScene" || PlayerPrefs.GetInt(DoneKey, 0) != 0
            || FindObjectOfType<TutorialObjectiveTracker>() != null)
            return;
        new GameObject("TutorialObjectiveTracker").AddComponent<TutorialObjectiveTracker>();
    }

    private void Awake()
    {
        dailyMode = PlayerPrefs.GetInt(DoneKey, 0) != 0;
        if (dailyMode)
            PrepareDailyQuest();

        step = Mathf.Clamp(PlayerPrefs.GetInt(StepKey, 0), 0, ObjectiveCount - 1);
        TutorialSignal.Raised += HandleAction;
        BuildUi();
        if (completeImage != null)
            completeImage.gameObject.SetActive(false);
        if (label != null)
            label.gameObject.SetActive(true);
        if (goalLabel != null)
            goalLabel.gameObject.SetActive(true);
        ApplyTextStyle(false);
        BuildHighlight();
        Refresh();
        CompleteSavedZone01StepIfNeeded();
    }

    private void Update()
    {
        UpdateHighlight();
        if (!dailyMode && !completingStep && step >= ObjectiveCount - 1
            && Time.unscaledTime >= nextSavedZoneCheckTime)
        {
            nextSavedZoneCheckTime = Time.unscaledTime + 0.5f;
            CompleteSavedZone01StepIfNeeded();
        }
    }

    private void OnDestroy() => TutorialSignal.Raised -= HandleAction;

    private void HandleAction(TutorialAction action)
    {
        if (dailyMode)
        {
            HandleDailyAction(action);
            return;
        }
        if (completingStep || step >= ObjectiveCount || action != GetAction(step)) return;
        int reward = GetCoinReward(step);
        if (CurrencyManager.Instance != null && reward > 0)
            CurrencyManager.Instance.AddCoin(reward);
        StartCoroutine(CompleteAndAdvance());
    }

    private void CompleteSavedZone01StepIfNeeded()
    {
        if (dailyMode || completingStep || step < ObjectiveCount - 1)
            return;

        FogZoneManager fogManager = FindObjectOfType<FogZoneManager>();
        if (fogManager == null)
            return;

        foreach (string zoneId in fogManager.ExportUnlockedZoneIds())
        {
            if (string.Equals(zoneId, "1", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(zoneId, "zone_01", System.StringComparison.OrdinalIgnoreCase))
            {
                StartCoroutine(CompleteAndAdvance());
                return;
            }
        }
    }

    private System.Collections.IEnumerator CompleteAndAdvance()
    {
        completingStep = true;
        bool finishingTutorial = step >= ObjectiveCount - 1;
        ApplyTextStyle(true);
        if (completeImage != null)
            completeImage.gameObject.SetActive(true);

        if (finishingTutorial)
        {
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.DeleteKey(StepKey);
            PlayerPrefs.Save();
            dailyMode = true;
            PrepareDailyQuest();
            ApplyTextStyle(false);
            Refresh();
        }

        yield return new WaitForSecondsRealtime(0.55f);

        if (completeImage != null)
            completeImage.gameObject.SetActive(false);

        step++;
        if (finishingTutorial)
        {
            completingStep = false;
            yield break;
        }
        PlayerPrefs.SetInt(StepKey, step);
        PlayerPrefs.Save();
        ApplyTextStyle(false);
        Refresh();
        completingStep = false;
    }

    private void BuildUi()
    {
        collapseSprite = Resources.Load<Sprite>("Tutorial/collapse_button");
        expandSprite = Resources.Load<Sprite>("Tutorial/expand_button");
        if (panel != null && panelRect != null && label != null && toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(ToggleCollapsed);
            toggleButton.onClick.AddListener(ToggleCollapsed);
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        panel = new GameObject("ObjectivePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = new Vector2(0.20f, 1f);
        panelRect.anchorMax = new Vector2(0.96f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        float topInset = Screen.height > 0
            ? (Screen.height - Screen.safeArea.yMax) * ((RectTransform)canvas.transform).rect.height / Screen.height
            : 0f;
        panelRect.anchoredPosition = new Vector2(0f, -topInset - 300f);
        panelRect.sizeDelta = new Vector2(0f, 164f);
        panel.GetComponent<Image>().color = new Color(0.96f, 0.9f, 0.66f, 0.96f);

        Sprite frameSprite = Resources.Load<Sprite>("Tutorial/objective_panel");
        if (frameSprite != null)
        {
            GameObject frame = new GameObject("GeneratedFrame", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(panel.transform, false);
            RectTransform frameRect = (RectTransform)frame.transform;
            frameRect.anchorMin = Vector2.zero; frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero; frameRect.offsetMax = Vector2.zero;
            Image frameImage = frame.GetComponent<Image>();
            frameImage.sprite = frameSprite;
            frameImage.type = Image.Type.Sliced;
            frameImage.raycastTarget = false;
        }

        GameObject textObject = new GameObject("ObjectiveText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(34f, 16f); textRect.offsetMax = new Vector2(-104f, -78f);
        label = textObject.GetComponent<TextMeshProUGUI>();
        label.color = new Color(0.2f, 0.16f, 0.1f);
        label.fontSize = 40f; label.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject goalObject = new GameObject("GoalText", typeof(RectTransform), typeof(TextMeshProUGUI));
        goalObject.transform.SetParent(panel.transform, false);
        RectTransform goalRect = (RectTransform)goalObject.transform;
        goalRect.anchorMin = Vector2.zero; goalRect.anchorMax = Vector2.one;
        goalRect.offsetMin = new Vector2(34f, 88f); goalRect.offsetMax = new Vector2(-104f, -18f);
        goalLabel = goalObject.GetComponent<TextMeshProUGUI>();
        goalLabel.fontSize = 30f; goalLabel.alignment = TextAlignmentOptions.MidlineLeft;
        goalLabel.fontStyle = FontStyles.Normal;

        GameObject toggle = new GameObject("ObjectiveToggle", typeof(RectTransform), typeof(Image), typeof(Button));
        toggle.transform.SetParent(panel.transform, false);
        RectTransform toggleRect = (RectTransform)toggle.transform;
        toggleRect.anchorMin = new Vector2(1f, 0.5f); toggleRect.anchorMax = new Vector2(1f, 0.5f);
        toggleRect.pivot = new Vector2(1f, 0.5f); toggleRect.anchoredPosition = new Vector2(-18f, 0f);
        toggleRect.sizeDelta = new Vector2(72f, 72f);
        toggle.GetComponent<Image>().sprite = collapseSprite;
        toggleButton = toggle.GetComponent<Button>();
        toggleButton.onClick.AddListener(ToggleCollapsed);

        GameObject complete = new GameObject("CompleteCheck", typeof(RectTransform), typeof(Image));
        complete.transform.SetParent(panel.transform, false);
        RectTransform completeRect = (RectTransform)complete.transform;
        completeRect.anchorMin = new Vector2(0f, 0.5f);
        completeRect.anchorMax = new Vector2(0f, 0.5f);
        completeRect.anchoredPosition = new Vector2(44f, 0f);
        completeRect.sizeDelta = new Vector2(72f, 72f);
        completeImage = complete.GetComponent<Image>();
        completeImage.sprite = Resources.Load<Sprite>("Tutorial/complete_check");
        completeImage.raycastTarget = false;
        complete.SetActive(false);
    }

    private void BuildHighlight()
    {
        Sprite ringSprite = Resources.Load<Sprite>("Tutorial/highlight_ring");
        Sprite arrowSprite = Resources.Load<Sprite>("Tutorial/tutorial_arrow");
        if (ringSprite == null || arrowSprite == null)
            return;

        if (highlightRing != null && highlightArrow != null)
        {
            // Inspector assignments take priority over default Resources assets.
            if (highlightRing.sprite == null)
                highlightRing.sprite = ringSprite;
            highlightRing.sortingOrder = 1200;
            if (highlightArrow.sprite == null)
                highlightArrow.sprite = arrowSprite;
            highlightArrow.sortingOrder = 1201;
            highlightRing.gameObject.SetActive(false);
            highlightArrow.gameObject.SetActive(false);
            return;
        }

        GameObject ringObject = new GameObject("TutorialHighlightRing", typeof(SpriteRenderer));
        ringObject.transform.SetParent(transform, false);
        highlightRing = ringObject.GetComponent<SpriteRenderer>();
        highlightRing.sprite = ringSprite;
        highlightRing.sortingOrder = 1200;

        GameObject arrowObject = new GameObject("TutorialHighlightArrow", typeof(SpriteRenderer));
        arrowObject.transform.SetParent(transform, false);
        highlightArrow = arrowObject.GetComponent<SpriteRenderer>();
        highlightArrow.sprite = arrowSprite;
        highlightArrow.sortingOrder = 1201;
        ringObject.SetActive(false);
    }

    private void UpdateHighlight()
    {
        if (highlightRing == null || step >= ObjectiveCount)
            return;

        if (highlightTarget == null && Time.unscaledTime >= nextTargetSearchTime)
        {
            nextTargetSearchTime = Time.unscaledTime + 0.35f;
            highlightTarget = FindHighlightTarget(GetAction(step));
        }

        if (!highlightShown && highlightTarget != null && highlightTarget.gameObject.activeInHierarchy)
        {
            highlightShown = true;
            highlightVisibleUntil = Time.unscaledTime + highlightDuration;
        }

        bool visible = highlightTarget != null && highlightTarget.gameObject.activeInHierarchy
            && Time.unscaledTime < highlightVisibleUntil;
        highlightRing.gameObject.SetActive(visible);
        if (highlightArrow != null)
            highlightArrow.gameObject.SetActive(visible);
        if (!visible)
            return;

        Bounds bounds = GetTargetBounds(highlightTarget);
        float diameter = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.y) * 1.2f, 0.65f, 1.8f);
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.08f;
        highlightRing.transform.position = new Vector3(bounds.center.x, bounds.center.y, 0f);
        float ringSpriteDiameter = Mathf.Max(highlightRing.sprite.bounds.size.x, highlightRing.sprite.bounds.size.y);
        highlightRing.transform.localScale = Vector3.one * (diameter / ringSpriteDiameter) * pulse;
        if (highlightArrow != null)
        {
            float arrowWidth = Mathf.Max(0.01f, highlightArrow.sprite.bounds.size.x);
            highlightArrow.transform.localScale = Vector3.one * (0.42f / arrowWidth);
            highlightArrow.transform.position = new Vector3(
                bounds.center.x,
                bounds.center.y + diameter * 0.5f + 0.28f + Mathf.Sin(Time.unscaledTime * 5f) * 0.05f,
                0f);
        }
    }

    private void ApplyTextStyle(bool completed)
    {
        if (label == null)
            return;

        label.color = completed ? completedTextColor : normalTextColor;
        label.outlineColor = completed ? completedOutlineColor : normalOutlineColor;
        label.outlineWidth = outlineWidth;
        if (goalLabel != null)
        {
            goalLabel.color = Color.black;
            goalLabel.outlineColor = Color.black;
            goalLabel.outlineWidth = outlineWidth;
        }
    }

    private static Transform FindHighlightTarget(TutorialAction action)
    {
        if (action == TutorialAction.CollectDrop)
        {
            BerryDropController drop = FindObjectOfType<BerryDropController>();
            return drop != null ? drop.transform : null;
        }

        if (action == TutorialAction.InteractMonster || action == TutorialAction.CollectCoin)
        {
            TinyMonsterTouch monster = FindObjectOfType<TinyMonsterTouch>();
            return monster != null ? monster.transform : null;
        }

        if (action == TutorialAction.UnlockZone01)
        {
            GameObject fog = GameObject.Find("Zone01_Fog");
            Transform button = fog != null ? FindChildByName(fog.transform, "Button_Unlock") : null;
            return button != null ? button : fog != null ? fog.transform : null;
        }

        if (action == TutorialAction.HarvestResource)
        {
            BushController fallback = null;
            foreach (BushController bush in FindObjectsOfType<BushController>())
            {
                if (bush.FruitItemId != "berry")
                    continue;
                if (bush.IsReadyToHarvest)
                    return bush.transform;
                if (fallback == null)
                    fallback = bush;
            }
            return fallback != null ? fallback.transform : null;
        }

        CookingPotController pot = FindObjectOfType<CookingPotController>();
        return pot != null ? pot.transform : null;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        foreach (Transform child in root)
        {
            if (child.name == childName)
                return child;
            Transform match = FindChildByName(child, childName);
            if (match != null)
                return match;
        }
        return null;
    }

    private static Bounds GetTargetBounds(Transform target)
    {
        Renderer renderer = target.GetComponentInChildren<Renderer>();
        if (renderer != null)
            return renderer.bounds;
        Collider2D collider = target.GetComponentInChildren<Collider2D>();
        return collider != null ? collider.bounds : new Bounds(target.position, Vector3.one);
    }

    private void ToggleCollapsed()
    {
        collapsed = !collapsed;
        if (panel != null)
        {
            foreach (Transform child in panel.transform)
            {
                if (toggleButton != null && child.gameObject == toggleButton.gameObject)
                    continue;
                if (completeImage != null && child.gameObject == completeImage.gameObject)
                {
                    child.gameObject.SetActive(false);
                    continue;
                }
                child.gameObject.SetActive(!collapsed);
            }
        }
        if (toggleButton != null)
            toggleButton.gameObject.SetActive(true);
    }

    private void Refresh()
    {
        highlightTarget = null;
        nextTargetSearchTime = 0f;
        highlightShown = false;
        highlightVisibleUntil = 0f;
        if (label != null)
            label.text = dailyMode ? GetDailyLabel() : GetLabel(step);
        if (goalLabel != null)
        {
            if (dailyMode)
            {
                DailyQuestSequenceData.Quest quest = GetDailyQuest();
                goalLabel.text = "DAILY QUEST";
                if (dailyRewardImage != null) dailyRewardImage.gameObject.SetActive(quest != null);
                if (dailyRewardLabel != null) dailyRewardLabel.text = quest == null ? string.Empty : $"{quest.coinReward}";
            }
            else
            {
                int reward = GetCoinReward(step);
                goalLabel.text = $"GOAL {step + 1}/{ObjectiveCount}{(reward > 0 ? $"  +{reward} coin" : string.Empty)}";
                if (dailyRewardImage != null) dailyRewardImage.gameObject.SetActive(false);
                if (dailyRewardLabel != null) dailyRewardLabel.text = string.Empty;
            }
        }
    }

    private void PrepareDailyQuest()
    {
        int day = (int)DateTime.Now.DayOfWeek;
        if (PlayerPrefs.GetInt(DailyDayKey, -1) != day)
        {
            PlayerPrefs.SetInt(DailyDayKey, day);
            PlayerPrefs.SetInt(DailyProgressKey, 0);
            PlayerPrefs.Save();
        }
        dailyQuestIndex = dailyQuestSequence != null && dailyQuestSequence.Count > 0
            ? day % dailyQuestSequence.Count : 0;
        LoadDailyProgress();
    }

    private DailyQuestSequenceData.Quest GetDailyQuest() => dailyQuestSequence != null
        ? dailyQuestSequence.Get(dailyQuestIndex) : null;

    private void LoadDailyProgress()
    {
        DailyQuestSequenceData.Quest quest = GetDailyQuest();
        dailyProgress = quest == null ? new int[0] : new int[quest.goals.Count];
        string[] values = PlayerPrefs.GetString(DailyProgressKey, string.Empty).Split(',');
        for (int i = 0; i < dailyProgress.Length && i < values.Length; i++)
            int.TryParse(values[i], out dailyProgress[i]);
    }

    private void HandleDailyAction(TutorialAction action)
    {
        DailyQuestSequenceData.Quest quest = GetDailyQuest();
        if (quest == null || dailyProgress == null) return;
        bool changed = false;
        for (int i = 0; i < quest.goals.Count; i++)
            if (quest.goals[i].action == action && dailyProgress[i] < quest.goals[i].target)
            { dailyProgress[i]++; changed = true; }
        if (!changed) return;
        PlayerPrefs.SetString(DailyProgressKey, string.Join(",", dailyProgress));
        if (IsDailyComplete())
        {
            CurrencyManager.Instance?.AddCoin(quest.coinReward);
            PlayerPrefs.SetString(DailyProgressKey, string.Join(",", new int[quest.goals.Count]));
        }
        PlayerPrefs.Save();
        Refresh();
    }

    private bool IsDailyComplete()
    {
        DailyQuestSequenceData.Quest quest = GetDailyQuest();
        for (int i = 0; quest != null && i < quest.goals.Count; i++)
            if (dailyProgress[i] < quest.goals[i].target) return false;
        return quest != null && quest.goals.Count > 0;
    }

    private string GetDailyLabel()
    {
        DailyQuestSequenceData.Quest quest = GetDailyQuest();
        if (quest == null) return "Daily quest unavailable";
        for (int i = 0; i < quest.goals.Count; i++)
            if (dailyProgress[i] < quest.goals[i].target)
                return $"{quest.title}: {quest.goals[i].displayText} {dailyProgress[i]}/{quest.goals[i].target}";
        return $"{quest.title}: Complete!";
    }

    private int ObjectiveCount => objectiveSequence != null && objectiveSequence.Count > 0
        ? objectiveSequence.Count
        : FallbackActions.Length;

    private TutorialAction GetAction(int index)
    {
        TutorialObjectiveSequenceData.Objective objective = objectiveSequence != null
            ? objectiveSequence.Get(index)
            : null;
        return objective != null ? objective.action : FallbackActions[index];
    }

    private string GetLabel(int index)
    {
        TutorialObjectiveSequenceData.Objective objective = objectiveSequence != null
            ? objectiveSequence.Get(index)
            : null;
        return objective != null && !string.IsNullOrWhiteSpace(objective.displayText)
            ? objective.displayText
            : FallbackLabels[index];
    }

    private int GetCoinReward(int index)
    {
        TutorialObjectiveSequenceData.Objective objective = objectiveSequence != null
            ? objectiveSequence.Get(index)
            : null;
        return objective != null ? Mathf.Max(0, objective.coinReward) : 1;
    }
}
