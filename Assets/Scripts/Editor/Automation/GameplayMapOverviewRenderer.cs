using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TinyMonsterKeeper.EditorAutomation
{
    public static class GameplayMapOverviewRenderer
    {
        private const string ScenePath = "Assets/Scenes/GameplayScene.unity";
        private const string OutputFolder = "Assets/Design/MapReferences";
        private const float Padding = 1.5f;
        private const float PixelsPerUnit = 48f;
        private const int MaximumDimension = 8192;

        [MenuItem("TinyMonsterKeeper/Automation/Render Current Gameplay Map Overview")]
        public static void RenderFromMenu()
        {
            RenderCurrentGameplayMapOverview();
        }

        public static void RenderCurrentGameplayMapOverview()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(OutputFolder);

            List<GameObject> temporarilyDisabled = new List<GameObject>();
            DisableCanvases(scene, temporarilyDisabled);

            Bounds mapBounds = CalculateMapBounds(scene);
            RenderOverview(scene, mapBounds, false, Path.Combine(OutputFolder, "gameplay-map-full-clear.png"), temporarilyDisabled);
            RenderOverview(scene, mapBounds, true, Path.Combine(OutputFolder, "gameplay-map-full-with-fog.png"), temporarilyDisabled);

            RestoreObjects(temporarilyDisabled);
            AssetDatabase.Refresh();
            Debug.Log($"Gameplay map overview rendered. Bounds center={mapBounds.center}, size={mapBounds.size}.");
        }

        private static Bounds CalculateMapBounds(Scene scene)
        {
            Renderer[] renderers = Object.FindObjectsOfType<Renderer>(true);
            bool hasBounds = false;
            Bounds bounds = default;

            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.gameObject.scene != scene)
                    continue;
                if (renderer is ParticleSystemRenderer || IsExcludedFromMapBounds(renderer.transform))
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
                throw new System.InvalidOperationException("GameplayScene has no active world renderers.");

            bounds.Expand(new Vector3(Padding * 2f, Padding * 2f, 0f));
            return bounds;
        }

        private static void RenderOverview(
            Scene scene,
            Bounds bounds,
            bool includeFog,
            string outputPath,
            List<GameObject> temporarilyDisabled)
        {
            int restoreStartIndex = temporarilyDisabled.Count;
            SetFogVisible(scene, includeFog, temporarilyDisabled);

            float aspect = Mathf.Max(0.01f, bounds.size.x / bounds.size.y);
            int width = Mathf.Clamp(Mathf.RoundToInt(bounds.size.x * PixelsPerUnit), 512, MaximumDimension);
            int height = Mathf.Clamp(Mathf.RoundToInt(width / aspect), 512, MaximumDimension);
            if (height == MaximumDimension)
                width = Mathf.Clamp(Mathf.RoundToInt(height * aspect), 512, MaximumDimension);

            GameObject cameraObject = new GameObject("__MapOverviewCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = bounds.size.y * 0.5f;
            camera.aspect = (float)width / height;
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -100f);
            camera.transform.rotation = Quaternion.identity;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 200f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(47, 72, 105, 255);
            camera.allowHDR = false;
            camera.allowMSAA = false;

            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point
            };
            camera.targetTexture = target;
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply(false);
            File.WriteAllBytes(outputPath, image.EncodeToPNG());

            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(cameraObject);

            RestoreObjectsFromIndex(temporarilyDisabled, restoreStartIndex);
        }

        private static void DisableCanvases(Scene scene, List<GameObject> disabled)
        {
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                if (canvas.gameObject.scene == scene && canvas.gameObject.activeSelf)
                {
                    canvas.gameObject.SetActive(false);
                    disabled.Add(canvas.gameObject);
                }
            }
        }

        private static void SetFogVisible(Scene scene, bool visible, List<GameObject> disabled)
        {
            if (visible)
                return;

            Renderer[] renderers = Object.FindObjectsOfType<Renderer>(true);
            HashSet<GameObject> fogRoots = new HashSet<GameObject>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer.gameObject.scene != scene || !IsFog(renderer.transform))
                    continue;

                GameObject fogObject = renderer.gameObject;
                if (fogObject.activeSelf && fogRoots.Add(fogObject))
                {
                    fogObject.SetActive(false);
                    disabled.Add(fogObject);
                }
            }
        }

        private static bool IsFog(Transform transform)
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                string objectName = current.name.ToLowerInvariant();
                if (objectName.Contains("fog") || objectName.Contains("dimblocker"))
                    return true;
            }

            return false;
        }

        private static bool IsExcludedFromMapBounds(Transform transform)
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                string objectName = current.name.ToLowerInvariant();
                if (objectName.Contains("cloudshadow")
                    || objectName.Contains("draglayer")
                    || objectName.StartsWith("__"))
                    return true;
            }

            return false;
        }

        private static void RestoreObjects(List<GameObject> disabled)
        {
            RestoreObjectsFromIndex(disabled, 0);
        }

        private static void RestoreObjectsFromIndex(List<GameObject> disabled, int startIndex)
        {
            for (int index = disabled.Count - 1; index >= startIndex; index--)
            {
                if (disabled[index] != null)
                    disabled[index].SetActive(true);
                disabled.RemoveAt(index);
            }
        }
    }
}
