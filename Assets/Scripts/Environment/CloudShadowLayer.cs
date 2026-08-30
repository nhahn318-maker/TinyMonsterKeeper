using System.Collections.Generic;
using UnityEngine;

public class CloudShadowLayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Look")]
    [SerializeField, Range(0f, 0.5f)] private float shadowAlpha = 0.12f;
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 1f);
    [SerializeField] private int sortingOrder = 60;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private Sprite[] cloudSprites;
    [SerializeField] private Vector2 cloudScaleRange = new Vector2(3.5f, 6f);
    [SerializeField] private Vector2 crossOffsetRange = new Vector2(-3.5f, 3.5f);

    [Header("Motion")]
    [SerializeField] private bool followCamera = true;
    [SerializeField] private Vector2 direction = new Vector2(1f, -0.25f);
    [SerializeField] private float speed = 0.22f;
    [SerializeField] private float worldTileSize = 14f;
    [SerializeField, Min(1)] private int tileCount = 3;
    [SerializeField, Min(1)] private int crossTileCount = 3;
    [SerializeField, Min(0.5f)] private float crossTileSpacing = 8f;
    [SerializeField] private Vector2 coverageCenterOffset = new Vector2(0f, 3f);

    [Header("Generated Texture")]
    [SerializeField, Min(32)] private int textureSize = 256;
    [SerializeField, Min(1)] private int cloudBlobCount = 28;
    [SerializeField] private Vector2 blobRadiusRange = new Vector2(0.08f, 0.24f);
    [SerializeField] private int seed = 715;
    [SerializeField] private FilterMode filterMode = FilterMode.Bilinear;

    private readonly List<SpriteRenderer> tileRenderers = new List<SpriteRenderer>();
    private Sprite generatedSprite;
    private Texture2D generatedTexture;
    private Vector2 normalizedDirection = Vector2.right;
    private float scrollOffset;

    private void OnEnable()
    {
        Build();
    }

    private void OnDisable()
    {
        SetTilesActive(false);
    }

    private void Update()
    {
        if (!HasCloudSprites() && generatedSprite == null)
            Build();

        scrollOffset = Mathf.Repeat(scrollOffset + speed * Time.deltaTime, worldTileSize);
        UpdateTilePositions();
    }

    [ContextMenu("Rebuild Cloud Shadow")]
    public void Build()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        normalizedDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        textureSize = Mathf.Max(32, textureSize);
        tileCount = Mathf.Max(1, tileCount);
        crossTileCount = Mathf.Max(1, crossTileCount);
        crossTileSpacing = Mathf.Max(0.5f, crossTileSpacing);
        worldTileSize = Mathf.Max(1f, worldTileSize);

        if (!HasCloudSprites())
            CreateTextureAndSprite();

        EnsureTileRenderers();
        ApplyRendererSettings();
        UpdateTilePositions();
        SetTilesActive(isActiveAndEnabled);
    }

    public void SetAlpha(float alpha)
    {
        shadowAlpha = Mathf.Clamp01(alpha);
        ApplyRendererSettings();
    }

    private void CreateTextureAndSprite()
    {
        if (generatedSprite != null)
        {
            DestroyCloudResource(generatedSprite);
            generatedSprite = null;
        }

        if (generatedTexture != null)
        {
            DestroyCloudResource(generatedTexture);
            generatedTexture = null;
        }

        generatedTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        generatedTexture.name = "Generated Cloud Shadow Mask";
        generatedTexture.wrapMode = TextureWrapMode.Clamp;
        generatedTexture.filterMode = filterMode;

        Color[] pixels = new Color[textureSize * textureSize];
        Vector2[] centers = new Vector2[cloudBlobCount];
        float[] radii = new float[cloudBlobCount];

        Random.State previousState = Random.state;
        Random.InitState(seed);

        for (int i = 0; i < cloudBlobCount; i++)
        {
            centers[i] = new Vector2(Random.value, Random.value);
            radii[i] = Random.Range(blobRadiusRange.x, blobRadiusRange.y);
        }

        Random.state = previousState;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 uv = new Vector2((float)x / (textureSize - 1), (float)y / (textureSize - 1));
                float alpha = 0f;

                for (int i = 0; i < cloudBlobCount; i++)
                {
                    float distance = Vector2.Distance(uv, centers[i]);
                    float radius = Mathf.Max(0.001f, radii[i]);
                    float blob = 1f - Mathf.SmoothStep(radius * 0.35f, radius, distance);
                    alpha = Mathf.Max(alpha, blob);
                }

                alpha = Mathf.Pow(Mathf.Clamp01(alpha), 1.6f);
                pixels[y * textureSize + x] = new Color(0f, 0f, 0f, alpha);
            }
        }

        generatedTexture.SetPixels(pixels);
        generatedTexture.Apply(false, false);

        generatedSprite = Sprite.Create(
            generatedTexture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize / worldTileSize
        );
        generatedSprite.name = "Generated Cloud Shadow Sprite";
    }

    private void EnsureTileRenderers()
    {
        for (int i = tileRenderers.Count - 1; i >= 0; i--)
        {
            if (tileRenderers[i] == null)
                tileRenderers.RemoveAt(i);
        }

        int requiredTileCount = tileCount * crossTileCount;
        while (tileRenderers.Count < requiredTileCount)
        {
            GameObject tile = new GameObject($"CloudShadowTile_{tileRenderers.Count + 1}");
            tile.transform.SetParent(transform, false);
            tileRenderers.Add(tile.AddComponent<SpriteRenderer>());
        }

        for (int i = tileRenderers.Count - 1; i >= requiredTileCount; i--)
        {
            if (tileRenderers[i] != null)
                DestroyCloudResource(tileRenderers[i].gameObject);

            tileRenderers.RemoveAt(i);
        }
    }

    private void ApplyRendererSettings()
    {
        Color color = shadowColor;
        color.a = shadowAlpha;

        for (int i = 0; i < tileRenderers.Count; i++)
        {
            SpriteRenderer tileRenderer = tileRenderers[i];
            if (tileRenderer == null)
                continue;

            Sprite sprite = HasCloudSprites()
                ? cloudSprites[i % cloudSprites.Length]
                : generatedSprite;

            tileRenderer.sprite = sprite;
            tileRenderer.color = color;
            tileRenderer.sortingOrder = sortingOrder;

            if (!string.IsNullOrEmpty(sortingLayerName))
                tileRenderer.sortingLayerName = sortingLayerName;
        }
    }

    private void UpdateTilePositions()
    {
        Vector3 center = transform.position;

        if (followCamera && targetCamera != null)
        {
            Vector3 cameraPosition = targetCamera.transform.position;
            center = new Vector3(cameraPosition.x, cameraPosition.y, transform.position.z);
        }

        center += new Vector3(coverageCenterOffset.x, coverageCenterOffset.y, 0f);
        float travelMiddle = (tileCount - 1) * 0.5f;
        float crossMiddle = (crossTileCount - 1) * 0.5f;
        Vector3 travel = new Vector3(normalizedDirection.x, normalizedDirection.y, 0f);
        Vector3 cross = new Vector3(-normalizedDirection.y, normalizedDirection.x, 0f);

        for (int i = 0; i < tileRenderers.Count; i++)
        {
            SpriteRenderer tileRenderer = tileRenderers[i];
            if (tileRenderer == null)
                continue;

            int travelIndex = i % tileCount;
            int crossIndex = i / tileCount;
            float distance = (travelIndex - travelMiddle) * worldTileSize + scrollOffset;
            float rowOffset = (crossIndex - crossMiddle) * crossTileSpacing;
            float jitter = Mathf.Lerp(crossOffsetRange.x, crossOffsetRange.y, GetStable01(i, 17));
            float crossOffset = rowOffset + jitter;
            float scale = Mathf.Lerp(cloudScaleRange.x, cloudScaleRange.y, GetStable01(i, 53));

            tileRenderer.transform.position = center + travel * distance + cross * crossOffset;
            tileRenderer.transform.localScale = HasCloudSprites() ? Vector3.one * scale : Vector3.one;
        }
    }

    private void SetTilesActive(bool active)
    {
        foreach (SpriteRenderer tileRenderer in tileRenderers)
        {
            if (tileRenderer != null)
                tileRenderer.gameObject.SetActive(active);
        }
    }

    private void DestroyCloudResource(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    private bool HasCloudSprites()
    {
        if (cloudSprites == null || cloudSprites.Length == 0)
            return false;

        for (int i = 0; i < cloudSprites.Length; i++)
        {
            if (cloudSprites[i] != null)
                return true;
        }

        return false;
    }

    private float GetStable01(int index, int salt)
    {
        int value = seed + index * 73856093 + salt * 19349663;
        value ^= value << 13;
        value ^= value >> 17;
        value ^= value << 5;
        return (value & 0x7fffffff) / (float)int.MaxValue;
    }
}
