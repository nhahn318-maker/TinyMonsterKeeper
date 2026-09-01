using System.IO;
using System.Linq;
using UnityEditor.Animations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinyMonsterKeeper.EditorAutomation
{
    public static class UnityCliTasks
    {
        [MenuItem("TinyMonsterKeeper/Automation/Setup Main Menu Settings UI")]
        public static void SetupMainMenuSettingsUI()
        {
            GameObject settingsRoot = GameObject.Find("SettingUI");
            if (settingsRoot == null)
            {
                Debug.LogError("Settings setup failed: SettingUI is missing.");
                return;
            }

            Transform panel = FindDescendant(settingsRoot.transform, "PanelSetting");
            Transform settingButton = FindSceneTransform("Button_Setting");
            Transform backButton = FindDescendant(settingsRoot.transform, "BackButton");
            Transform musicButton = FindDescendant(settingsRoot.transform, "Music_Button");
            Transform sfxButton = FindDescendant(settingsRoot.transform, "SFX_Button");
            Transform alertsRow = FindDescendant(settingsRoot.transform, "Notifications");
            Transform alertsButton = FindDescendant(settingsRoot.transform, "Notifications_Button")
                ?? FindVisibleButtonChild(alertsRow);

            if (panel == null || settingButton == null || backButton == null || musicButton == null || sfxButton == null || alertsButton == null)
            {
                Debug.LogError("Settings setup failed: one or more required setting objects are missing.");
                return;
            }

            Sprite settingNormal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arts/SettingUI/setting_icon.png");
            Sprite settingPressed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arts/SettingUI/setting_icon_click.png");
            Sprite backNormal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arts/SettingUI/button_back.png");
            Sprite backPressed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arts/SettingUI/button_back_click.png");
            Sprite onSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arts/SettingUI/button_on.png");
            Sprite offSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arts/SettingUI/button_off.png");

            Button open = ConfigureSpriteSwapButton(settingButton.gameObject, settingNormal, settingPressed);
            Button back = ConfigureSpriteSwapButton(backButton.gameObject, backNormal, backPressed);
            Image music = ConfigureToggleButton(musicButton.gameObject, onSprite);
            Image sfx = ConfigureToggleButton(sfxButton.gameObject, onSprite);
            Image alerts = ConfigureToggleButton(alertsButton.gameObject, onSprite);
            GameObject dimBlocker = GetOrCreateSettingsDimBlocker(settingsRoot.transform, panel);

            MainMenuSettingsUI controller = settingsRoot.GetComponent<MainMenuSettingsUI>();
            if (controller == null)
                controller = settingsRoot.AddComponent<MainMenuSettingsUI>();

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("panelSetting").objectReferenceValue = panel.gameObject;
            serializedController.FindProperty("dimBlocker").objectReferenceValue = dimBlocker;
            serializedController.FindProperty("openButton").objectReferenceValue = open;
            serializedController.FindProperty("backButton").objectReferenceValue = back;
            serializedController.FindProperty("musicImage").objectReferenceValue = music;
            serializedController.FindProperty("sfxImage").objectReferenceValue = sfx;
            serializedController.FindProperty("alertsImage").objectReferenceValue = alerts;
            serializedController.FindProperty("onSprite").objectReferenceValue = onSprite;
            serializedController.FindProperty("offSprite").objectReferenceValue = offSprite;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(settingsRoot);
            EditorSceneManager.MarkSceneDirty(settingsRoot.scene);
            EditorSceneManager.SaveScene(settingsRoot.scene);
            Debug.Log("Main menu settings UI setup finished.");
        }

        private static Button ConfigureSpriteSwapButton(GameObject target, Sprite normal, Sprite pressed)
        {
            Image image = target.GetComponent<Image>() ?? target.AddComponent<Image>();
            image.sprite = normal;
            Button button = target.GetComponent<Button>() ?? target.AddComponent<Button>();
            button.transition = Selectable.Transition.SpriteSwap;
            button.targetGraphic = image;
            SpriteState state = button.spriteState;
            state.pressedSprite = pressed;
            state.selectedSprite = pressed;
            button.spriteState = state;
            return button;
        }

        private static Image ConfigureToggleButton(GameObject target, Sprite onSprite)
        {
            Image image = target.GetComponent<Image>() ?? target.AddComponent<Image>();
            image.sprite = onSprite;
            Button button = target.GetComponent<Button>() ?? target.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            return image;
        }

        private static GameObject GetOrCreateSettingsDimBlocker(Transform settingsRoot, Transform panel)
        {
            Transform existing = settingsRoot.Find("DimBlocker");
            GameObject dimBlocker = existing != null ? existing.gameObject : new GameObject("DimBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = dimBlocker.GetComponent<RectTransform>();
            rect.SetParent(settingsRoot, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            Image image = dimBlocker.GetComponent<Image>();
            image.sprite = null;
            image.color = new Color(0f, 0f, 0f, 0.5f);
            image.raycastTarget = true;

            if (panel != null)
            {
                panel.SetAsLastSibling();
                rect.SetSiblingIndex(Mathf.Max(0, panel.GetSiblingIndex() - 1));
            }

            dimBlocker.SetActive(false);
            return dimBlocker;
        }

        private static Transform FindSceneTransform(string objectName)
        {
            GameObject gameObject = GameObject.Find(objectName);
            return gameObject != null ? gameObject.transform : null;
        }

        private static Transform FindDescendant(Transform root, string objectName)
        {
            if (root == null)
                return null;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                    return child;
            }

            return null;
        }

        private static Transform FindVisibleButtonChild(Transform root)
        {
            if (root == null)
                return null;

            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.transform == root)
                    continue;

                Image image = button.GetComponent<Image>();
                if (image != null && image.color.a > 0f)
                    return button.transform;
            }

            return null;
        }

        [MenuItem("TinyMonsterKeeper/Automation/Setup Main Menu Title Intro")]
        public static void SetupMainMenuTitleIntro()
        {
            RepairMainMenuTitleAnimation();

            GameObject gameTitle = GameObject.Find("GameTitle");
            if (gameTitle == null)
            {
                Debug.LogError("Main menu title intro setup failed: GameTitle is missing.");
                return;
            }

            if (gameTitle.GetComponent<CanvasGroup>() == null)
                gameTitle.AddComponent<CanvasGroup>();

            MainMenuTitleIntro intro = gameTitle.GetComponent<MainMenuTitleIntro>();
            if (intro == null)
                intro = gameTitle.AddComponent<MainMenuTitleIntro>();

            SerializedObject serializedIntro = new SerializedObject(intro);
            serializedIntro.FindProperty("introDuration").floatValue = 2.2f;
            serializedIntro.FindProperty("startOffsetY").floatValue = 140f;
            serializedIntro.FindProperty("buttonStartDelay").floatValue = 2.2f;
            GameObject guestButton = GameObject.Find("ButtonGuest");
            GameObject googleButton = GameObject.Find("ButtonGoogle");
            if (guestButton != null && googleButton != null)
            {
                CanvasGroup guestCanvasGroup = guestButton.GetComponent<CanvasGroup>();
                if (guestCanvasGroup == null)
                    guestCanvasGroup = guestButton.AddComponent<CanvasGroup>();

                CanvasGroup googleCanvasGroup = googleButton.GetComponent<CanvasGroup>();
                if (googleCanvasGroup == null)
                    googleCanvasGroup = googleButton.AddComponent<CanvasGroup>();

                SerializedProperty buttonRects = serializedIntro.FindProperty("buttonRects");
                buttonRects.arraySize = 2;
                buttonRects.GetArrayElementAtIndex(0).objectReferenceValue = guestButton.GetComponent<RectTransform>();
                buttonRects.GetArrayElementAtIndex(1).objectReferenceValue = googleButton.GetComponent<RectTransform>();

                SerializedProperty buttonGroups = serializedIntro.FindProperty("buttonCanvasGroups");
                buttonGroups.arraySize = 2;
                buttonGroups.GetArrayElementAtIndex(0).objectReferenceValue = guestCanvasGroup;
                buttonGroups.GetArrayElementAtIndex(1).objectReferenceValue = googleCanvasGroup;
            }
            serializedIntro.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(gameTitle);
            EditorSceneManager.MarkSceneDirty(gameTitle.scene);
            EditorSceneManager.SaveScene(gameTitle.scene);
            Debug.Log("Main menu title intro setup finished.");
        }

        [MenuItem("TinyMonsterKeeper/Automation/Repair Main Menu Title Animation")]
        public static void RepairMainMenuTitleAnimation()
        {
            const string titleAnimationPath = "Assets/Animations/UI/MainMenuTitleIdle.anim";
            const string titleSpriteSheetPath = "Assets/Arts/MainMenu/namegame_256x256_animation.png";

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(titleAnimationPath);
            if (clip == null)
            {
                Debug.LogError("Main menu title animation is missing: " + titleAnimationPath);
                return;
            }

            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(titleSpriteSheetPath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name)
                .ToArray();

            if (sprites.Length == 0)
            {
                Debug.LogError("Main menu title sprites are missing: " + titleSpriteSheetPath);
                return;
            }

            foreach (EditorCurveBinding existingBinding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, existingBinding, null);

            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("Image", typeof(Image), "m_Sprite");
            ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                frames[i] = new ObjectReferenceKeyframe
                {
                    time = i / 8f,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
            clip.frameRate = 8f;
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            Debug.Log("Main menu title animation binding repaired. Play the MainMenuScene to verify it.");
        }

        public static void SetupMainMenu()
        {
            const string scenePath = "Assets/Scenes/MainMenuScene.unity";
            const string titleControllerPath = "Assets/Animators/UI/MainMenuTitleController.controller";
            const string guestNormalSpritePath = "Assets/Arts/MainMenu/GuestButton_256x256.png";
            const string guestPressedSpritePath = "Assets/Arts/MainMenu/GuestButton_256x256_Click.png";
            const string googleNormalSpritePath = "Assets/Arts/MainMenu/GoogleButton_256x256.png";
            const string googlePressedSpritePath = "Assets/Arts/MainMenu/GoogleButton_256x256_Click.png";

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            AnimatorController titleController = AssetDatabase.LoadAssetAtPath<AnimatorController>(titleControllerPath);
            Sprite guestNormalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(guestNormalSpritePath);
            Sprite guestPressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(guestPressedSpritePath);
            Sprite googleNormalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(googleNormalSpritePath);
            Sprite googlePressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(googlePressedSpritePath);

            if (titleController == null || guestNormalSprite == null || guestPressedSprite == null ||
                googleNormalSprite == null || googlePressedSprite == null)
            {
                Debug.LogError("Main menu setup failed because a required animation or button sprite is missing.");
                EditorApplication.Exit(1);
                return;
            }

            GameObject gameTitle = GameObject.Find("GameTitle");
            if (gameTitle == null)
            {
                Debug.LogError("Main menu setup failed: GameTitle is missing.");
                EditorApplication.Exit(1);
                return;
            }

            Animator titleAnimator = gameTitle.GetComponent<Animator>();
            if (titleAnimator == null)
                titleAnimator = gameTitle.AddComponent<Animator>();
            titleAnimator.runtimeAnimatorController = titleController;
            EditorUtility.SetDirty(titleAnimator);

            SetupMenuButton("ButtonGuest", guestNormalSprite, guestPressedSprite, true);
            SetupMenuButton("ButtonGoogle", googleNormalSprite, googlePressedSprite, false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/MainMenuScene.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/GameplayScene.unity", true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("Main menu setup finished.");
        }

        private static void SetupMenuButton(string objectName, Sprite normalSprite, Sprite pressedSprite, bool isGuestButton)
        {
            GameObject buttonObject = GameObject.Find(objectName);
            if (buttonObject == null)
                throw new System.InvalidOperationException("Main menu setup failed: " + objectName + " is missing.");

            Image image = buttonObject.GetComponent<Image>();
            if (image == null)
                image = buttonObject.AddComponent<Image>();
            image.sprite = normalSprite;

            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
                button = buttonObject.AddComponent<Button>();
            button.transition = Selectable.Transition.SpriteSwap;
            button.targetGraphic = image;

            SpriteState spriteState = button.spriteState;
            spriteState.highlightedSprite = normalSprite;
            spriteState.pressedSprite = pressedSprite;
            spriteState.selectedSprite = pressedSprite;
            spriteState.disabledSprite = null;
            button.spriteState = spriteState;

            if (isGuestButton && buttonObject.GetComponent<MainMenuGuestButton>() == null)
                buttonObject.AddComponent<MainMenuGuestButton>();

            EditorUtility.SetDirty(buttonObject);
        }

        public static void ValidateProject()
        {
            int issueCount = 0;

            issueCount += RequireFile("Assets/google-services.json", "Firebase config is missing.");
            issueCount += RequireAsset("Assets/Scenes", "Scenes folder is missing.");
            issueCount += RequireAsset("Assets/ScriptableObjects", "ScriptableObjects folder is missing.");

            Debug.Log($"Unity CLI validation finished. Issues: {issueCount}");

            if (issueCount > 0)
                EditorApplication.Exit(1);
        }

        [MenuItem("TinyMonsterKeeper/Automation/Setup Save Runtime Binder")]
        public static void SetupSaveRuntimeBinder()
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject saveSystem = GameObject.Find("SaveSystem");
            if (saveSystem == null)
            {
                saveSystem = new GameObject("SaveSystem");
                Undo.RegisterCreatedObjectUndo(saveSystem, "Create SaveSystem");
            }

            SaveSystemBootstrap bootstrap = saveSystem.GetComponent<SaveSystemBootstrap>();
            if (bootstrap == null)
                bootstrap = saveSystem.AddComponent<SaveSystemBootstrap>();

            SaveGameRuntimeBinder binder = saveSystem.GetComponent<SaveGameRuntimeBinder>();
            if (binder == null)
                binder = saveSystem.AddComponent<SaveGameRuntimeBinder>();

            SaveAccountResetTool resetTool = saveSystem.GetComponent<SaveAccountResetTool>();
            if (resetTool == null)
                resetTool = saveSystem.AddComponent<SaveAccountResetTool>();

            SerializedObject serializedBinder = new SerializedObject(binder);
            AssignObjectArray<ItemData>(serializedBinder.FindProperty("itemDatabase"), "Assets/ScriptableObjects/ItemData");
            AssignObjectArray<MonsterData>(serializedBinder.FindProperty("monsterDatabase"), "Assets/ScriptableObjects/MonsterData");

            serializedBinder.FindProperty("loadSaveOnStart").boolValue = true;
            serializedBinder.FindProperty("autosaveOnChange").boolValue = true;
            serializedBinder.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(saveSystem);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Save runtime binder setup finished.");
        }

        [MenuItem("TinyMonsterKeeper/Automation/Setup Fog Unlock Visuals")]
        public static void SetupFogUnlockVisuals()
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";
            const string unlockSpritePath = "Assets/Arts/UI/padlock_unlock.png";

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Sprite unlockSprite = AssetDatabase.LoadAssetAtPath<Sprite>(unlockSpritePath);
            if (unlockSprite == null)
            {
                Debug.LogError("padlock_unlock sprite is missing. Path: " + unlockSpritePath);
                EditorApplication.Exit(1);
                return;
            }

            FogZoneManager fogZoneManager = Object.FindObjectOfType<FogZoneManager>();
            if (fogZoneManager == null)
            {
                Debug.LogError("FogZoneManager is missing in scene.");
                EditorApplication.Exit(1);
                return;
            }

            SerializedObject serializedManager = new SerializedObject(fogZoneManager);
            SerializedProperty zones = serializedManager.FindProperty("zones");
            for (int i = 0; i < zones.arraySize; i++)
            {
                SerializedProperty zone = zones.GetArrayElementAtIndex(i);
                zone.FindPropertyRelative("unlockedButtonSprite").objectReferenceValue = unlockSprite;
                zone.FindPropertyRelative("unlockVisualDuration").floatValue = 0.25f;
            }

            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fogZoneManager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Fog unlock visuals setup finished.");
        }

        [MenuItem("TinyMonsterKeeper/Automation/Setup Zones 06-13 Fog Unlock")]
        public static void SetupZones06To13FogUnlock()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded || scene.path != "Assets/Scenes/GameplayScene.unity")
            {
                Debug.LogError("Open GameplayScene before running the Zone 06-13 fog setup.");
                return;
            }

            FogZoneManager manager = Object.FindObjectOfType<FogZoneManager>();
            if (manager == null)
            {
                Debug.LogError("FogZoneManager is missing in GameplayScene.");
                return;
            }

            SerializedObject serializedManager = new SerializedObject(manager);
            SerializedProperty zones = serializedManager.FindProperty("zones");
            Sprite unlockedSprite = zones.arraySize > 0
                ? zones.GetArrayElementAtIndex(0).FindPropertyRelative("unlockedButtonSprite").objectReferenceValue as Sprite
                : AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arts/UI/padlock_unlock.png");

            int configured = 0;
            for (int zoneNumber = 6; zoneNumber <= 13; zoneNumber++)
            {
                string fogName = $"Zone{zoneNumber:D2}_Fog";
                GameObject fogObject = GameObject.Find(fogName);
                if (fogObject == null)
                {
                    Debug.LogWarning($"Skipped Zone {zoneNumber:D2}: {fogName} is missing.");
                    continue;
                }

                FogTilemapRevealController reveal = fogObject.GetComponent<FogTilemapRevealController>();
                FogAreaBlocker[] blockers = fogObject.GetComponents<FogAreaBlocker>();
                Transform unlockTransform = FindDescendant(fogObject.transform, "Button_Unlock");
                Collider2D unlockCollider = unlockTransform != null
                    ? unlockTransform.GetComponent<Collider2D>()
                    : null;

                if (reveal == null || unlockCollider == null)
                {
                    Debug.LogWarning($"Skipped Zone {zoneNumber:D2}: missing FogTilemapRevealController or Button_Unlock Collider2D.");
                    continue;
                }

                string zoneId = $"zone_{zoneNumber:D2}";
                SerializedProperty zoneProperty = FindFogZoneById(zones, zoneId);
                if (zoneProperty == null)
                {
                    int index = zones.arraySize;
                    zones.InsertArrayElementAtIndex(index);
                    zoneProperty = zones.GetArrayElementAtIndex(index);
                }

                zoneProperty.FindPropertyRelative("zoneName").stringValue = zoneId;
                zoneProperty.FindPropertyRelative("unlockCost").intValue = 0;
                zoneProperty.FindPropertyRelative("uiButton").objectReferenceValue = null;
                zoneProperty.FindPropertyRelative("mapButtonCollider").objectReferenceValue = unlockCollider;
                zoneProperty.FindPropertyRelative("unlockedButtonSprite").objectReferenceValue = unlockedSprite;
                zoneProperty.FindPropertyRelative("unlockVisualDuration").floatValue = 0.45f;
                zoneProperty.FindPropertyRelative("fogReveal").objectReferenceValue = reveal;
                zoneProperty.FindPropertyRelative("revealBounds").objectReferenceValue = null;
                zoneProperty.FindPropertyRelative("revealWholeTilemap").boolValue = true;
                zoneProperty.FindPropertyRelative("revealDirection").enumValueIndex = GetRevealDirection(zoneNumber);
                zoneProperty.FindPropertyRelative("revealDistance").floatValue = 0.75f;
                zoneProperty.FindPropertyRelative("customRevealDriftOffset").vector3Value = Vector3.zero;
                zoneProperty.FindPropertyRelative("hideButtonOnUnlock").boolValue = true;

                SerializedProperty blockerProperty = zoneProperty.FindPropertyRelative("monsterBlockers");
                blockerProperty.arraySize = blockers.Length;
                for (int blockerIndex = 0; blockerIndex < blockers.Length; blockerIndex++)
                    blockerProperty.GetArrayElementAtIndex(blockerIndex).objectReferenceValue = blockers[blockerIndex];

                configured++;
            }

            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Fog unlock setup finished: configured {configured} zones (Zone06-Zone13).");
        }

        [MenuItem("TinyMonsterKeeper/Automation/Repair Zone 05 Fog Unlock")]
        public static void RepairZone05FogUnlock()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded || scene.path != "Assets/Scenes/GameplayScene.unity")
            {
                Debug.LogError("Open GameplayScene before repairing Zone 05.");
                return;
            }

            FogZoneManager manager = Object.FindObjectOfType<FogZoneManager>();
            GameObject fogObject = GameObject.Find("Zone05_Fog");
            if (manager == null || fogObject == null)
            {
                Debug.LogError("FogZoneManager or Zone05_Fog is missing in GameplayScene.");
                return;
            }

            FogTilemapRevealController reveal = fogObject.GetComponent<FogTilemapRevealController>();
            FogAreaBlocker[] blockers = fogObject.GetComponents<FogAreaBlocker>();
            Transform unlockTransform = FindDescendant(fogObject.transform, "Button_Unlock");
            Collider2D unlockCollider = unlockTransform != null
                ? unlockTransform.GetComponent<Collider2D>()
                : null;

            if (reveal == null || blockers.Length == 0 || unlockCollider == null)
            {
                Debug.LogError("Zone05_Fog needs FogTilemapRevealController, FogAreaBlocker, and a Button_Unlock Collider2D.");
                return;
            }

            Undo.RecordObject(manager, "Repair Zone 05 Fog Unlock");
            SerializedObject serializedManager = new SerializedObject(manager);
            SerializedProperty zones = serializedManager.FindProperty("zones");
            SerializedProperty zone = FindFogZoneById(zones, "zone_05");
            if (zone == null)
            {
                Debug.LogError("FogZoneManager does not contain the zone_05 entry.");
                return;
            }

            zone.FindPropertyRelative("uiButton").objectReferenceValue = null;
            zone.FindPropertyRelative("mapButtonCollider").objectReferenceValue = unlockCollider;
            zone.FindPropertyRelative("fogReveal").objectReferenceValue = reveal;
            zone.FindPropertyRelative("revealBounds").objectReferenceValue = null;
            zone.FindPropertyRelative("revealWholeTilemap").boolValue = true;
            zone.FindPropertyRelative("revealDirection").enumValueIndex = 1; // Left
            zone.FindPropertyRelative("revealDistance").floatValue = 0.75f;
            zone.FindPropertyRelative("customRevealDriftOffset").vector3Value = Vector3.zero;
            zone.FindPropertyRelative("hideButtonOnUnlock").boolValue = true;

            SerializedProperty blockerProperty = zone.FindPropertyRelative("monsterBlockers");
            blockerProperty.arraySize = blockers.Length;
            for (int index = 0; index < blockers.Length; index++)
                blockerProperty.GetArrayElementAtIndex(index).objectReferenceValue = blockers[index];

            serializedManager.ApplyModifiedProperties();
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = fogObject;
            Debug.Log("Zone 05 fog unlock references repaired. Test in Play Mode, then save GameplayScene if correct.");
        }

        private static SerializedProperty FindFogZoneById(SerializedProperty zones, string zoneId)
        {
            for (int index = 0; index < zones.arraySize; index++)
            {
                SerializedProperty candidate = zones.GetArrayElementAtIndex(index);
                if (candidate.FindPropertyRelative("zoneName").stringValue == zoneId)
                    return candidate;
            }

            return null;
        }

        private static int GetRevealDirection(int zoneNumber)
        {
            // Fog drifts outward from the hub: left, right, or north for each new branch.
            if (zoneNumber == 9)
                return 2; // Up

            return zoneNumber == 6 || zoneNumber == 12 || zoneNumber == 13
                ? 1 // Left
                : 0; // Right
        }

        [MenuItem("TinyMonsterKeeper/Automation/Setup Selected Fence Y Sorting")]
        public static void SetupSelectedFenceYSorting()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogError("Select the fence object in the Hierarchy before running this setup.");
                return;
            }

            Transform fenceRoot = selected.transform;
            string selectedName = selected.name.ToLowerInvariant();
            if ((selectedName.Contains("top") || selectedName.Contains("down") || selectedName.Contains("bottom"))
                && fenceRoot.parent != null
                && fenceRoot.parent.name.ToLowerInvariant().Contains("fence"))
            {
                fenceRoot = fenceRoot.parent;
            }

            if (!fenceRoot.name.ToLowerInvariant().Contains("fence"))
            {
                Debug.LogError($"Selected object '{fenceRoot.name}' is not a fence. Select the fence root and run the setup again.");
                return;
            }

            Sprite topSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arts/ResourcesNode/Enviroment/fence_top.png");
            Sprite downSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arts/ResourcesNode/Enviroment/fencedown.png");
            if (topSprite == null || downSprite == null)
            {
                Debug.LogError("Fence setup failed: fence_top.png or fencedown.png could not be loaded.");
                return;
            }

            SpriteRenderer sourceRenderer = fenceRoot.GetComponent<SpriteRenderer>();
            Transform top = GetOrCreateFenceLayer(fenceRoot, "Fence_Top");
            Transform down = GetOrCreateFenceLayer(fenceRoot, "Fence_Down");

            ConfigureFenceLayer(top, topSprite, sourceRenderer, 0.2f);
            ConfigureFenceLayer(down, downSprite, sourceRenderer, -0.2f);

            if (sourceRenderer != null && sourceRenderer.enabled)
            {
                Undo.RecordObject(sourceRenderer, "Disable original fence renderer");
                sourceRenderer.enabled = false;
                EditorUtility.SetDirty(sourceRenderer);
            }

            YSortByPosition rootYSort = fenceRoot.GetComponent<YSortByPosition>();
            if (rootYSort != null && rootYSort.enabled)
            {
                Undo.RecordObject(rootYSort, "Disable fence root Y sort");
                rootYSort.enabled = false;
                EditorUtility.SetDirty(rootYSort);
            }

            SortingGroup rootSortingGroup = fenceRoot.GetComponent<SortingGroup>();
            if (rootSortingGroup != null && rootSortingGroup.enabled)
            {
                Undo.RecordObject(rootSortingGroup, "Disable fence root sorting group");
                rootSortingGroup.enabled = false;
                EditorUtility.SetDirty(rootSortingGroup);
            }

            EditorSceneManager.MarkSceneDirty(fenceRoot.gameObject.scene);
            Selection.activeGameObject = fenceRoot.gameObject;
            Debug.Log($"Fence Y sorting configured on '{fenceRoot.name}'. Verify the overlap, then save the scene. Ctrl+Z will undo the setup.");
        }

        private static Transform GetOrCreateFenceLayer(Transform root, string layerName)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name.Equals(layerName, System.StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            GameObject layer = new GameObject(layerName);
            Undo.RegisterCreatedObjectUndo(layer, "Create " + layerName);
            layer.transform.SetParent(root, false);
            layer.transform.localPosition = Vector3.zero;
            layer.transform.localRotation = Quaternion.identity;
            layer.transform.localScale = Vector3.one;
            return layer.transform;
        }

        private static void ConfigureFenceLayer(Transform layer, Sprite sprite, SpriteRenderer sourceRenderer, float sortYOffset)
        {
            SpriteRenderer renderer = layer.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = Undo.AddComponent<SpriteRenderer>(layer.gameObject);

            Undo.RecordObject(renderer, "Configure fence renderer");
            renderer.sprite = sprite;
            if (sourceRenderer != null)
            {
                renderer.sharedMaterial = sourceRenderer.sharedMaterial;
                renderer.color = sourceRenderer.color;
                renderer.flipX = sourceRenderer.flipX;
                renderer.flipY = sourceRenderer.flipY;
                renderer.sortingLayerID = sourceRenderer.sortingLayerID;
            }

            renderer.enabled = true;

            SortingGroup childSortingGroup = layer.GetComponent<SortingGroup>();
            if (childSortingGroup != null && childSortingGroup.enabled)
            {
                Undo.RecordObject(childSortingGroup, "Disable fence layer sorting group");
                childSortingGroup.enabled = false;
            }

            YSortByPosition ySort = layer.GetComponent<YSortByPosition>();
            if (ySort == null)
                ySort = Undo.AddComponent<YSortByPosition>(layer.gameObject);

            Undo.RecordObject(ySort, "Configure fence Y sort");
            SerializedObject serializedYSort = new SerializedObject(ySort);
            serializedYSort.FindProperty("sortPoint").objectReferenceValue = layer;
            serializedYSort.FindProperty("sortYOffset").floatValue = sortYOffset;
            serializedYSort.FindProperty("worldBaseOrder").intValue = 500;
            serializedYSort.FindProperty("baseOrder").intValue = 0;
            serializedYSort.FindProperty("unitsToOrder").floatValue = 100f;
            serializedYSort.FindProperty("minOrder").intValue = -32768;
            serializedYSort.FindProperty("maxOrder").intValue = 32767;
            serializedYSort.FindProperty("preferSortingGroup").boolValue = false;
            serializedYSort.FindProperty("sortingGroup").objectReferenceValue = null;

            SerializedProperty renderers = serializedYSort.FindProperty("spriteRenderers");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            serializedYSort.ApplyModifiedProperties();

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(ySort);
            EditorUtility.SetDirty(layer.gameObject);
        }

        [MenuItem("TinyMonsterKeeper/Automation/Setup Mobile Screen Layout")]
        public static void SetupMobileScreenLayout()
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject canvasObject = GameObject.Find("UI_Canvas");
            GameObject hudObject = GameObject.Find("HUD");
            GameObject cameraObject = GameObject.Find("Main Camera");
            GameObject grassObject = GameObject.Find("Tilemap_Grass");

            if (canvasObject == null || hudObject == null || cameraObject == null)
            {
                Debug.LogError("Setup Mobile Screen Layout failed: UI_Canvas, HUD, or Main Camera is missing.");
                EditorApplication.Exit(1);
                return;
            }

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            RectTransform hudRect = hudObject.GetComponent<RectTransform>();
            if (canvasRect == null || hudRect == null)
            {
                Debug.LogError("Setup Mobile Screen Layout failed: UI_Canvas or HUD is missing RectTransform.");
                EditorApplication.Exit(1);
                return;
            }

            SafeAreaFitter safeAreaFitter = hudObject.GetComponent<SafeAreaFitter>();
            if (safeAreaFitter == null)
                safeAreaFitter = hudObject.AddComponent<SafeAreaFitter>();

            SerializedObject serializedSafeArea = new SerializedObject(safeAreaFitter);
            serializedSafeArea.FindProperty("fitMode").enumValueIndex = 1;
            serializedSafeArea.FindProperty("fitLeft").boolValue = true;
            serializedSafeArea.FindProperty("fitRight").boolValue = true;
            serializedSafeArea.FindProperty("fitTop").boolValue = true;
            serializedSafeArea.FindProperty("fitBottom").boolValue = false;
            serializedSafeArea.FindProperty("extraPadding").vector2Value = new Vector2(8f, 8f);
            serializedSafeArea.ApplyModifiedPropertiesWithoutUndo();

            hudRect.anchorMin = new Vector2(0f, 1f);
            hudRect.anchorMax = new Vector2(1f, 1f);
            hudRect.pivot = new Vector2(0.5f, 1f);
            hudRect.anchoredPosition = new Vector2(0f, -8f);
            hudRect.sizeDelta = new Vector2(0f, 150f);
            hudRect.localScale = Vector3.one;

            CameraMapAspectFitter aspectFitter = cameraObject.GetComponent<CameraMapAspectFitter>();
            if (aspectFitter == null)
                aspectFitter = cameraObject.AddComponent<CameraMapAspectFitter>();

            SerializedObject serializedAspectFitter = new SerializedObject(aspectFitter);
            serializedAspectFitter.FindProperty("targetCamera").objectReferenceValue = cameraObject.GetComponent<Camera>();
            serializedAspectFitter.FindProperty("mapBoundsCollider").objectReferenceValue =
                grassObject != null ? grassObject.GetComponent<Collider2D>() : null;
            serializedAspectFitter.FindProperty("desiredOrthographicSize").floatValue = 5f;
            serializedAspectFitter.FindProperty("edgePadding").floatValue = 0.05f;
            serializedAspectFitter.FindProperty("fallbackBackgroundColor").colorValue = new Color(0.76f, 0.88f, 0.56f, 1f);
            serializedAspectFitter.ApplyModifiedPropertiesWithoutUndo();
            aspectFitter.ApplyFit();

            EditorUtility.SetDirty(hudObject);
            EditorUtility.SetDirty(cameraObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Mobile screen layout setup finished.");
        }


        [MenuItem("TinyMonsterKeeper/Automation/Add Save Account Reset Tool")]
        public static void AddSaveAccountResetTool()
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject saveSystem = GameObject.Find("SaveSystem");
            if (saveSystem == null)
                saveSystem = new GameObject("SaveSystem");

            if (saveSystem.GetComponent<SaveAccountResetTool>() == null)
                saveSystem.AddComponent<SaveAccountResetTool>();

            EditorUtility.SetDirty(saveSystem);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Save account reset tool setup finished.");
        }

        [MenuItem("TinyMonsterKeeper/Automation/Setup Garden Monster Save Manager")]
        public static void SetupGardenMonsterSaveManager()
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GardenMonsterSaveManager manager = Object.FindObjectOfType<GardenMonsterSaveManager>();
            if (manager == null)
            {
                GameObject managerObject = new GameObject("GardenMonsterSaveManager");
                manager = managerObject.AddComponent<GardenMonsterSaveManager>();
            }

            SerializedObject serializedManager = new SerializedObject(manager);
            AssignObjectArray<MonsterData>(serializedManager.FindProperty("monsters"), "Assets/ScriptableObjects/MonsterData");

            CookingPotController cookingPot = Object.FindObjectOfType<CookingPotController>();
            Collider2D gardenBounds = null;
            if (cookingPot != null)
            {
                SerializedObject serializedPot = new SerializedObject(cookingPot);
                gardenBounds = serializedPot.FindProperty("monsterGardenBounds").objectReferenceValue as Collider2D;
            }

            if (gardenBounds != null)
                serializedManager.FindProperty("gardenBounds").objectReferenceValue = gardenBounds;

            serializedManager.FindProperty("spawnDefaultUnlockedMonsters").boolValue = true;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Garden monster save manager setup finished.");
        }

        [MenuItem("TinyMonsterKeeper/Automation/Reorganize Scene Hierarchy")]
        public static void ReorganizeSceneHierarchy()
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Transform systems = GetOrCreateRootGroup("_Systems").transform;
            Transform world = GetOrCreateRootGroup("_World").transform;
            Transform ui = GetOrCreateRootGroup("_UI").transform;
            Transform camera = GetOrCreateRootGroup("_Camera").transform;
            Transform lighting = GetOrCreateRootGroup("_Lighting").transform;
            Transform navigation = GetOrCreateRootGroup("_Navigation").transform;

            MoveRootIfExists("SaveSystem", systems);
            MoveRootIfExists("InventoryManager", systems);
            MoveRootIfExists("CurrencyManager", systems);
            MoveRootIfExists("FogZoneManager", systems);
            MoveRootIfExists("GardenMonsterSaveManager", systems);

            MoveRootIfExists("Enviroment", world);
            MoveRootIfExists("ResourcesNode", world);
            MoveRootIfExists("CookingPot_Map", world);

            MoveRootIfExists("UI_Canvas", ui);
            MoveRootIfExists("EventSystem", ui);
            MoveRootIfExists("UIManager", ui);
            MoveRootIfExists("NoticeSystem", ui);
            MoveRootIfExists("RewardPopupManager", ui);

            MoveRootIfExists("Main Camera", camera);
            MoveRootIfExists("Global Light 2D", lighting);
            MoveRootIfExists("Navmesh", navigation);

            RenameInventoryFoodItems();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("Scene hierarchy reorganization finished.");
        }

        [MenuItem("TinyMonsterKeeper/Automation/Setup Kabuto Monster")]
        public static void SetupKabutoMonster()
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";
            const string sourceSpritePath = "Assets/Arts/Monsters/MonNo6_Kabuto/Kabuto_Idle.png";
            const string animationFolder = "Assets/Animations/MonNo6";
            const string animatorFolder = "Assets/Animators/Monsters";
            const string prefabPath = "Assets/Prefabs/Monsters/MonNo6_Kabuto.prefab";
            const string dataPath = "Assets/ScriptableObjects/MonsterData/KabutoData.asset";
            const string sourcePrefabPath = "Assets/Prefabs/Monsters/MonNo4_Cotty.prefab";
            const string sourceControllerPath = "Assets/Animators/Monsters/MonNo4_CottyController.controller";

            Sprite[] idleSprites = LoadSprites(sourceSpritePath);
            if (idleSprites.Length == 0)
            {
                Debug.LogError("Kabuto idle sprites are missing or not sliced. Path: " + sourceSpritePath);
                EditorApplication.Exit(1);
                return;
            }

            EnsureFolder("Assets/Animations", "MonNo6");

            AnimationClip idleClip = CreateOrReplaceSpriteClip(animationFolder + "/Idle.anim", idleSprites, 8f, true);
            AnimationClip happyClip = CreateOrReplaceSpriteClip(animationFolder + "/Happy.anim", new[] { idleSprites[0] }, 8f, false);
            AnimationClip sleepClip = CreateOrReplaceSpriteClip(animationFolder + "/Sleep.anim", new[] { idleSprites[0] }, 8f, true);

            string controllerPath = animatorFolder + "/MonNo6_KabutoController.controller";
            AnimatorController controller = CreateMonsterAnimatorController(sourceControllerPath, controllerPath, "MonNo6_KabutoController", idleClip, happyClip, sleepClip);

            MonsterData monsterData = CreateOrUpdateKabutoData(dataPath, idleSprites[0]);
            GameObject prefab = CreateOrUpdateKabutoPrefab(sourcePrefabPath, prefabPath, monsterData, idleSprites[0], controller);
            monsterData.prefab = prefab;
            EditorUtility.SetDirty(monsterData);

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            AppendMonsterToSceneDatabases(monsterData);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Kabuto monster setup finished.");
        }

        [MenuItem("TinyMonsterKeeper/Automation/Setup Antie Monster")]
        public static void SetupAntieMonster()
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";
            const string sourceSpritePath = "Assets/Arts/Monsters/MonNo7_Antie/Antie_Idle.png";
            const string animationFolder = "Assets/Animations/MonNo7";
            const string animatorFolder = "Assets/Animators/Monsters";
            const string prefabPath = "Assets/Prefabs/Monsters/MonNo7_Antie.prefab";
            const string dataPath = "Assets/ScriptableObjects/MonsterData/AntieData.asset";
            const string sourcePrefabPath = "Assets/Prefabs/Monsters/MonNo4_Cotty.prefab";
            const string sourceControllerPath = "Assets/Animators/Monsters/MonNo4_CottyController.controller";

            Sprite[] idleSprites = LoadSprites(sourceSpritePath);
            if (idleSprites.Length == 0)
            {
                Debug.LogError("Antie idle sprites are missing or not sliced. Path: " + sourceSpritePath);
                EditorApplication.Exit(1);
                return;
            }

            EnsureFolder("Assets/Animations", "MonNo7");

            AnimationClip idleClip = CreateOrReplaceSpriteClip(animationFolder + "/Idle.anim", idleSprites, 8f, true);
            AnimationClip happyClip = CreateOrReplaceSpriteClip(animationFolder + "/Happy.anim", new[] { idleSprites[0] }, 8f, false);
            AnimationClip sleepClip = CreateOrReplaceSpriteClip(animationFolder + "/Sleep.anim", new[] { idleSprites[0] }, 8f, true);

            string controllerPath = animatorFolder + "/MonNo7_AntieController.controller";
            AnimatorController controller = CreateMonsterAnimatorController(sourceControllerPath, controllerPath, "MonNo7_AntieController", idleClip, happyClip, sleepClip);

            MonsterData monsterData = CreateOrUpdateMonsterData(dataPath, "007", "Antie", idleSprites[0]);
            GameObject prefab = CreateOrUpdateMonsterPrefab(sourcePrefabPath, prefabPath, "MonNo7_Antie", "MonNo7_Antie_Visual", monsterData, idleSprites[0], controller);
            monsterData.prefab = prefab;
            EditorUtility.SetDirty(monsterData);

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            AppendMonsterToSceneDatabases(monsterData);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Antie monster setup finished.");
        }

        [MenuItem("TinyMonsterKeeper/Automation/Setup MushRibbit Monster")]
        public static void SetupMushRibbitMonster()
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";
            const string sourceSpritePath = "Assets/Arts/Monsters/MonNo8_MushRibbit/Mushribbit_Idle.png";
            const string animationFolder = "Assets/Animations/MonNo8";
            const string animatorFolder = "Assets/Animators/Monsters";
            const string prefabPath = "Assets/Prefabs/Monsters/MonNo8_MushRibbit.prefab";
            const string dataPath = "Assets/ScriptableObjects/MonsterData/MushRibbitData.asset";
            const string sourcePrefabPath = "Assets/Prefabs/Monsters/MonNo4_Cotty.prefab";
            const string sourceControllerPath = "Assets/Animators/Monsters/MonNo4_CottyController.controller";

            Sprite[] idleSprites = LoadSprites(sourceSpritePath);
            if (idleSprites.Length == 0)
            {
                Debug.LogError("MushRibbit idle sprites are missing or not sliced. Path: " + sourceSpritePath);
                EditorApplication.Exit(1);
                return;
            }

            EnsureFolder("Assets/Animations", "MonNo8");

            AnimationClip idleClip = CreateOrReplaceSpriteClip(animationFolder + "/Idle.anim", idleSprites, 8f, true);
            AnimationClip happyClip = CreateOrReplaceSpriteClip(animationFolder + "/Happy.anim", new[] { idleSprites[0] }, 8f, false);
            AnimationClip sleepClip = CreateOrReplaceSpriteClip(animationFolder + "/Sleep.anim", new[] { idleSprites[0] }, 8f, true);

            string controllerPath = animatorFolder + "/MonNo8_MushRibbitController.controller";
            AnimatorController controller = CreateMonsterAnimatorController(sourceControllerPath, controllerPath, "MonNo8_MushRibbitController", idleClip, happyClip, sleepClip);

            MonsterData monsterData = CreateOrUpdateMonsterData(dataPath, "008", "MushRibbit", idleSprites[0]);
            GameObject prefab = CreateOrUpdateMonsterPrefab(sourcePrefabPath, prefabPath, "MonNo8_MushRibbit", "MonNo8_MushRibbit_Visual", monsterData, idleSprites[0], controller);
            monsterData.prefab = prefab;
            EditorUtility.SetDirty(monsterData);

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            AppendMonsterToSceneDatabases(monsterData);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("MushRibbit monster setup finished.");
        }

        [MenuItem("TinyMonsterKeeper/Automation/Setup Arcant Monster")]
        public static void SetupArcantMonster()
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";
            const string sourceSpritePath = "Assets/Arts/Monsters/MonNo9_Arcant/Arcant_Idle.png";
            const string animationFolder = "Assets/Animations/MonNo9";
            const string animatorFolder = "Assets/Animators/Monsters";
            const string prefabPath = "Assets/Prefabs/Monsters/MonNo9_Arcant.prefab";
            const string dataPath = "Assets/ScriptableObjects/MonsterData/ArcantData.asset";
            const string sourcePrefabPath = "Assets/Prefabs/Monsters/MonNo4_Cotty.prefab";
            const string sourceControllerPath = "Assets/Animators/Monsters/MonNo4_CottyController.controller";

            Sprite[] idleSprites = LoadSprites(sourceSpritePath);
            if (idleSprites.Length == 0)
            {
                Debug.LogError("Arcant idle sprites are missing or not sliced. Path: " + sourceSpritePath);
                EditorApplication.Exit(1);
                return;
            }

            EnsureFolder("Assets/Animations", "MonNo9");

            AnimationClip idleClip = CreateOrReplaceSpriteClip(animationFolder + "/Idle.anim", idleSprites, 8f, true);
            AnimationClip happyClip = CreateOrReplaceSpriteClip(animationFolder + "/Happy.anim", new[] { idleSprites[0] }, 8f, false);
            AnimationClip sleepClip = CreateOrReplaceSpriteClip(animationFolder + "/Sleep.anim", new[] { idleSprites[0] }, 8f, true);

            string controllerPath = animatorFolder + "/MonNo9_ArcantController.controller";
            AnimatorController controller = CreateMonsterAnimatorController(sourceControllerPath, controllerPath, "MonNo9_ArcantController", idleClip, happyClip, sleepClip);

            MonsterData monsterData = CreateOrUpdateMonsterData(dataPath, "009", "Arcant", idleSprites[0]);
            GameObject prefab = CreateOrUpdateMonsterPrefab(sourcePrefabPath, prefabPath, "MonNo9_Arcant", "MonNo9_Arcant_Visual", monsterData, idleSprites[0], controller);
            monsterData.prefab = prefab;
            EditorUtility.SetDirty(monsterData);

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            AppendMonsterToSceneDatabases(monsterData);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Arcant monster setup finished.");
        }

        [MenuItem("TinyMonsterKeeper/Automation/Setup Monsters 10-16")]
        public static void SetupMonsters10To16()
        {
            MonsterSetupDefinition[] monstersToSetup =
            {
                new MonsterSetupDefinition(10, "Moolo", "MonNo10_Moolo", "Moolo_Idle.png"),
                new MonsterSetupDefinition(11, "Lotus", "MonNo11_Lotus", "lotus_idle.png"),
                new MonsterSetupDefinition(12, "Pipcher", "MonNo12_Pipcher", "Pipcher_Idle.png"),
                new MonsterSetupDefinition(13, "Woody", "MonNo13_Woody", "Woody_Idle.png"),
                new MonsterSetupDefinition(14, "Cooconi", "MonNo14_Cooconi", "Cooconi_Idle.png"),
                new MonsterSetupDefinition(15, "LilyPadle", "MonNo15_LilyPadle", "LilyPadle_Idle.png"),
                new MonsterSetupDefinition(16, "Strawli", "MonNo16_Strawli", "Strawli_Idle.png")
            };

            const string scenePath = "Assets/Scenes/SampleScene.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            for (int i = 0; i < monstersToSetup.Length; i++)
            {
                MonsterData monsterData = SetupMonsterAssetBundle(monstersToSetup[i]);
                AppendMonsterToSceneDatabases(monsterData);
            }

            SetupBookCardCapacity(16, 4);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Monsters 10-16 setup finished.");
        }

        [MenuItem("TinyMonsterKeeper/Automation/Setup Cacu Monster")]
        public static void SetupCacuMonster()
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            MonsterData monsterData = SetupMonsterAssetBundle(new MonsterSetupDefinition(17, "Cacu", "MonNo17_Cacu", "Cacu_Idle.png"));
            AppendMonsterToSceneDatabases(monsterData);
            SetupBookCardCapacity(17, 4);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Cacu monster setup finished.");
        }

        [MenuItem("TinyMonsterKeeper/Automation/Setup Leafbag Monster")]
        public static void SetupLeafbagMonster()
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            MonsterData monsterData = SetupMonsterAssetBundle(new MonsterSetupDefinition(18, "Leafbag", "MonNo18_Leafbag", "Leafbag_Idle.png"));
            AppendMonsterToSceneDatabases(monsterData);
            SetupBookCardCapacity(18, 4);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Leafbag monster setup finished.");
        }

        [MenuItem("TinyMonsterKeeper/Automation/Setup Monsters 19-27")]
        public static void SetupMonsters19To27()
        {
            MonsterSetupDefinition[] monstersToSetup =
            {
                new MonsterSetupDefinition(19, "Molli", "MonNo19_Molli", "Molli_Idle.png"),
                new MonsterSetupDefinition(20, "Rooty", "MonNo20_Rooty", "Rooty_Idle.png"),
                new MonsterSetupDefinition(21, "Wispbo", "MonNo21_Wispbo", "Wispbo_Idle.png"),
                new MonsterSetupDefinition(22, "Bambat", "MonNo22_Bambat", "Bambat_Idle.png"),
                new MonsterSetupDefinition(23, "Bambam", "MonNo23_Bambam", "Bambam_Idle.png"),
                new MonsterSetupDefinition(24, "Bamurtle", "MonNo24_Bamurtle", "Bamurtle_Idle.png"),
                new MonsterSetupDefinition(25, "Beo", "MonNo25_Beo", "Beo_Idle.png"),
                new MonsterSetupDefinition(26, "Moss", "MonNo26_Moss", "Moss_Idle.png"),
                new MonsterSetupDefinition(27, "Bolla", "MonNo27_Bolla", "Bolla_Idle.png")
            };

            const string scenePath = "Assets/Scenes/SampleScene.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            for (int i = 0; i < monstersToSetup.Length; i++)
            {
                MonsterData monsterData = SetupMonsterAssetBundle(monstersToSetup[i]);
                AppendMonsterToSceneDatabases(monsterData);
            }

            SetupBookCardCapacity(27, 4);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Monsters 19-27 setup finished.");
        }

        private static void AssignObjectArray<T>(SerializedProperty arrayProperty, string searchFolder) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { searchFolder });
            arrayProperty.arraySize = guids.Length;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                arrayProperty.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<T>(path);
            }
        }

        private static Sprite[] LoadSprites(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            int count = 0;
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite)
                    count++;
            }

            Sprite[] sprites = new Sprite[count];
            int index = 0;
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    sprites[index] = sprite;
                    index++;
                }
            }

            return sprites;
        }

        private static MonsterData SetupMonsterAssetBundle(MonsterSetupDefinition definition)
        {
            const string sourcePrefabPath = "Assets/Prefabs/Monsters/MonNo4_Cotty.prefab";
            const string sourceControllerPath = "Assets/Animators/Monsters/MonNo4_CottyController.controller";

            string sourceSpritePath = $"Assets/Arts/Monsters/{definition.folderName}/{definition.idleSpriteFileName}";
            Sprite[] idleSprites = LoadSprites(sourceSpritePath);
            if (idleSprites.Length == 0)
            {
                Debug.LogError($"{definition.monsterName} idle sprites are missing or not sliced. Path: {sourceSpritePath}");
                EditorApplication.Exit(1);
                return null;
            }

            string animationFolder = $"Assets/Animations/MonNo{definition.monsterNumber}";
            EnsureFolder("Assets/Animations", $"MonNo{definition.monsterNumber}");

            AnimationClip idleClip = CreateOrReplaceSpriteClip(animationFolder + "/Idle.anim", idleSprites, 8f, true);
            AnimationClip happyClip = CreateOrReplaceSpriteClip(animationFolder + "/Happy.anim", new[] { idleSprites[0] }, 8f, false);
            AnimationClip sleepClip = CreateOrReplaceSpriteClip(animationFolder + "/Sleep.anim", new[] { idleSprites[0] }, 8f, true);

            string controllerPath = $"Assets/Animators/Monsters/{definition.folderName}Controller.controller";
            AnimatorController controller = CreateMonsterAnimatorController(sourceControllerPath, controllerPath, $"{definition.folderName}Controller", idleClip, happyClip, sleepClip);

            string dataPath = $"Assets/ScriptableObjects/MonsterData/{definition.monsterName}Data.asset";
            MonsterData monsterData = CreateOrUpdateMonsterData(dataPath, definition.monsterNumber.ToString("000"), definition.monsterName, idleSprites[0]);

            string prefabPath = $"Assets/Prefabs/Monsters/{definition.folderName}.prefab";
            GameObject prefab = CreateOrUpdateMonsterPrefab(sourcePrefabPath, prefabPath, definition.folderName, definition.folderName + "_Visual", monsterData, idleSprites[0], controller);
            monsterData.prefab = prefab;
            EditorUtility.SetDirty(monsterData);

            return monsterData;
        }

        private static AnimationClip CreateOrReplaceSpriteClip(string path, Sprite[] sprites, float frameRate, bool loop)
        {
            AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existingClip != null)
                AssetDatabase.DeleteAsset(path);

            AnimationClip clip = new AnimationClip
            {
                frameRate = frameRate
            };

            EditorCurveBinding binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / frameRate,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static AnimatorController CreateMonsterAnimatorController(string sourcePath, string targetPath, string controllerName, AnimationClip idleClip, AnimationClip happyClip, AnimationClip sleepClip)
        {
            if (!File.Exists(targetPath))
                AssetDatabase.CopyAsset(sourcePath, targetPath);

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(targetPath);
            if (controller == null)
            {
                Debug.LogError("Could not create Kabuto animator controller.");
                EditorApplication.Exit(1);
                return null;
            }

            controller.name = controllerName;
            EnsureAnimatorTrigger(controller, "OnIdle");
            EnsureAnimatorTrigger(controller, "IsHappy");
            EnsureAnimatorTrigger(controller, "OnSleep");

            for (int i = 0; i < controller.layers.Length; i++)
            {
                AnimatorState idleState = null;
                AnimatorState happyState = null;
                AnimatorState sleepState = null;
                ChildAnimatorState[] states = controller.layers[i].stateMachine.states;
                for (int j = 0; j < states.Length; j++)
                {
                    AnimatorState state = states[j].state;
                    if (state.name == "Idle")
                    {
                        state.motion = idleClip;
                        idleState = state;
                    }
                    else if (state.name == "Happy")
                    {
                        state.motion = happyClip;
                        happyState = state;
                    }
                    else if (state.name == "Sleep")
                    {
                        state.motion = sleepClip;
                        sleepState = state;
                    }
                }

                if (idleState != null)
                    controller.layers[i].stateMachine.defaultState = idleState;

                if (happyState != null)
                    EnsureAnyStateTriggerTransition(controller.layers[i].stateMachine, happyState, "IsHappy");

                if (sleepState != null)
                    EnsureAnyStateTriggerTransition(controller.layers[i].stateMachine, sleepState, "OnSleep");
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void EnsureAnimatorTrigger(AnimatorController controller, string parameterName)
        {
            for (int i = 0; i < controller.parameters.Length; i++)
            {
                if (controller.parameters[i].name == parameterName)
                    return;
            }

            controller.AddParameter(parameterName, AnimatorControllerParameterType.Trigger);
        }

        private static void EnsureAnyStateTriggerTransition(AnimatorStateMachine stateMachine, AnimatorState destinationState, string triggerName)
        {
            AnimatorStateTransition[] transitions = stateMachine.anyStateTransitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorStateTransition transition = transitions[i];
                if (transition.destinationState != destinationState)
                    continue;

                AnimatorCondition[] conditions = transition.conditions;
                for (int j = 0; j < conditions.Length; j++)
                {
                    if (conditions[j].parameter == triggerName)
                        return;
                }
            }

            AnimatorStateTransition newTransition = stateMachine.AddAnyStateTransition(destinationState);
            newTransition.hasExitTime = false;
            newTransition.duration = 0f;
            newTransition.canTransitionToSelf = true;
            newTransition.AddCondition(AnimatorConditionMode.If, 0f, triggerName);
        }

        private static MonsterData CreateOrUpdateKabutoData(string dataPath, Sprite icon)
        {
            MonsterData data = AssetDatabase.LoadAssetAtPath<MonsterData>(dataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<MonsterData>();
                AssetDatabase.CreateAsset(data, dataPath);
            }

            data.id = "006";
            data.monsterName = "Kabuto";
            data.icon = icon;
            data.favoriteFoodId = string.Empty;
            data.favoriteToyId = string.Empty;
            data.berryCostPerFeed = 1;
            data.feedFriendshipGain = 10;
            data.coinPerTick = 1;
            data.coinTickInterval = 8f;
            data.maxStoredCoin = 5;
            data.unlockAppealCost = 0;
            data.unlockFriendshipCost = 0;
            data.unlockRequiredItemId = string.Empty;

            EditorUtility.SetDirty(data);
            return data;
        }

        private static MonsterData CreateOrUpdateMonsterData(string dataPath, string monsterId, string monsterName, Sprite icon)
        {
            MonsterData data = AssetDatabase.LoadAssetAtPath<MonsterData>(dataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<MonsterData>();
                AssetDatabase.CreateAsset(data, dataPath);
            }

            data.id = monsterId;
            data.monsterName = monsterName;
            data.icon = icon;
            data.favoriteFoodId = string.Empty;
            data.favoriteToyId = string.Empty;
            data.berryCostPerFeed = 1;
            data.feedFriendshipGain = 10;
            data.coinPerTick = 1;
            data.coinTickInterval = 8f;
            data.maxStoredCoin = 5;
            data.unlockAppealCost = 0;
            data.unlockFriendshipCost = 0;
            data.unlockRequiredItemId = string.Empty;

            EditorUtility.SetDirty(data);
            return data;
        }

        private static GameObject CreateOrUpdateKabutoPrefab(string sourcePath, string targetPath, MonsterData data, Sprite idleSprite, RuntimeAnimatorController controller)
        {
            if (!File.Exists(targetPath))
                AssetDatabase.CopyAsset(sourcePath, targetPath);

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(targetPath);
            prefabRoot.name = "MonNo6_Kabuto";

            TinyMonsterController monsterController = prefabRoot.GetComponent<TinyMonsterController>();
            if (monsterController != null)
            {
                SerializedObject serializedMonster = new SerializedObject(monsterController);
                serializedMonster.FindProperty("monsterData").objectReferenceValue = data;
                serializedMonster.ApplyModifiedPropertiesWithoutUndo();
            }

            TinyMonsterNavRoam navRoam = prefabRoot.GetComponent<TinyMonsterNavRoam>();
            if (navRoam != null)
            {
                SerializedObject serializedNav = new SerializedObject(navRoam);
                SerializedProperty spriteRendererProperty = serializedNav.FindProperty("spriteRenderer");
                SpriteRenderer visualRenderer = FindMainVisualRenderer(prefabRoot);
                if (visualRenderer != null)
                    spriteRendererProperty.objectReferenceValue = visualRenderer;
                serializedNav.ApplyModifiedPropertiesWithoutUndo();
            }

            TinyMonsterAnimationController animationController = prefabRoot.GetComponent<TinyMonsterAnimationController>();
            Animator animator = prefabRoot.GetComponentInChildren<Animator>(true);
            if (animationController != null && animator != null)
            {
                SerializedObject serializedAnimation = new SerializedObject(animationController);
                serializedAnimation.FindProperty("animator").objectReferenceValue = animator;
                serializedAnimation.ApplyModifiedPropertiesWithoutUndo();
            }

            SpriteRenderer mainRenderer = FindMainVisualRenderer(prefabRoot);
            if (mainRenderer != null)
            {
                mainRenderer.gameObject.name = "MonNo6_Kabuto_Visual";
                mainRenderer.sprite = idleSprite;
                mainRenderer.enabled = true;
                EditorUtility.SetDirty(mainRenderer);
            }

            if (animator != null)
                animator.runtimeAnimatorController = controller;

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, targetPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
        }

        private static GameObject CreateOrUpdateMonsterPrefab(string sourcePath, string targetPath, string rootName, string visualName, MonsterData data, Sprite idleSprite, RuntimeAnimatorController controller)
        {
            if (!File.Exists(targetPath))
                AssetDatabase.CopyAsset(sourcePath, targetPath);

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(targetPath);
            prefabRoot.name = rootName;

            TinyMonsterController monsterController = prefabRoot.GetComponent<TinyMonsterController>();
            if (monsterController != null)
            {
                SerializedObject serializedMonster = new SerializedObject(monsterController);
                serializedMonster.FindProperty("monsterData").objectReferenceValue = data;
                serializedMonster.ApplyModifiedPropertiesWithoutUndo();
            }

            TinyMonsterNavRoam navRoam = prefabRoot.GetComponent<TinyMonsterNavRoam>();
            if (navRoam != null)
            {
                SerializedObject serializedNav = new SerializedObject(navRoam);
                SerializedProperty spriteRendererProperty = serializedNav.FindProperty("spriteRenderer");
                SpriteRenderer visualRenderer = FindMainVisualRenderer(prefabRoot);
                if (visualRenderer != null)
                    spriteRendererProperty.objectReferenceValue = visualRenderer;
                serializedNav.ApplyModifiedPropertiesWithoutUndo();
            }

            TinyMonsterAnimationController animationController = prefabRoot.GetComponent<TinyMonsterAnimationController>();
            Animator animator = prefabRoot.GetComponentInChildren<Animator>(true);
            if (animationController != null && animator != null)
            {
                SerializedObject serializedAnimation = new SerializedObject(animationController);
                serializedAnimation.FindProperty("animator").objectReferenceValue = animator;
                serializedAnimation.ApplyModifiedPropertiesWithoutUndo();
            }

            SpriteRenderer mainRenderer = FindMainVisualRenderer(prefabRoot);
            if (mainRenderer != null)
            {
                mainRenderer.gameObject.name = visualName;
                mainRenderer.sprite = idleSprite;
                mainRenderer.enabled = true;
                EditorUtility.SetDirty(mainRenderer);
            }

            if (animator != null)
                animator.runtimeAnimatorController = controller;

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, targetPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            GameObject savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
            EnforceSavedPrefabVisualSprite(savedPrefab, idleSprite, rootName);
            return savedPrefab;
        }

        private static void EnforceSavedPrefabVisualSprite(GameObject prefab, Sprite expectedSprite, string rootName)
        {
            if (prefab == null || expectedSprite == null)
                return;

            SpriteRenderer visualRenderer = FindMainVisualRenderer(prefab);
            if (visualRenderer == null || visualRenderer.sprite == expectedSprite)
                return;

            visualRenderer.sprite = expectedSprite;
            EditorUtility.SetDirty(visualRenderer);
            PrefabUtility.SavePrefabAsset(prefab);

            if (visualRenderer.sprite != expectedSprite)
                Debug.LogError($"{rootName} visual sprite is still {visualRenderer.sprite?.name}; expected {expectedSprite.name}.");
        }

        private static SpriteRenderer FindMainVisualRenderer(GameObject prefabRoot)
        {
            SpriteRenderer[] renderers = prefabRoot.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].gameObject.name.Contains("_Visual"))
                    return renderers[i];
            }

            return renderers.Length > 0 ? renderers[0] : null;
        }

        private static void AppendMonsterToSceneDatabases(MonsterData monsterData)
        {
            if (monsterData == null)
                return;

            BookOpenUI book = Object.FindObjectOfType<BookOpenUI>(true);
            if (book != null)
                AppendObjectToSerializedArray(book, "monsters", monsterData);

            SaveGameRuntimeBinder binder = Object.FindObjectOfType<SaveGameRuntimeBinder>(true);
            if (binder != null)
                AppendObjectToSerializedArray(binder, "monsterDatabase", monsterData);

            GardenMonsterSaveManager gardenManager = Object.FindObjectOfType<GardenMonsterSaveManager>(true);
            if (gardenManager != null)
                AppendObjectToSerializedArray(gardenManager, "monsters", monsterData);
        }

        private static void AppendObjectToSerializedArray(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty array = serializedObject.FindProperty(propertyName);
            if (array == null || !array.isArray)
                return;

            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == value)
                {
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }
            }

            array.arraySize++;
            array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetupBookCardCapacity(int totalMonsterCount, int cardsPerPage)
        {
            BookOpenUI book = Object.FindObjectOfType<BookOpenUI>(true);
            if (book != null)
            {
                SerializedObject serializedBook = new SerializedObject(book);
                SerializedProperty totalPages = serializedBook.FindProperty("totalPages");
                if (totalPages != null)
                    totalPages.intValue = Mathf.Max(1, Mathf.CeilToInt(totalMonsterCount / (float)cardsPerPage));

                SerializedProperty serializedCardsPerPage = serializedBook.FindProperty("cardsPerPage");
                if (serializedCardsPerPage != null)
                    serializedCardsPerPage.intValue = cardsPerPage;

                serializedBook.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(book);
            }

            GameObject contentRoot = FindSceneObject("BookContentRootLeft");
            if (contentRoot == null)
            {
                Debug.LogWarning("BookContentRootLeft is missing. Book card count was not updated.");
                return;
            }

            EnsureChildCardCount(contentRoot.transform, cardsPerPage);
        }

        private static void EnsureChildCardCount(Transform root, int targetCount)
        {
            GameObject template = null;
            int cardCount = 0;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (!child.name.StartsWith("BookCard_Lock"))
                    continue;

                cardCount++;
                if (template == null)
                    template = child.gameObject;
            }

            if (template == null)
            {
                Debug.LogWarning("No BookCard_Lock template found under BookContentRootLeft.");
                return;
            }

            for (int i = cardCount + 1; i <= targetCount; i++)
            {
                GameObject clone = Object.Instantiate(template, root);
                clone.name = $"BookCard_Lock_{i:00}";
                clone.SetActive(true);
                EditorUtility.SetDirty(clone);
            }

            EditorUtility.SetDirty(root.gameObject);
        }

        private static void EnsureFolder(string parentPath, string folderName)
        {
            string fullPath = parentPath + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(fullPath))
                AssetDatabase.CreateFolder(parentPath, folderName);
        }

        private readonly struct MonsterSetupDefinition
        {
            public readonly int monsterNumber;
            public readonly string monsterName;
            public readonly string folderName;
            public readonly string idleSpriteFileName;

            public MonsterSetupDefinition(int monsterNumber, string monsterName, string folderName, string idleSpriteFileName)
            {
                this.monsterNumber = monsterNumber;
                this.monsterName = monsterName;
                this.folderName = folderName;
                this.idleSpriteFileName = idleSpriteFileName;
            }
        }

        private static GameObject GetOrCreateRootGroup(string groupName)
        {
            GameObject group = FindRootObject(groupName);
            if (group != null)
                return group;

            group = new GameObject(groupName);
            Undo.RegisterCreatedObjectUndo(group, "Create " + groupName);
            group.transform.SetParent(null);
            group.transform.position = Vector3.zero;
            group.transform.rotation = Quaternion.identity;
            group.transform.localScale = Vector3.one;
            return group;
        }

        private static void MoveRootIfExists(string objectName, Transform parent)
        {
            GameObject target = FindRootObject(objectName);
            if (target == null || target.transform == parent)
                return;

            target.transform.SetParent(parent, true);
            EditorUtility.SetDirty(target);
        }

        private static GameObject FindRootObject(string objectName)
        {
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                    return roots[i];
            }

            return null;
        }

        private static void RenameInventoryFoodItems()
        {
            GameObject foodItemsRoot = FindSceneObject("FoodGrid");
            if (foodItemsRoot == null)
                foodItemsRoot = FindSceneObject("YourFoodPanel");

            if (foodItemsRoot == null)
                return;

            int slotIndex = 1;
            for (int i = 0; i < foodItemsRoot.transform.childCount; i++)
            {
                Transform child = foodItemsRoot.transform.GetChild(i);
                if (!child.name.StartsWith("FoodItem"))
                    continue;

                child.name = $"InventorySlot_{slotIndex:00}";
                EditorUtility.SetDirty(child.gameObject);
                slotIndex++;
            }
        }

        private static GameObject FindSceneObject(string objectName)
        {
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject found = FindInChildrenIncludingInactive(roots[i].transform, objectName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static GameObject FindInChildrenIncludingInactive(Transform root, string objectName)
        {
            if (root.name == objectName)
                return root.gameObject;

            for (int i = 0; i < root.childCount; i++)
            {
                GameObject found = FindInChildrenIncludingInactive(root.GetChild(i), objectName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static int RequireFile(string path, string message)
        {
            if (File.Exists(path))
                return 0;

            Debug.LogError(message + " Path: " + path);
            return 1;
        }

        private static int RequireAsset(string path, string message)
        {
            if (AssetDatabase.IsValidFolder(path))
                return 0;

            Debug.LogError(message + " Path: " + path);
            return 1;
        }
    }
}
