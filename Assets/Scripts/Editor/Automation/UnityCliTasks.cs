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
