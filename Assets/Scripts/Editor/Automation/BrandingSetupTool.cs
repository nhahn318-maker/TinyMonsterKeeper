#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TinyMonsterKeeper.EditorAutomation
{
    public static class BrandingSetupTool
    {
        private const string ScenePath = "Assets/Scenes/MainMenuScene.unity";
        private const string IconPath = "Assets/Arts/Branding/MonsterGarden_AppIcon_Dewli.png";
        private const string LogoPath = "Assets/Arts/Branding/MonsterGarden_Logo_Dewli.png";

        [MenuItem("TinyMonsterKeeper/Automation/Setup App Icon And Logo")]
        public static void Setup()
        {
            ConfigureTexture(IconPath, false);
            ConfigureTexture(LogoPath, true);

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            Sprite logo = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
            if (icon == null || logo == null)
            {
                Debug.LogError("Branding setup failed because the icon or logo asset is missing.");
                return;
            }

            ConfigureAndroidIcons(icon);
            PlayerSettings.SplashScreen.logos = new[]
            {
                PlayerSettings.SplashScreenLogo.Create(2f, logo)
            };

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject title = GameObject.Find("GameTitle");
            if (title == null)
            {
                Debug.LogError("Branding setup failed because GameTitle is missing from MainMenuScene.");
                return;
            }

            foreach (TMP_Text text in title.GetComponentsInChildren<TMP_Text>(true))
                text.gameObject.SetActive(false);

            Transform existing = title.transform.Find("BrandLogo");
            GameObject logoObject = existing != null ? existing.gameObject : new GameObject("BrandLogo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            logoObject.layer = LayerMask.NameToLayer("UI");
            logoObject.transform.SetParent(title.transform, false);

            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(620f, 280f);

            RectTransform logoRect = logoObject.GetComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0.5f, 0.5f);
            logoRect.anchorMax = new Vector2(0.5f, 0.5f);
            logoRect.pivot = new Vector2(0.5f, 0.5f);
            logoRect.anchoredPosition = Vector2.zero;
            logoRect.sizeDelta = new Vector2(600f, 250f);

            Image image = logoObject.GetComponent<Image>();
            image.sprite = logo;
            image.preserveAspect = true;
            image.raycastTarget = false;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Android app icons, splash logo, and Main Menu logo configured.");
        }

        private static void ConfigureAndroidIcons(Texture2D icon)
        {
            NamedBuildTarget target = NamedBuildTarget.Android;
            foreach (PlatformIconKind kind in PlayerSettings.GetSupportedIconKinds(target))
            {
                PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(target, kind);
                foreach (PlatformIcon platformIcon in icons)
                {
                    Texture2D[] textures = Enumerable.Repeat(icon, platformIcon.maxLayerCount).ToArray();
                    platformIcon.SetTextures(textures);
                }
                PlayerSettings.SetPlatformIcons(target, kind, icons);
            }
        }

        private static void ConfigureTexture(string path, bool sprite)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            importer.spriteImportMode = sprite ? SpriteImportMode.Single : SpriteImportMode.None;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }
}
#endif
