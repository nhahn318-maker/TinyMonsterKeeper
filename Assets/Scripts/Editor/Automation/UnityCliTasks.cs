using System.IO;
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
