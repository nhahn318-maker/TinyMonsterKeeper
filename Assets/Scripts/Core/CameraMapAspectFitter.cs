using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraMapAspectFitter : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Collider2D mapBoundsCollider;
    [SerializeField] private float desiredOrthographicSize = 5f;
    [SerializeField] private float edgePadding = 0.05f;
    [SerializeField] private Color fallbackBackgroundColor = new Color(0.76f, 0.88f, 0.56f, 1f);

    private float lastAspect;
    private Vector2 lastBoundsSize;

    private void Awake()
    {
        ResolveReferences();
        ApplyFit();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ApplyFit();
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            return;

        Vector2 boundsSize = mapBoundsCollider != null ? mapBoundsCollider.bounds.size : Vector2.zero;
        if (!Mathf.Approximately(targetCamera.aspect, lastAspect) || boundsSize != lastBoundsSize)
            ApplyFit();
    }

    public void ApplyFit()
    {
        ResolveReferences();

        if (targetCamera == null || !targetCamera.orthographic)
            return;

        targetCamera.backgroundColor = fallbackBackgroundColor;

        if (mapBoundsCollider == null)
        {
            targetCamera.orthographicSize = desiredOrthographicSize;
            return;
        }

        Bounds bounds = mapBoundsCollider.bounds;
        float maxSizeByHeight = Mathf.Max(0.1f, bounds.size.y * 0.5f - edgePadding);
        float maxSizeByWidth = Mathf.Max(0.1f, (bounds.size.x * 0.5f - edgePadding) / Mathf.Max(0.01f, targetCamera.aspect));
        targetCamera.orthographicSize = Mathf.Min(desiredOrthographicSize, maxSizeByHeight, maxSizeByWidth);

        lastAspect = targetCamera.aspect;
        lastBoundsSize = bounds.size;
    }

    private void ResolveReferences()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        if (targetCamera == null)
            targetCamera = Camera.main;
    }
}
