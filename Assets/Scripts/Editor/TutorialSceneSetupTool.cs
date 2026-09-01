#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class TutorialSceneSetupTool
{
    private const string GameplaySceneName = "GameplayScene";
    private const string SessionKey = "TinyMonsterKeeper.TutorialSceneSetup.V9.DailyReward";
    private const string ObjectiveAssetPath = "Assets/ScriptableObjects/Tutorial/TutorialObjectiveSequence.asset";
    private const string DailyQuestAssetPath = "Assets/ScriptableObjects/Tutorial/DailyQuestSequence.asset";

    static TutorialSceneSetupTool()
    {
        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);
        EditorApplication.delayCall += SetupOpenGameplayScene;
    }

    [MenuItem("TinyMonsterKeeper/Tutorial/Setup Objective Tracker In Open Scene %#t")]
    public static void SetupOpenGameplayScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != GameplaySceneName)
            return;

        TutorialObjectiveTracker tracker = Object.FindObjectOfType<TutorialObjectiveTracker>(true);
        if (tracker == null)
        {
            GameObject trackerObject = new GameObject("TutorialObjectiveTracker");
            Undo.RegisterCreatedObjectUndo(trackerObject, "Setup tutorial objective tracker");
            tracker = trackerObject.AddComponent<TutorialObjectiveTracker>();
        }

        BuildEditableHierarchy(tracker);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = tracker.gameObject;
        Debug.Log("TutorialObjectiveTracker was added to GameplayScene. Press Play to test it.");
    }

    private static void BuildEditableHierarchy(TutorialObjectiveTracker tracker)
    {
        TutorialObjectiveSequenceData objectiveSequence = EnsureObjectiveSequenceAsset();
        DailyQuestSequenceData dailyQuestSequence = EnsureDailyQuestAsset();
        Canvas canvas = FindScreenCanvas();
        if (canvas == null)
        {
            Debug.LogError("Tutorial setup could not find a screen-space Canvas in GameplayScene.");
            return;
        }

        Transform existingPanel = canvas.transform.Find("ObjectivePanel");
        if (existingPanel != null)
        {
            TMP_Text existingGoalLabel = EnsureGoalLabel(existingPanel);
            Image existingRewardImage = EnsureDailyRewardImage(existingPanel);
            TMP_Text existingRewardLabel = EnsureDailyRewardLabel(existingPanel);
            Transform objectiveTransform = existingPanel.Find("ObjectiveText");
            if (objectiveTransform != null)
            {
                RectTransform objectiveRect = objectiveTransform as RectTransform;
                objectiveRect.offsetMin = new Vector2(34f, 16f);
                objectiveRect.offsetMax = new Vector2(-104f, -78f);
            }
            SerializedObject existingTracker = new SerializedObject(tracker);
            existingTracker.FindProperty("objectiveSequence").objectReferenceValue = objectiveSequence;
            existingTracker.FindProperty("dailyQuestSequence").objectReferenceValue = dailyQuestSequence;
            existingTracker.FindProperty("goalLabel").objectReferenceValue = existingGoalLabel;
            existingTracker.FindProperty("dailyRewardImage").objectReferenceValue = existingRewardImage;
            existingTracker.FindProperty("dailyRewardLabel").objectReferenceValue = existingRewardLabel;
            existingTracker.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tracker);
            return;
        }

        GameObject panel = CreateUiObject("ObjectivePanel", canvas.transform, typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.20f, 1f);
        panelRect.anchorMax = new Vector2(0.96f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -300f);
        panelRect.sizeDelta = new Vector2(0f, 164f);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0f);

        GameObject background = CreateUiObject("PanelBackground", panel.transform, typeof(Image));
        Stretch(background.GetComponent<RectTransform>(), new Vector2(20f, 16f), new Vector2(-20f, -16f));
        background.GetComponent<Image>().color = new Color(0.98f, 0.94f, 0.78f, 0f);

        GameObject frame = CreateUiObject("GeneratedFrame", panel.transform, typeof(Image));
        Stretch(frame.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        Image frameImage = frame.GetComponent<Image>();
        frameImage.sprite = Resources.Load<Sprite>("Tutorial/objective_panel");
        frameImage.type = Image.Type.Sliced;
        frameImage.raycastTarget = false;

        GameObject textObject = CreateUiObject("ObjectiveText", panel.transform, typeof(TextMeshProUGUI));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        Stretch(textRect, new Vector2(34f, 16f), new Vector2(-104f, -78f));
        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.color = new Color(0.2f, 0.16f, 0.1f);
        label.fontSize = 40f;
        label.alignment = TextAlignmentOptions.MidlineLeft;

        TMP_Text sceneGoalText = EnsureGoalLabel(panel.transform);
        Image sceneRewardImage = EnsureDailyRewardImage(panel.transform);
        TMP_Text sceneRewardLabel = EnsureDailyRewardLabel(panel.transform);

        GameObject complete = CreateUiObject("CompleteCheck", panel.transform, typeof(Image));
        RectTransform completeRect = complete.GetComponent<RectTransform>();
        completeRect.anchorMin = completeRect.anchorMax = new Vector2(0f, 0.5f);
        completeRect.anchoredPosition = new Vector2(44f, 0f);
        completeRect.sizeDelta = new Vector2(72f, 72f);
        Image completeImage = complete.GetComponent<Image>();
        completeImage.sprite = Resources.Load<Sprite>("Tutorial/complete_check");
        completeImage.raycastTarget = false;
        complete.SetActive(false);

        GameObject toggle = CreateUiObject("ObjectiveToggle", panel.transform, typeof(Image), typeof(Button));
        RectTransform toggleRect = toggle.GetComponent<RectTransform>();
        toggleRect.anchorMin = toggleRect.anchorMax = new Vector2(1f, 0.5f);
        toggleRect.pivot = new Vector2(1f, 0.5f);
        toggleRect.anchoredPosition = new Vector2(-18f, 0f);
        toggleRect.sizeDelta = new Vector2(72f, 72f);
        toggle.GetComponent<Image>().sprite = Resources.Load<Sprite>("Tutorial/collapse_button");

        SpriteRenderer ring = CreateWorldSprite("TutorialHighlightRing", tracker.transform, "Tutorial/highlight_ring", 1200);
        SpriteRenderer arrow = CreateWorldSprite("TutorialHighlightArrow", tracker.transform, "Tutorial/tutorial_arrow", 1201);
        ring.gameObject.SetActive(false);
        arrow.gameObject.SetActive(false);

        SerializedObject serialized = new SerializedObject(tracker);
        serialized.FindProperty("objectiveSequence").objectReferenceValue = objectiveSequence;
        serialized.FindProperty("dailyQuestSequence").objectReferenceValue = dailyQuestSequence;
        serialized.FindProperty("panel").objectReferenceValue = panel;
        serialized.FindProperty("panelRect").objectReferenceValue = panelRect;
        serialized.FindProperty("label").objectReferenceValue = label;
        serialized.FindProperty("goalLabel").objectReferenceValue = sceneGoalText;
        serialized.FindProperty("dailyRewardImage").objectReferenceValue = sceneRewardImage;
        serialized.FindProperty("dailyRewardLabel").objectReferenceValue = sceneRewardLabel;
        serialized.FindProperty("toggleButton").objectReferenceValue = toggle.GetComponent<Button>();
        serialized.FindProperty("completeImage").objectReferenceValue = completeImage;
        serialized.FindProperty("highlightRing").objectReferenceValue = ring;
        serialized.FindProperty("highlightArrow").objectReferenceValue = arrow;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TMP_Text EnsureGoalLabel(Transform panel)
    {
        Transform existing = panel.Find("GoalText");
        GameObject goalObject = existing != null ? existing.gameObject : CreateUiObject("GoalText", panel, typeof(TextMeshProUGUI));
        RectTransform goalRect = goalObject.GetComponent<RectTransform>();
        goalRect.anchorMin = Vector2.zero;
        goalRect.anchorMax = Vector2.one;
        goalRect.offsetMin = new Vector2(34f, 88f);
        goalRect.offsetMax = new Vector2(-104f, -18f);
        TextMeshProUGUI sceneGoalText = goalObject.GetComponent<TextMeshProUGUI>();
        sceneGoalText.fontSize = 30f;
        sceneGoalText.alignment = TextAlignmentOptions.MidlineLeft;
        sceneGoalText.fontStyle = FontStyles.Normal;
        sceneGoalText.color = Color.black;
        sceneGoalText.outlineColor = Color.black;
        sceneGoalText.outlineWidth = 0.2f;
        return sceneGoalText;
    }

    private static Image EnsureDailyRewardImage(Transform panel)
    {
        Transform existing = panel.Find("DailyRewardImage");
        GameObject go = existing != null ? existing.gameObject : CreateUiObject("DailyRewardImage", panel, typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.78f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 48f);
        rect.sizeDelta = new Vector2(28f, 28f);
        Image image = go.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arts/UI/coin16x16.png");
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text EnsureDailyRewardLabel(Transform panel)
    {
        Transform existing = panel.Find("DailyRewardText");
        GameObject go = existing != null ? existing.gameObject : CreateUiObject("DailyRewardText", panel, typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.86f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 48f);
        rect.sizeDelta = new Vector2(48f, 32f);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = Color.black;
        text.raycastTarget = false;
        return text;
    }

    private static TutorialObjectiveSequenceData EnsureObjectiveSequenceAsset()
    {
        TutorialObjectiveSequenceData asset = AssetDatabase.LoadAssetAtPath<TutorialObjectiveSequenceData>(ObjectiveAssetPath);
        if (asset != null)
            return asset;

        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Tutorial"))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Tutorial");

        asset = ScriptableObject.CreateInstance<TutorialObjectiveSequenceData>();
        AssetDatabase.CreateAsset(asset, ObjectiveAssetPath);

        TutorialAction[] actions = {
            TutorialAction.HarvestResource,
            TutorialAction.CollectDrop,
            TutorialAction.OpenCooking,
            TutorialAction.StartCooking,
            TutorialAction.CollectCookedResult,
            TutorialAction.SummonMonster,
            TutorialAction.InteractMonster,
            TutorialAction.CollectCoin,
            TutorialAction.UnlockZone01
        };
        string[] labels = {
            "Harvest a Red Berry",
            "Collect the Red Berry",
            "Open the Cooking Pot",
            "Start cooking",
            "Collect your result",
            "Welcome your monster",
            "Tap a monster",
            "Collect a coin",
            "Unlock Zone01"
        };

        SerializedObject serialized = new SerializedObject(asset);
        SerializedProperty objectives = serialized.FindProperty("objectives");
        objectives.arraySize = actions.Length;
        for (int i = 0; i < actions.Length; i++)
        {
            SerializedProperty objective = objectives.GetArrayElementAtIndex(i);
            objective.FindPropertyRelative("action").enumValueIndex = (int)actions[i];
            objective.FindPropertyRelative("displayText").stringValue = labels[i];
            objective.FindPropertyRelative("coinReward").intValue = 1;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        return asset;
    }

    private static DailyQuestSequenceData EnsureDailyQuestAsset()
    {
        DailyQuestSequenceData asset = AssetDatabase.LoadAssetAtPath<DailyQuestSequenceData>(DailyQuestAssetPath);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<DailyQuestSequenceData>();
        AssetDatabase.CreateAsset(asset, DailyQuestAssetPath);
        string[] titles = { "Garden Routine", "Cooking Day", "Monster Care", "Forager's Day", "Coin Keeper", "Recipe Hunt", "Weekly Garden" };
        TutorialAction[][] actions = {
            new[] { TutorialAction.HarvestResource, TutorialAction.StartCooking, TutorialAction.CollectCoin },
            new[] { TutorialAction.StartCooking, TutorialAction.CollectCookedResult, TutorialAction.InteractMonster },
            new[] { TutorialAction.InteractMonster, TutorialAction.SummonMonster, TutorialAction.CollectCoin },
            new[] { TutorialAction.HarvestResource, TutorialAction.CollectDrop, TutorialAction.HarvestResource },
            new[] { TutorialAction.CollectCoin, TutorialAction.CollectCoin, TutorialAction.InteractMonster },
            new[] { TutorialAction.StartCooking, TutorialAction.StartCooking, TutorialAction.CollectCookedResult },
            new[] { TutorialAction.HarvestResource, TutorialAction.StartCooking, TutorialAction.CollectCookedResult, TutorialAction.CollectCoin }
        };
        string[][] labels = {
            new[] { "Harvest resources", "Cook a recipe", "Collect monster coins" },
            new[] { "Start 2 recipes", "Collect 2 cooking results", "Interact with a monster" },
            new[] { "Interact with monsters", "Welcome a new monster", "Collect monster coins" },
            new[] { "Harvest different resources", "Collect item drops", "Harvest again" },
            new[] { "Collect stored coins", "Collect more coins", "Check on a monster" },
            new[] { "Start recipes", "Start another recipe", "Collect a cooking result" },
            new[] { "Harvest resources", "Cook a recipe", "Collect the result", "Collect coins" }
        };
        int[][] targets = {
            new[] { 5, 2, 3 }, new[] { 2, 2, 2 }, new[] { 3, 1, 3 }, new[] { 5, 2, 5 },
            new[] { 3, 5, 2 }, new[] { 2, 2, 2 }, new[] { 5, 2, 2, 3 }
        };
        SerializedObject serialized = new SerializedObject(asset);
        SerializedProperty quests = serialized.FindProperty("quests");
        quests.arraySize = titles.Length;
        for (int i = 0; i < titles.Length; i++)
        {
            SerializedProperty quest = quests.GetArrayElementAtIndex(i);
            quest.FindPropertyRelative("title").stringValue = titles[i];
            quest.FindPropertyRelative("coinReward").intValue = 5 + i;
            SerializedProperty goals = quest.FindPropertyRelative("goals");
            goals.arraySize = actions[i].Length;
            for (int j = 0; j < actions[i].Length; j++)
            {
                SerializedProperty goal = goals.GetArrayElementAtIndex(j);
                goal.FindPropertyRelative("action").enumValueIndex = (int)actions[i][j];
                goal.FindPropertyRelative("displayText").stringValue = labels[i][j];
                goal.FindPropertyRelative("target").intValue = targets[i][j];
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        return asset;
    }

    private static Canvas FindScreenCanvas()
    {
        Canvas fallback = null;
        foreach (Canvas canvas in Object.FindObjectsOfType<Canvas>(true))
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
                continue;
            if (fallback == null || canvas.sortingOrder > fallback.sortingOrder)
                fallback = canvas;
        }
        return fallback;
    }

    private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        foreach (System.Type component in components)
            gameObject.AddComponent(component);
        Undo.RegisterCreatedObjectUndo(gameObject, "Create " + name);
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static SpriteRenderer CreateWorldSprite(string name, Transform parent, string resourcePath, int order)
    {
        Transform existing = parent.Find(name);
        GameObject gameObject = existing != null ? existing.gameObject : new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>(resourcePath);
        renderer.sortingOrder = order;
        return renderer;
    }

    [MenuItem("TinyMonsterKeeper/Tutorial/Reset Tutorial Progress %#y")]
    public static void ResetTutorialProgress()
    {
        PlayerPrefs.DeleteKey("tutorial.objectives.v1.step");
        PlayerPrefs.DeleteKey("tutorial.objectives.v1.done");
        PlayerPrefs.Save();
        Debug.Log("Tutorial objective progress was reset. Enter Play Mode from GameplayScene to test from step 1.");
    }
}
#endif
