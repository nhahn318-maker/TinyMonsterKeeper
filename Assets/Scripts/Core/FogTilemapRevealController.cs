using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class FogTilemapRevealController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap fogTilemap;
    [SerializeField] private TilemapRenderer fogRenderer;

    [Header("Reveal Motion")]
    [SerializeField] private float revealDuration = 0.45f;
    [SerializeField] private Vector3 driftOffset = new Vector3(0.75f, 0f, 0f);
    [SerializeField] private float tileStaggerDelay = 0.025f;
    [SerializeField] private int tempSortingOrderOffset = 2;

    private Transform tempRoot;
    private readonly HashSet<Vector3Int> revealingCells = new HashSet<Vector3Int>();

    private void Awake()
    {
        if (fogTilemap == null)
            fogTilemap = GetComponent<Tilemap>();

        if (fogRenderer == null)
            fogRenderer = GetComponent<TilemapRenderer>();

        CreateTempRootIfNeeded();
    }

    [ContextMenu("Reveal All Fog")]
    public void RevealAll()
    {
        RevealAll(driftOffset);
    }

    public void RevealAll(Vector3 revealDriftOffset)
    {
        if (fogTilemap == null)
            return;

        RevealCellBounds(fogTilemap.cellBounds, revealDriftOffset);
    }

    public void ClearAll()
    {
        if (fogTilemap == null)
            return;

        fogTilemap.ClearAllTiles();
        revealingCells.Clear();
    }

    public void RevealWorldBounds(Bounds worldBounds)
    {
        RevealWorldBounds(worldBounds, driftOffset);
    }

    public void RevealWorldBounds(Bounds worldBounds, Vector3 revealDriftOffset)
    {
        if (fogTilemap == null)
            return;

        Vector3Int minCell = fogTilemap.WorldToCell(worldBounds.min);
        Vector3Int maxCell = fogTilemap.WorldToCell(worldBounds.max);

        BoundsInt cellBounds = new BoundsInt(
            minCell.x,
            minCell.y,
            minCell.z,
            maxCell.x - minCell.x + 1,
            maxCell.y - minCell.y + 1,
            maxCell.z - minCell.z + 1
        );

        RevealCellBounds(cellBounds, revealDriftOffset);
    }

    public void RevealColliderBounds(Collider2D boundsCollider)
    {
        RevealColliderBounds(boundsCollider, driftOffset);
    }

    public void RevealColliderBounds(Collider2D boundsCollider, Vector3 revealDriftOffset)
    {
        if (boundsCollider == null)
            return;

        RevealWorldBounds(boundsCollider.bounds, revealDriftOffset);
    }

    public void ClearColliderBounds(Collider2D boundsCollider)
    {
        if (boundsCollider == null || fogTilemap == null)
            return;

        ClearWorldBounds(boundsCollider.bounds);
    }

    public void ClearWorldBounds(Bounds worldBounds)
    {
        if (fogTilemap == null)
            return;

        Vector3Int minCell = fogTilemap.WorldToCell(worldBounds.min);
        Vector3Int maxCell = fogTilemap.WorldToCell(worldBounds.max);

        BoundsInt cellBounds = new BoundsInt(
            minCell.x,
            minCell.y,
            minCell.z,
            maxCell.x - minCell.x + 1,
            maxCell.y - minCell.y + 1,
            maxCell.z - minCell.z + 1
        );

        ClearCellBounds(cellBounds);
    }

    public void RevealCircle(Vector3 worldCenter, float radius)
    {
        RevealCircle(worldCenter, radius, driftOffset);
    }

    public void RevealCircle(Vector3 worldCenter, float radius, Vector3 revealDriftOffset)
    {
        Bounds worldBounds = new Bounds(worldCenter, Vector3.one * radius * 2f);
        RevealWorldBounds(worldBounds, revealDriftOffset);
    }

    public void RevealCellBounds(BoundsInt cellBounds)
    {
        RevealCellBounds(cellBounds, driftOffset);
    }

    public void RevealCellBounds(BoundsInt cellBounds, Vector3 revealDriftOffset)
    {
        if (fogTilemap == null)
            return;

        CreateTempRootIfNeeded();

        List<Vector3Int> cells = CollectFogCells(cellBounds);
        for (int i = 0; i < cells.Count; i++)
            StartCoroutine(RevealCellRoutine(cells[i], i * tileStaggerDelay, revealDriftOffset));
    }

    public void ClearCellBounds(BoundsInt cellBounds)
    {
        if (fogTilemap == null)
            return;

        foreach (Vector3Int cellPosition in cellBounds.allPositionsWithin)
        {
            fogTilemap.SetTile(cellPosition, null);
            revealingCells.Remove(cellPosition);
        }
    }

    private List<Vector3Int> CollectFogCells(BoundsInt cellBounds)
    {
        List<Vector3Int> cells = new List<Vector3Int>();

        foreach (Vector3Int cellPosition in cellBounds.allPositionsWithin)
        {
            if (revealingCells.Contains(cellPosition))
                continue;

            if (!fogTilemap.HasTile(cellPosition))
                continue;

            cells.Add(cellPosition);
            revealingCells.Add(cellPosition);
        }

        return cells;
    }

    private IEnumerator RevealCellRoutine(Vector3Int cellPosition, float delay, Vector3 revealDriftOffset)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        TileBase tile = fogTilemap.GetTile(cellPosition);
        Sprite sprite = fogTilemap.GetSprite(cellPosition);
        Color color = fogTilemap.GetColor(cellPosition);

        if (tile == null || sprite == null)
        {
            revealingCells.Remove(cellPosition);
            yield break;
        }

        Vector3 startPosition = fogTilemap.GetCellCenterWorld(cellPosition);
        GameObject tempTile = CreateTempTile(sprite, color, startPosition);

        fogTilemap.SetTile(cellPosition, null);

        yield return AnimateTempTile(tempTile, startPosition, revealDriftOffset);

        revealingCells.Remove(cellPosition);
    }

    private GameObject CreateTempTile(Sprite sprite, Color color, Vector3 worldPosition)
    {
        GameObject tempTile = new GameObject("RevealingFogTile");
        tempTile.transform.SetParent(tempRoot, false);
        tempTile.transform.position = worldPosition;

        SpriteRenderer spriteRenderer = tempTile.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;

        if (fogRenderer != null)
        {
            spriteRenderer.sortingLayerID = fogRenderer.sortingLayerID;
            spriteRenderer.sortingOrder = fogRenderer.sortingOrder + tempSortingOrderOffset;
        }

        return tempTile;
    }

    private IEnumerator AnimateTempTile(GameObject tempTile, Vector3 startPosition, Vector3 revealDriftOffset)
    {
        if (tempTile == null)
            yield break;

        SpriteRenderer spriteRenderer = tempTile.GetComponent<SpriteRenderer>();
        Vector3 endPosition = startPosition + revealDriftOffset;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        float duration = Mathf.Max(0.01f, revealDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);

            tempTile.transform.position = Vector3.Lerp(startPosition, endPosition, eased);

            if (spriteRenderer != null)
            {
                Color color = startColor;
                color.a = Mathf.Lerp(startColor.a, 0f, eased);
                spriteRenderer.color = color;
            }

            yield return null;
        }

        Destroy(tempTile);
    }

    private void CreateTempRootIfNeeded()
    {
        if (tempRoot != null)
            return;

        GameObject rootObject = new GameObject("FogRevealTempRoot");
        rootObject.transform.SetParent(transform, false);
        tempRoot = rootObject.transform;
    }
}
