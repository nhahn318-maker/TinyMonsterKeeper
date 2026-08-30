using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinyMonsterKeeper.EditorAutomation
{
    public static class ReleaseBalanceSetupTool
    {
        private const string GameplayScenePath = "Assets/Scenes/GameplayScene.unity";

        private static readonly Dictionary<string, float> HarvestSecondsByItemId = new Dictionary<string, float>
        {
            { "berry", 45f },
            { "apple", 90f },
            { "red_mushroom_harvest", 120f },
            { "purple_berry", 120f },
            { "green_mushroom_harvest", 180f },
            { "pumpkin_harvest", 240f },
            { "eggplant_harvest", 240f },
            { "tomato_harvest", 240f },
            { "mushroom_harvest", 300f },
            { "bamboo_shoot_harvest", 360f },
            { "honey_butter", 480f },
            { "glowing_mushroom_harvest", 720f },
            { "crystal_harvest", 900f }
        };

        private static readonly int[] FogCosts =
        {
            15, 35, 60, 90, 130, 180, 240, 310, 390, 480, 580, 700, 850
        };

        [MenuItem("TinyMonsterKeeper/Automation/Setup Release Balance V1")]
        public static void SetupReleaseBalance()
        {
            SetupResourcePrefabs();
            SetupMonsterEconomy();
            SetupGameplayScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Release balance V1 setup finished: resources, monster coins, fog costs, and Zone02 tomato.");
        }

        private static void SetupResourcePrefabs()
        {
            SetBushTimer("Assets/Prefabs/ResourcesNode/ResourcesNode_Map/RedBush.prefab", 45f);
            SetBushTimer("Assets/Prefabs/ResourcesNode/ResourcesNode_Map/PurpleBush.prefab", 120f);
            SetBushTimer("Assets/Prefabs/ResourcesNode/ResourcesNode_Map/AppleTree.prefab", 90f);

            SetHarvestTimer("Assets/Prefabs/ResourcesNode/ResourcesNode_Map/Bamboo_Shoot.prefab", 360f);
            SetHarvestTimer("Assets/Prefabs/ResourcesNode/ResourcesNode_Map/eggplant_map.prefab", 240f);
            SetHarvestTimer("Assets/Prefabs/ResourcesNode/ResourcesNode_Map/pumpkin_map.prefab", 240f);
            SetHarvestTimer("Assets/Prefabs/ResourcesNode/ResourcesNode_Map/Tomato_Map.prefab", 240f);
            SetHarvestTimer("Assets/Prefabs/ResourcesNode/ResourcesNode_Map/glowing_mushroom_map.prefab", 720f);
            SetStaticHarvestTimer("Assets/Prefabs/ResourcesNode/ResourcesNode_Map/BeeHome_Map.prefab", 480f);
            SetStaticHarvestTimer("Assets/Prefabs/ResourcesNode/ResourcesNode_Map/Crystal_Cluster_Map.prefab", 900f);
        }

        private static void SetBushTimer(string path, float seconds)
        {
            EditPrefabComponent<BushController>(path, component => SetFloat(component, "timeToFruit", seconds));
        }

        private static void SetHarvestTimer(string path, float seconds)
        {
            EditPrefabComponent<HarvestNodeController>(path, component => SetFloat(component, "respawnDuration", seconds));
        }

        private static void SetStaticHarvestTimer(string path, float seconds)
        {
            EditPrefabComponent<StaticTimedHarvestNodeController>(path, component => SetFloat(component, "respawnDuration", seconds));
        }

        private static void EditPrefabComponent<T>(string path, Action<T> edit) where T : Component
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component == null)
                {
                    Debug.LogWarning($"Release balance skipped {path}: {typeof(T).Name} is missing.");
                    return;
                }

                edit(component);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupMonsterEconomy()
        {
            string[] guids = AssetDatabase.FindAssets("t:MonsterData", new[] { "Assets/ScriptableObjects/MonsterData" });
            foreach (string guid in guids)
            {
                MonsterData data = AssetDatabase.LoadAssetAtPath<MonsterData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data == null)
                    continue;

                bool isLeafy = string.Equals(data.monsterName, "Leafy", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(data.id, "leafy", StringComparison.OrdinalIgnoreCase);
                data.coinPerTick = 1;
                data.coinTickInterval = isLeafy ? 180f : 240f;
                data.maxStoredCoin = isLeafy ? 5 : 8;
                EditorUtility.SetDirty(data);
            }
        }

        private static void SetupGameplayScene()
        {
            Scene scene = SceneManager.GetSceneByPath(GameplayScenePath);
            bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
            if (openedForSetup)
                scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Additive);

            try
            {
                SetupSceneHarvestTimers(scene);
                SetupFogCosts(scene);
                EnsureZone02Tomato(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (openedForSetup && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void SetupSceneHarvestTimers(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (HarvestNodeController node in root.GetComponentsInChildren<HarvestNodeController>(true))
                {
                    SerializedObject serialized = new SerializedObject(node);
                    SerializedProperty itemData = serialized.FindProperty("itemData");
                    ItemData item = itemData != null ? itemData.objectReferenceValue as ItemData : null;
                    if (item != null && HarvestSecondsByItemId.TryGetValue(item.itemId, out float seconds))
                    {
                        serialized.FindProperty("respawnDuration").floatValue = seconds;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                foreach (StaticTimedHarvestNodeController node in root.GetComponentsInChildren<StaticTimedHarvestNodeController>(true))
                {
                    SerializedObject serialized = new SerializedObject(node);
                    SerializedProperty itemData = serialized.FindProperty("itemData");
                    ItemData item = itemData != null ? itemData.objectReferenceValue as ItemData : null;
                    if (item != null && HarvestSecondsByItemId.TryGetValue(item.itemId, out float seconds))
                    {
                        serialized.FindProperty("respawnDuration").floatValue = seconds;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
        }

        private static void SetupFogCosts(Scene scene)
        {
            FogZoneManager manager = FindInScene<FogZoneManager>(scene);
            if (manager == null)
            {
                Debug.LogError("Release balance could not find FogZoneManager in GameplayScene.");
                return;
            }

            SerializedObject serialized = new SerializedObject(manager);
            SerializedProperty zones = serialized.FindProperty("zones");
            int count = Mathf.Min(zones.arraySize, FogCosts.Length);
            for (int i = 0; i < count; i++)
                zones.GetArrayElementAtIndex(i).FindPropertyRelative("unlockCost").intValue = FogCosts[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureZone02Tomato(Scene scene)
        {
            Transform zone02 = FindTransform(scene, "Zone02");
            Transform harvest = zone02 != null ? FindDescendant(zone02, "Harvest") : null;
            if (harvest == null)
            {
                Debug.LogError("Release balance could not find Zone02/ResourcesNode/Harvest.");
                return;
            }

            if (FindDescendant(harvest, "Tomato_Map_Release") != null)
                return;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/ResourcesNode/ResourcesNode_Map/Tomato_Map.prefab");
            GameObject tomato = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (tomato == null)
                return;

            tomato.name = "Tomato_Map_Release";
            tomato.transform.SetParent(harvest, true);
            tomato.transform.position = new Vector3(8.15f, 2.1f, 0.03082484f);
            EditorUtility.SetDirty(tomato);
        }

        private static void SetFloat(Component component, string propertyName, float value)
        {
            SerializedObject serialized = new SerializedObject(component);
            serialized.FindProperty(propertyName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T result = root.GetComponentInChildren<T>(true);
                if (result != null)
                    return result;
            }
            return null;
        }

        private static Transform FindTransform(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = FindDescendant(root.transform, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root.name == name)
                return root;
            foreach (Transform child in root)
            {
                Transform found = FindDescendant(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
