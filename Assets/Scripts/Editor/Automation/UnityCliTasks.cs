using System.IO;
using System.Linq;
using UnityEditor.Animations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinyMonsterKeeper.EditorAutomation
{
    public static class UnityCliTasks
    {
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
