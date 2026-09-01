#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace TinyMonsterKeeper.EditorAutomation
{
    public static class SortingOrderNormalizationTool
    {
        private const string GameplayScenePath = "Assets/Scenes/GameplayScene.unity";
        private const int OldWorldBase = 10000;
        private const float Scale = 0.32f;
        private const int NewWorldBase = 500;
        private const float NewUnitsToOrder = 32f;
        private const int FogOrder = 1000;
        private const int UnlockOrder = 1010;
        private const int DropOrder = 1100;

        [MenuItem("TinyMonsterKeeper/Automation/Normalize Sorting Orders")]
        public static void Normalize()
        {
            int prefabCount = NormalizePrefabs();
            int sceneCount = NormalizeGameplayScene();
            AssetDatabase.SaveAssets();
            Debug.Log($"Sorting orders normalized. Prefabs: {prefabCount}, scene objects: {sceneCount}.");
        }

        private static int NormalizePrefabs()
        {
            int changedCount = 0;
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;

                foreach (YSortByPosition ySort in root.GetComponentsInChildren<YSortByPosition>(true))
                    changed |= NormalizeYSort(ySort);

                foreach (BerryDropController drop in root.GetComponentsInChildren<BerryDropController>(true))
                    changed |= SetInt(drop, "dropSortingOrder", DropOrder);

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changedCount++;
                }
                PrefabUtility.UnloadPrefabContents(root);
            }
            return changedCount;
        }

        private static int NormalizeGameplayScene()
        {
            Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            int changedCount = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (YSortByPosition ySort in root.GetComponentsInChildren<YSortByPosition>(true))
                {
                    if (NormalizeYSort(ySort))
                        changedCount++;
                }

                foreach (BerryDropController drop in root.GetComponentsInChildren<BerryDropController>(true))
                {
                    if (SetInt(drop, "dropSortingOrder", DropOrder))
                        changedCount++;
                }

                foreach (TilemapRenderer renderer in root.GetComponentsInChildren<TilemapRenderer>(true))
                {
                    if (!renderer.name.Contains("_Fog"))
                        continue;
                    renderer.sortingOrder = FogOrder;
                    EditorUtility.SetDirty(renderer);
                    changedCount++;
                }

                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!transform.name.Contains("Button_Unlock"))
                        continue;
                    foreach (SpriteRenderer renderer in transform.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        renderer.sortingOrder = UnlockOrder;
                        EditorUtility.SetDirty(renderer);
                    }
                    SortingGroup group = transform.GetComponent<SortingGroup>();
                    if (group != null)
                    {
                        group.sortingOrder = UnlockOrder;
                        EditorUtility.SetDirty(group);
                    }
                    changedCount++;
                }
            }

            foreach (SpriteRenderer renderer in Object.FindObjectsOfType<SpriteRenderer>(true))
            {
                if (renderer.gameObject.scene != scene || renderer.sortingOrder < 20000)
                    continue;
                renderer.sortingOrder = DropOrder;
                EditorUtility.SetDirty(renderer);
                changedCount++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return changedCount;
        }

        private static bool NormalizeYSort(YSortByPosition ySort)
        {
            SerializedObject serialized = new SerializedObject(ySort);
            SerializedProperty worldBase = serialized.FindProperty("worldBaseOrder");
            SerializedProperty baseOrder = serialized.FindProperty("baseOrder");
            SerializedProperty units = serialized.FindProperty("unitsToOrder");
            SerializedProperty min = serialized.FindProperty("minOrder");
            SerializedProperty max = serialized.FindProperty("maxOrder");

            int oldBase = worldBase != null ? worldBase.intValue : NewWorldBase;
            int convertedBase = oldBase;
            if (oldBase > 2000)
                convertedBase = NewWorldBase + Mathf.RoundToInt((oldBase - OldWorldBase) * Scale);
            else if (oldBase < -900)
                convertedBase = NewWorldBase;
            if (worldBase != null)
                worldBase.intValue = convertedBase;
            if (baseOrder != null && Mathf.Abs(baseOrder.intValue) > 100)
                baseOrder.intValue = Mathf.RoundToInt(baseOrder.intValue * Scale);
            if (units != null)
                units.floatValue = NewUnitsToOrder;
            if (min != null)
                min.intValue = -900;
            if (max != null)
                max.intValue = 900;

            bool changed = serialized.ApplyModifiedPropertiesWithoutUndo();
            if (changed)
                EditorUtility.SetDirty(ySort);
            return changed;
        }

        private static bool SetInt(Object target, string propertyName, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.intValue == value)
                return false;
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            return true;
        }
    }
}
#endif
