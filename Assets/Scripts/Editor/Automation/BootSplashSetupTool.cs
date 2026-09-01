#if UNITY_EDITOR
using TMPro;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinyMonsterKeeper.EditorAutomation
{
    public static class BootSplashSetupTool
    {
        private const string SplashScenePath = "Assets/Scenes/SplashScene.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenuScene.unity";
        private const string DewliSpritePath = "Assets/Arts/Monsters/MonNo5_Dewli/Dewli_Idle_Sheet.png";

        [MenuItem("TinyMonsterKeeper/Automation/Setup Boot Splash Scene")]
        public static void Setup()
        {
            RestoreMainMenu();

            Sprite dewli = AssetDatabase.LoadAllAssetsAtPath(DewliSpritePath)
                .OfType<Sprite>()
                .FirstOrDefault(sprite => sprite.name == "Dewli_Idle_Sheet_0");
            if (dewli == null)
            {
                Debug.LogError("Boot splash setup failed because Dewli_Idle_Sheet_0 is missing.");
                return;
            }

            PlayerSettings.SplashScreen.logos = new PlayerSettings.SplashScreenLogo[0];
            PlayerSettings.SplashScreen.showUnityLogo = false;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(194, 224, 132, 255);

            GameObject canvasObject = new GameObject("SplashCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backgroundObject.transform.SetParent(canvasObject.transform, false);
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            backgroundObject.GetComponent<Image>().color = new Color32(194, 224, 132, 255);

            GameObject dewliObject = new GameObject("Dewli", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            dewliObject.transform.SetParent(canvasObject.transform, false);
            RectTransform dewliRect = dewliObject.GetComponent<RectTransform>();
            dewliRect.anchorMin = new Vector2(0.5f, 0.5f);
            dewliRect.anchorMax = new Vector2(0.5f, 0.5f);
            dewliRect.pivot = new Vector2(0.5f, 0.5f);
            dewliRect.anchoredPosition = Vector2.zero;
            dewliRect.sizeDelta = new Vector2(360f, 360f);
            Image dewliImage = dewliObject.GetComponent<Image>();
            dewliImage.sprite = dewli;
            dewliImage.preserveAspect = true;
            dewliImage.raycastTarget = false;

            GameObject controllerObject = new GameObject("BootSplashController", typeof(BootSplashController));
            SerializedObject controller = new SerializedObject(controllerObject.GetComponent<BootSplashController>());
            controller.FindProperty("logoGroup").objectReferenceValue = dewliObject.GetComponent<CanvasGroup>();
            controller.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, SplashScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SplashScenePath, true),
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene("Assets/Scenes/LoadingScene.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/GameplayScene.unity", true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("Boot Splash Scene configured before MainMenuScene.");
        }

        private static void RestoreMainMenu()
        {
            Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            GameObject title = GameObject.Find("GameTitle");
            if (title == null)
                return;

            Transform brandLogo = title.transform.Find("BrandLogo");
            if (brandLogo != null)
                Object.DestroyImmediate(brandLogo.gameObject);

            foreach (TMP_Text text in title.GetComponentsInChildren<TMP_Text>(true))
                text.gameObject.SetActive(true);

            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(100f, 100f);
            titleRect.anchoredPosition = Vector2.zero;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif
