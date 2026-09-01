using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class YSortSetupTool
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    private static readonly string[] ResourceMapSkipNames =
    {
        "grass_tilemap",
        "tilemap",
        "fog"
    };

    [MenuItem("Tools/Tiny Monster/Setup Y Sorting")]
    public static void SetupYSorting()
    {
        int changedPrefabs = 0;
        changedPrefabs += SetupMonsterPrefabs();
        changedPrefabs += SetupResourceNodeMapPrefabs();
        bool sceneChanged = SetupSceneObjects();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"YSort setup complete. Changed prefabs: {changedPrefabs}, scene changed: {sceneChanged}");
    }

    private static int SetupMonsterPrefabs()
    {
        string[] prefabPaths = Directory.GetFiles("Assets/Prefabs/Monsters", "*.prefab", SearchOption.TopDirectoryOnly)
            .ReplaceBackslashes();

        int changedCount = 0;
        foreach (string path in prefabPaths)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool changed = EnsureSortingGroup(root, out SortingGroup sortingGroup);
            changed |= EnsureYSort(root, root.transform, sortingGroup, 0f, preferSortingGroup: true);

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                changedCount++;
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        return changedCount;
    }

    private static int SetupResourceNodeMapPrefabs()
    {
        string[] prefabPaths = Directory.GetFiles("Assets/Prefabs/ResourcesNode/ResourcesNode_Map", "*.prefab", SearchOption.TopDirectoryOnly)
            .ReplaceBackslashes();

        int changedCount = 0;
        foreach (string path in prefabPaths)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (ShouldSkipResourceMapPrefab(fileName))
            {
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root.GetComponentsInChildren<SpriteRenderer>(true).Length == 0)
            {
                PrefabUtility.UnloadPrefabContents(root);
                continue;
            }

            bool changed = EnsureSortingGroup(root, out SortingGroup sortingGroup);
            changed |= EnsureYSort(root, root.transform, sortingGroup, GuessSortYOffset(fileName), preferSortingGroup: true);

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                changedCount++;
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        return changedCount;
    }

    private static bool SetupSceneObjects()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        bool changed = false;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform target in transforms)
            {
                GameObject go = target.gameObject;
                if (!ShouldSetupSceneObject(go))
                {
                    continue;
                }

                changed |= EnsureSortingGroup(go, out SortingGroup sortingGroup);
                changed |= EnsureYSort(go, go.transform, sortingGroup, GuessSortYOffset(go.name), preferSortingGroup: true);
            }
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        return changed;
    }

    private static bool ShouldSetupSceneObject(GameObject go)
    {
        if (go.GetComponentsInChildren<SpriteRenderer>(true).Length == 0)
        {
            return false;
        }

        string name = go.name.ToLowerInvariant();
        return go.GetComponent<CookingPotController>() != null
            || go.GetComponent<BushController>() != null
            || go.GetComponent<HarvestNodeController>() != null
            || name.Contains("tree")
            || name.Contains("bush")
            || name.Contains("mushroom")
            || name.Contains("bamboo")
            || name.Contains("stone")
            || name.Contains("log")
            || name.Contains("flower")
            || name.Contains("cave")
            || name.Contains("crystal")
            || name.Contains("stalagmite");
    }

    private static bool ShouldSkipResourceMapPrefab(string fileName)
    {
        string lower = fileName.ToLowerInvariant();
        foreach (string skipName in ResourceMapSkipNames)
        {
            if (lower.Contains(skipName))
            {
                return true;
            }
        }

        return false;
    }

    private static float GuessSortYOffset(string objectName)
    {
        string lower = objectName.ToLowerInvariant();
        if (lower.Contains("appletree") || lower == "trees" || lower.Contains("tree"))
        {
            return -1.2f;
        }

        if (lower.Contains("bamboo"))
        {
            return -0.45f;
        }

        return 0f;
    }

    private static bool EnsureSortingGroup(GameObject target, out SortingGroup sortingGroup)
    {
        sortingGroup = target.GetComponent<SortingGroup>();
        if (sortingGroup != null)
        {
            return false;
        }

        sortingGroup = target.AddComponent<SortingGroup>();
        EditorUtility.SetDirty(target);
        return true;
    }

    private static bool EnsureYSort(GameObject target, Transform sortPoint, SortingGroup sortingGroup, float sortYOffset, bool preferSortingGroup)
    {
        YSortByPosition ySort = target.GetComponent<YSortByPosition>();
        bool changed = false;
        if (ySort == null)
        {
            ySort = target.AddComponent<YSortByPosition>();
            changed = true;
        }

        SerializedObject serialized = new SerializedObject(ySort);
        changed |= SetObject(serialized, "sortPoint", sortPoint);
        changed |= SetFloat(serialized, "sortYOffset", sortYOffset);
        changed |= SetInt(serialized, "worldBaseOrder", 500);
        changed |= SetInt(serialized, "baseOrder", 0);
        changed |= SetFloat(serialized, "unitsToOrder", 32f);
        changed |= SetInt(serialized, "minOrder", -900);
        changed |= SetInt(serialized, "maxOrder", 900);
        changed |= SetBool(serialized, "preferSortingGroup", preferSortingGroup);
        changed |= SetObject(serialized, "sortingGroup", sortingGroup);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        if (changed)
        {
            EditorUtility.SetDirty(target);
        }

        return changed;
    }

    private static bool SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue == value)
        {
            return false;
        }

        property.objectReferenceValue = value;
        return true;
    }

    private static bool SetFloat(SerializedObject serialized, string propertyName, float value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || Mathf.Approximately(property.floatValue, value))
        {
            return false;
        }

        property.floatValue = value;
        return true;
    }

    private static bool SetInt(SerializedObject serialized, string propertyName, int value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || property.intValue == value)
        {
            return false;
        }

        property.intValue = value;
        return true;
    }

    private static bool SetBool(SerializedObject serialized, string propertyName, bool value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || property.boolValue == value)
        {
            return false;
        }

        property.boolValue = value;
        return true;
    }

    private static string[] ReplaceBackslashes(this string[] paths)
    {
        List<string> normalized = new List<string>(paths.Length);
        foreach (string path in paths)
        {
            normalized.Add(path.Replace('\\', '/'));
        }

        return normalized.ToArray();
    }
}
