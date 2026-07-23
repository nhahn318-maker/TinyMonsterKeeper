using System.IO;
using UnityEditor.Animations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinyMonsterKeeper.EditorAutomation
{
    public static class UnityCliTasks
    {
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
            return AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
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

        private static void EnsureFolder(string parentPath, string folderName)
        {
            string fullPath = parentPath + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(fullPath))
                AssetDatabase.CreateFolder(parentPath, folderName);
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
