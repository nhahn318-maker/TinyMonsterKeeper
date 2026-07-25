using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Collider2D))]
public class FogAreaBlocker : MonoBehaviour
{
    private static readonly List<FogAreaBlocker> ActiveBlockers = new List<FogAreaBlocker>();

    [SerializeField] private Collider2D blockerCollider;
    [SerializeField] private Tilemap blockerTilemap;
    [SerializeField] private float pathSampleSpacing = 0.1f;

    private void Awake()
    {
        if (blockerCollider == null)
            blockerCollider = GetComponent<Collider2D>();

        if (blockerTilemap == null)
            blockerTilemap = GetComponent<Tilemap>();
    }

    private void OnEnable()
    {
        if (!ActiveBlockers.Contains(this))
            ActiveBlockers.Add(this);
    }

    private void OnDisable()
    {
        ActiveBlockers.Remove(this);
    }

    public void SetBlocked(bool blocked)
    {
        enabled = blocked;

        if (blockerCollider != null)
            blockerCollider.enabled = blocked;
    }

    public static bool BlocksPoint(Vector2 point)
    {
        for (int i = 0; i < ActiveBlockers.Count; i++)
        {
            if (ActiveBlockers[i].ContainsPoint(point))
                return true;
        }

        return false;
    }

    public static bool BlocksPath(Vector2 from, Vector2 to)
    {
        for (int i = 0; i < ActiveBlockers.Count; i++)
        {
            if (ActiveBlockers[i].IntersectsPath(from, to))
                return true;
        }

        return false;
    }

    private bool ContainsPoint(Vector2 point)
    {
        if (!isActiveAndEnabled)
            return false;

        if (blockerTilemap != null && TilemapHasTileAtWorldPoint(point))
            return true;

        return blockerCollider != null && blockerCollider.enabled && blockerCollider.OverlapPoint(point);
    }

    private bool IntersectsPath(Vector2 from, Vector2 to)
    {
        if (!isActiveAndEnabled)
            return false;

        float distance = Vector2.Distance(from, to);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / Mathf.Max(0.01f, pathSampleSpacing)));

        for (int i = 0; i <= steps; i++)
        {
            Vector2 point = Vector2.Lerp(from, to, i / (float)steps);
            if (ContainsPoint(point))
                return true;
        }

        return false;
    }

    private bool TilemapHasTileAtWorldPoint(Vector2 point)
    {
        Vector3Int cellPosition = blockerTilemap.WorldToCell(point);
        return blockerTilemap.HasTile(cellPosition);
    }
}
