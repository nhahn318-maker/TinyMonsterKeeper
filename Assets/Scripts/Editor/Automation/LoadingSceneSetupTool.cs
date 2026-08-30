#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TinyMonsterKeeper.EditorAutomation
{
    public static class LoadingSceneSetupTool
    {
        private const string ScenePath = "Assets/Scenes/LoadingScene.unity";

        [MenuItem("TinyMonsterKeeper/Automation/Setup Loading Scene")]
        public static void SetupLoadingScene()
        {
            ConfigureSprite("Assets/Arts/UI/Loading/LoadingBackground.png", 100);
            ConfigureSprite("Assets/Arts/UI/Loading/LoadingBarFrame.png", 100);
            ConfigureSprite("Assets/Arts/UI/Loading/LoadingBarFill.png", 100);

            Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arts/UI/Loading/LoadingBackground.png");
            Sprite frame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arts/UI/Loading/LoadingBarFrame.png");
            Sprite fill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arts/UI/Loading/LoadingBarFill.png");
            GameObject leafyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monsters/MonNo1_Leafy.prefab");
            if (background == null || frame == null || fill == null || leafyPrefab == null)
            {
                Debug.LogError("Loading scene setup failed because an art asset or Leafy prefab is missing.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera camera = CreateCamera();
            SpriteRenderer backgroundRenderer = CreateSprite("Background", background, Vector3.zero, 8f, -10);

            GameObject visuals = new GameObject("LoadingVisuals");
            SpriteRenderer fillRenderer = CreateSprite("ProgressFill", fill, new Vector3(-2.75f, -1.8f, 0f), 5.45f, 3);
            fillRenderer.transform.SetParent(visuals.transform, true);
            SpriteRenderer frameRenderer = CreateSprite("ProgressFrame", frame, new Vector3(0f, -1.8f, 0f), 6.4f, 2);
            frameRenderer.transform.SetParent(visuals.transform, true);

            GameObject leafy = (GameObject)PrefabUtility.InstantiatePrefab(leafyPrefab, scene);
            leafy.name = "LeafyLoadingRunner";
            leafy.transform.SetParent(visuals.transform, false);
            leafy.transform.localPosition = new Vector3(-2.75f, -0.491f, 0f);
            leafy.transform.localScale = Vector3.one * 1.35f;
            PrepareLeafyForLoading(leafy);

            TextMeshPro loadingText = CreateLoadingText(visuals.transform);
            GameObject controllerObject = new GameObject("LoadingSceneController");
            LoadingSceneController controller = controllerObject.AddComponent<LoadingSceneController>();
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("backgroundRenderer").objectReferenceValue = backgroundRenderer;
            serializedController.FindProperty("fillTransform").objectReferenceValue = fillRenderer.transform;
            serializedController.FindProperty("leafyTransform").objectReferenceValue = leafy.transform;
            serializedController.FindProperty("loadingText").objectReferenceValue = loadingText;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/MainMenuScene.unity", true),
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene("Assets/Scenes/GameplayScene.unity", true)
            };
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = controllerObject;
            Debug.Log("Loading scene setup finished. Guest now loads LoadingScene before GameplayScene.");
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(190, 220, 125, 255);
            camera.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private static SpriteRenderer CreateSprite(string name, Sprite sprite, Vector3 position, float width, int order)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.position = position;
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            float scale = width / sprite.bounds.size.x;
            gameObject.transform.localScale = new Vector3(scale, scale, 1f);
            return renderer;
        }

        private static TextMeshPro CreateLoadingText(Transform parent)
        {
            GameObject textObject = new GameObject("LoadingText");
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = new Vector3(0f, -2.65f, 0f);
            TextMeshPro text = textObject.AddComponent<TextMeshPro>();
            text.text = "Loading... 0%";
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 4.5f;
            text.color = new Color32(82, 100, 32, 255);
            text.rectTransform.sizeDelta = new Vector2(8f, 1f);
            text.GetComponent<MeshRenderer>().sortingOrder = 5;
            return text;
        }

        private static void PrepareLeafyForLoading(GameObject leafy)
        {
            foreach (MonoBehaviour behaviour in leafy.GetComponentsInChildren<MonoBehaviour>(true))
                behaviour.enabled = false;
            foreach (NavMeshAgent agent in leafy.GetComponentsInChildren<NavMeshAgent>(true))
                agent.enabled = false;
            foreach (Collider2D collider in leafy.GetComponentsInChildren<Collider2D>(true))
                collider.enabled = false;
            foreach (Rigidbody2D body in leafy.GetComponentsInChildren<Rigidbody2D>(true))
                body.simulated = false;
            foreach (SpriteRenderer renderer in leafy.GetComponentsInChildren<SpriteRenderer>(true))
                renderer.sortingOrder = 4;

            SortingGroup sortingGroup = leafy.GetComponent<SortingGroup>();
            if (sortingGroup != null)
                sortingGroup.sortingOrder = 1;

            Animator animator = leafy.GetComponentInChildren<Animator>(true);
            if (animator != null)
                animator.enabled = true;

            foreach (Transform child in leafy.transform.Cast<Transform>())
            {
                if (child.name.Contains("Heart") || child.name.Contains("Star") || child.name.Contains("Coin"))
                    child.gameObject.SetActive(false);
            }
        }

        private static void ConfigureSprite(string path, float pixelsPerUnit)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }
}
#endif
