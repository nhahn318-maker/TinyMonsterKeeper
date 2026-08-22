using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinyMonsterKeeper.EditorAutomation
{
    public static class HarvestCountdownSetupTool
    {
        private const string TemplatePrefabPath = "Assets/Prefabs/ResourcesNode/ResourcesNode_Map/Crystal_Cluster_Map.prefab";
        private const string HarvestPrefabFolder = "Assets/Prefabs/ResourcesNode/ResourcesNode_Map";

        [MenuItem("TinyMonsterKeeper/Automation/Setup Harvest Countdown Prefabs")]
        public static void SetupHarvestCountdownPrefabs()
        {
            GameObject templateRoot = PrefabUtility.LoadPrefabContents(TemplatePrefabPath);
            TMP_Text template = FindCountdown(templateRoot.transform);
            if (template == null)
            {
                PrefabUtility.UnloadPrefabContents(templateRoot);
                Debug.LogError("Crystal_Cluster_Map prefab does not contain GrowthTimerText.");
                return;
            }

            CreateHarvestPrefabFromSceneIfMissing("pumpkin", "Pumpkin_Map", template);
            CreateHarvestPrefabFromSceneIfMissing("eggplant", "Eggplant_Map", template);

            int configured = 0;
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { HarvestPrefabFolder });
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
                HarvestNodeController[] nodes = prefabRoot.GetComponentsInChildren<HarvestNodeController>(true);
                bool changed = false;

                foreach (HarvestNodeController node in nodes)
                {
                    if (!IsTargetHarvest(node.name))
                        continue;

                    TMP_Text countdown = FindCountdown(node.transform);
                    if (countdown == null)
                        countdown = CloneCountdown(template, node.transform);
                    else
                        ApplyTemplateStyle(template, countdown);

                    SerializedObject serializedNode = new SerializedObject(node);
                    serializedNode.FindProperty("growthCountdownText").objectReferenceValue = countdown;
                    serializedNode.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(node);
                    configured++;
                    changed = true;
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);

                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            PrefabUtility.UnloadPrefabContents(templateRoot);
            AssetDatabase.SaveAssets();
            Debug.Log($"Harvest prefab countdown setup finished: configured {configured} nodes with the Crystal Cluster text style.");
        }

        private static void CreateHarvestPrefabFromSceneIfMissing(string nameToken, string prefabName, TMP_Text template)
        {
            string prefabPath = $"{HarvestPrefabFolder}/{prefabName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                return;

            HarvestNodeController source = null;
            HarvestNodeController[] sceneNodes = Object.FindObjectsOfType<HarvestNodeController>(true);
            foreach (HarvestNodeController node in sceneNodes)
            {
                if (node.gameObject.scene.IsValid()
                    && node.name.ToLowerInvariant().Contains(nameToken))
                {
                    source = node;
                    break;
                }
            }

            if (source == null)
            {
                Debug.LogWarning($"Could not create {prefabName}.prefab because no {nameToken} harvest exists in the open scene.");
                return;
            }

            GameObject prefabSource = Object.Instantiate(source.gameObject);
            prefabSource.name = prefabName;
            TMP_Text countdown = FindCountdown(prefabSource.transform);
            if (countdown == null)
                countdown = CloneCountdown(template, prefabSource.transform);
            else
                ApplyTemplateStyle(template, countdown);

            HarvestNodeController clonedNode = prefabSource.GetComponent<HarvestNodeController>();
            SerializedObject serializedNode = new SerializedObject(clonedNode);
            serializedNode.FindProperty("growthCountdownText").objectReferenceValue = countdown;
            serializedNode.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefabSource, prefabPath);
            Object.DestroyImmediate(prefabSource);
            Debug.Log($"Created harvest prefab: {prefabPath}");
        }

        [MenuItem("TinyMonsterKeeper/Automation/Setup Harvest Countdown Texts")]
        public static void SetupHarvestCountdownTexts()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded)
            {
                Debug.LogError("Open the gameplay scene before setting up harvest countdown texts.");
                return;
            }

            GameObject templateRoot = PrefabUtility.LoadPrefabContents(TemplatePrefabPath);
            TMP_Text template = FindCountdown(templateRoot.transform);
            if (template == null)
            {
                PrefabUtility.UnloadPrefabContents(templateRoot);
                Debug.LogError("Crystal_Cluster_Map prefab does not contain GrowthTimerText.");
                return;
            }

            HarvestNodeController[] harvestNodes = Object.FindObjectsOfType<HarvestNodeController>(true);
            int configured = 0;
            foreach (HarvestNodeController node in harvestNodes)
            {
                if (node.gameObject.scene != scene)
                    continue;

                if (!IsTargetHarvest(node.name))
                {
                    RemoveGeneratedCountdown(node);
                    continue;
                }

                TMP_Text countdown = FindCountdown(node.transform);
                if (countdown == null)
                {
                    countdown = CloneCountdown(template, node.transform);
                    Undo.RegisterCreatedObjectUndo(countdown.gameObject, "Create harvest countdown text");
                }
                else
                {
                    Undo.RecordObject(countdown, "Style harvest countdown text");
                    Undo.RecordObject(countdown.rectTransform, "Layout harvest countdown text");
                    MeshRenderer existingRenderer = countdown.GetComponent<MeshRenderer>();
                    if (existingRenderer != null)
                        Undo.RecordObject(existingRenderer, "Style harvest countdown renderer");
                    ApplyTemplateStyle(template, countdown);
                }

                Undo.RecordObject(node, "Assign harvest countdown text");
                SerializedObject serializedNode = new SerializedObject(node);
                serializedNode.FindProperty("growthCountdownText").objectReferenceValue = countdown;
                serializedNode.ApplyModifiedProperties();
                EditorUtility.SetDirty(node);
                configured++;
            }

            PrefabUtility.UnloadPrefabContents(templateRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Harvest countdown setup finished: configured {configured} nodes. Test, adjust GrowthTimerText children, then save the scene.");
        }

        private static bool IsTargetHarvest(string objectName)
        {
            string normalized = objectName.ToLowerInvariant();
            return normalized.Contains("tomato")
                || normalized.Contains("pumpkin")
                || normalized.Contains("eggplant")
                || normalized.Contains("egg_plant");
        }

        private static void RemoveGeneratedCountdown(HarvestNodeController node)
        {
            TMP_Text countdown = FindCountdown(node.transform);
            if (countdown == null)
                return;

            SerializedObject serializedNode = new SerializedObject(node);
            SerializedProperty countdownProperty = serializedNode.FindProperty("growthCountdownText");
            if (countdownProperty.objectReferenceValue == countdown)
            {
                Undo.RecordObject(node, "Remove unused harvest countdown reference");
                countdownProperty.objectReferenceValue = null;
                serializedNode.ApplyModifiedProperties();
            }

            Undo.DestroyObjectImmediate(countdown.gameObject);
            EditorUtility.SetDirty(node);
        }

        private static TMP_Text FindCountdown(Transform root)
        {
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in texts)
            {
                if (text.name == "GrowthTimerText")
                    return text;
            }

            return null;
        }

        private static TMP_Text CloneCountdown(TMP_Text template, Transform parent)
        {
            GameObject textObject = Object.Instantiate(template.gameObject, parent);
            textObject.name = "GrowthTimerText";
            textObject.transform.localPosition = template.transform.localPosition;
            textObject.transform.localRotation = template.transform.localRotation;
            textObject.transform.localScale = template.transform.localScale;
            textObject.SetActive(false);
            return textObject.GetComponent<TMP_Text>();
        }

        private static void ApplyTemplateStyle(TMP_Text template, TMP_Text target)
        {
            EditorUtility.CopySerialized(template, target);

            RectTransform templateRect = template.rectTransform;
            RectTransform targetRect = target.rectTransform;
            targetRect.anchorMin = templateRect.anchorMin;
            targetRect.anchorMax = templateRect.anchorMax;
            targetRect.pivot = templateRect.pivot;
            targetRect.anchoredPosition = templateRect.anchoredPosition;
            targetRect.sizeDelta = templateRect.sizeDelta;
            targetRect.localRotation = templateRect.localRotation;
            targetRect.localScale = templateRect.localScale;

            MeshRenderer templateRenderer = template.GetComponent<MeshRenderer>();
            MeshRenderer targetRenderer = target.GetComponent<MeshRenderer>();
            if (templateRenderer != null && targetRenderer != null)
                EditorUtility.CopySerialized(templateRenderer, targetRenderer);

            target.gameObject.SetActive(false);
            EditorUtility.SetDirty(target);
            EditorUtility.SetDirty(targetRect);
            if (targetRenderer != null)
                EditorUtility.SetDirty(targetRenderer);
        }
    }
}
