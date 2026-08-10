using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class YSortByPosition : MonoBehaviour
{
    [Header("Sort Point")]
    [SerializeField] private Transform sortPoint;
    [SerializeField] private float sortYOffset;

    [Header("Sorting")]
    [SerializeField] private int worldBaseOrder = 10000;
    [SerializeField] private int baseOrder;
    [SerializeField] private float unitsToOrder = 100f;
    [SerializeField] private int minOrder = -32768;
    [SerializeField] private int maxOrder = 32767;

    [Header("Targets")]
    [SerializeField] private bool preferSortingGroup = true;
    [SerializeField] private SortingGroup sortingGroup;
    [SerializeField] private SpriteRenderer[] spriteRenderers;

    private int lastOrder = int.MinValue;

    private void Reset()
    {
        sortPoint = transform;
        CacheTargets();
    }

    private void Awake()
    {
        CacheTargets();
        ApplySortingOrder(force: true);
    }

    private void LateUpdate()
    {
        ApplySortingOrder(force: false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (sortPoint == null)
        {
            sortPoint = transform;
        }

        CacheTargets();
        ApplySortingOrder(force: true);
    }
#endif

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform point = sortPoint != null ? sortPoint : transform;
        Vector3 sortPosition = point.position + new Vector3(0f, sortYOffset, 0f);
        int previewOrder = Mathf.Clamp(worldBaseOrder + baseOrder + Mathf.RoundToInt(-sortPosition.y * unitsToOrder), minOrder, maxOrder);

        Gizmos.color = new Color(1f, 0.75f, 0.1f, 1f);
        Gizmos.DrawLine(sortPosition + Vector3.left * 0.18f, sortPosition + Vector3.right * 0.18f);
        Gizmos.DrawLine(sortPosition + Vector3.down * 0.18f, sortPosition + Vector3.up * 0.18f);
        Gizmos.DrawWireSphere(sortPosition, 0.12f);

        Handles.color = new Color(1f, 0.75f, 0.1f, 1f);
        Handles.Label(sortPosition + Vector3.up * 0.25f, $"Y Sort: {previewOrder}");
    }
#endif

    private void CacheTargets()
    {
        if (preferSortingGroup && sortingGroup == null)
        {
            sortingGroup = GetComponent<SortingGroup>();
        }

        if ((!preferSortingGroup || sortingGroup == null) && (spriteRenderers == null || spriteRenderers.Length == 0))
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        }
    }

    private void ApplySortingOrder(bool force)
    {
        Transform point = sortPoint != null ? sortPoint : transform;
        float sortY = point.position.y + sortYOffset;
        int order = Mathf.Clamp(worldBaseOrder + baseOrder + Mathf.RoundToInt(-sortY * unitsToOrder), minOrder, maxOrder);

        if (!force && order == lastOrder)
        {
            return;
        }

        lastOrder = order;

        if (preferSortingGroup && sortingGroup != null)
        {
            sortingGroup.sortingOrder = order;
            return;
        }

        if (spriteRenderers == null)
        {
            return;
        }

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].sortingOrder = order;
            }
        }
    }
}
