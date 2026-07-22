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
