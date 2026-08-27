using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace TinyMonsterKeeper.EditorAutomation
{
    public static class MapRedesignPrototypeSetup
    {
        private const string SourceScene = "Assets/Scenes/GameplayScene.unity";
        private const string PrototypeScene = "Assets/Scenes/GameplayMapRedesignPrototype.unity";
        private const string BackgroundPath = "Assets/Design/MapReferences/gameplay-map-redesign-background-v1.png";

        [MenuItem("TinyMonsterKeeper/Automation/Create Map Redesign Prototype")]
        public static void CreateMapRedesignPrototype()
        {
            ConfigureBackgroundImport();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PrototypeScene) != null)
                AssetDatabase.DeleteAsset(PrototypeScene);

            if (!AssetDatabase.CopyAsset(SourceScene, PrototypeScene))
                throw new IOException("Could not copy GameplayScene for the map redesign prototype.");

            AssetDatabase.Refresh();
            Scene scene = EditorSceneManager.OpenScene(PrototypeScene, OpenSceneMode.Single);
            DisableOldGroundAndFog(scene);
            DisableDetachedSummonPathVisuals(scene);
            CreateBackground(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Map redesign prototype created: {PrototypeScene}");
        }

        private static void ConfigureBackgroundImport()
        {
            AssetDatabase.ImportAsset(BackgroundPath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
            if (importer == null)
                throw new IOException("Map redesign background could not be imported as a texture.");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 48f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static void DisableOldGroundAndFog(Scene scene)
        {
            TilemapRenderer[] tilemaps = Object.FindObjectsOfType<TilemapRenderer>(true);
            foreach (TilemapRenderer renderer in tilemaps)
            {
                if (renderer.gameObject.scene != scene)
                    continue;

                string path = GetHierarchyPath(renderer.transform).ToLowerInvariant();
                if (path.Contains("fog")
                    || path.Contains("grass_tilemap")
                    || renderer.name.ToLowerInvariant().Contains("tilemap_grass"))
                    renderer.enabled = false;
            }
        }

        private static void DisableDetachedSummonPathVisuals(Scene scene)
        {
            GameObject summonPath = GameObject.Find("SummonPathArea");
            if (summonPath == null || summonPath.scene != scene)
                return;

            Renderer[] renderers = summonPath.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
                renderer.enabled = false;
        }

        private static void CreateBackground(Scene scene)
        {
            Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            if (background == null)
                throw new IOException("Map redesign background sprite is missing after import.");

            GameObject backgroundObject = new GameObject("MapRedesign_Background");
            SceneManager.MoveGameObjectToScene(backgroundObject, scene);
            backgroundObject.transform.position = new Vector3(-0.55f, 0f, 5f);

            SpriteRenderer renderer = backgroundObject.AddComponent<SpriteRenderer>();
            renderer.sprite = background;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = -32000;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            for (Transform current = transform.parent; current != null; current = current.parent)
                path = current.name + "/" + path;
            return path;
        }
    }
}
