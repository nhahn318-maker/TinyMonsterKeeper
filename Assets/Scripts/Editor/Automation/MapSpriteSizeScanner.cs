using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinyMonsterKeeper.EditorAutomation
{
    public static class MapSpriteSizeScanner
    {
        private const string ScenePath = "Assets/Scenes/GameplayScene.unity";
        private const string OutputPath = "Assets/Design/MapReferences/gameplay-map-sprite-sizes.csv";

        [MenuItem("TinyMonsterKeeper/Automation/Export Map Sprite Size Report")]
        public static void ExportMapSpriteSizeReport()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SpriteRenderer[] renderers = Object.FindObjectsOfType<SpriteRenderer>(true);
            List<SpriteRenderer> mapRenderers = new List<SpriteRenderer>();

            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer.gameObject.scene == scene && renderer.sprite != null && !IsUiOrEffect(renderer.transform))
                    mapRenderers.Add(renderer);
            }

            mapRenderers.Sort((left, right) => string.CompareOrdinal(GetHierarchyPath(left.transform), GetHierarchyPath(right.transform)));

            StringBuilder csv = new StringBuilder();
            csv.AppendLine("Zone,Hierarchy Path,Sprite Asset,Sprite Name,Pixel Width,Pixel Height,PPU,Local Scale X,Local Scale Y,World Scale X,World Scale Y,World Width,World Height,Sorting Layer,Order,Enabled");

            foreach (SpriteRenderer renderer in mapRenderers)
            {
                Sprite sprite = renderer.sprite;
                Vector3 localScale = renderer.transform.localScale;
                Vector3 worldScale = renderer.transform.lossyScale;
                Vector3 worldSize = renderer.bounds.size;

                AppendCsvRow(csv,
                    FindZone(renderer.transform),
                    GetHierarchyPath(renderer.transform),
                    AssetDatabase.GetAssetPath(sprite),
                    sprite.name,
                    sprite.rect.width.ToString("0", CultureInfo.InvariantCulture),
                    sprite.rect.height.ToString("0", CultureInfo.InvariantCulture),
                    sprite.pixelsPerUnit.ToString("0.###", CultureInfo.InvariantCulture),
                    localScale.x.ToString("0.###", CultureInfo.InvariantCulture),
                    localScale.y.ToString("0.###", CultureInfo.InvariantCulture),
                    worldScale.x.ToString("0.###", CultureInfo.InvariantCulture),
                    worldScale.y.ToString("0.###", CultureInfo.InvariantCulture),
                    worldSize.x.ToString("0.###", CultureInfo.InvariantCulture),
                    worldSize.y.ToString("0.###", CultureInfo.InvariantCulture),
                    renderer.sortingLayerName,
                    renderer.sortingOrder.ToString(CultureInfo.InvariantCulture),
                    renderer.enabled ? "true" : "false");
            }

            File.WriteAllText(OutputPath, csv.ToString(), new UTF8Encoding(true));
            AssetDatabase.Refresh();
            Debug.Log($"Map sprite size report exported: {mapRenderers.Count} renderers -> {OutputPath}");
        }

        private static void AppendCsvRow(StringBuilder csv, params string[] values)
        {
            for (int index = 0; index < values.Length; index++)
            {
                if (index > 0)
                    csv.Append(',');

                string value = values[index] ?? string.Empty;
                csv.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
            }

            csv.AppendLine();
        }

        private static string FindZone(Transform transform)
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                if (current.name.StartsWith("Zone", System.StringComparison.OrdinalIgnoreCase)
                    || current.name.Contains("SummonPath"))
                    return current.name;
            }

            return "Unassigned";
        }

        private static string GetHierarchyPath(Transform transform)
        {
            Stack<string> names = new Stack<string>();
            for (Transform current = transform; current != null; current = current.parent)
                names.Push(current.name);
            return string.Join("/", names);
        }

        private static bool IsUiOrEffect(Transform transform)
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                string objectName = current.name.ToLowerInvariant();
                if (objectName.Contains("canvas")
                    || objectName.Contains("ui")
                    || objectName.Contains("cloudshadow")
                    || objectName.Contains("draglayer"))
                    return true;
            }

            return false;
        }
    }
}
