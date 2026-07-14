using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraMapDragController : MonoBehaviour
{
    public static CameraMapDragController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Collider2D mapBoundsCollider;
    [SerializeField] private Transform mapRoot;

    [Header("Drag")]
    [SerializeField] private bool allowDrag = true;
    [SerializeField] private bool blockDragWhenCookingPanelOpen = true;
    [SerializeField] private bool blockDragOverUI = false;
    [SerializeField] private float dragSensitivity = 1f;
    [SerializeField] private float minDragDistancePixels = 6f;

    [Header("Feel")]
    [SerializeField] private bool useInertia = true;
    [SerializeField] private float inertiaDamping = 8f;
    [SerializeField] private float maxInertiaSpeed = 20f;

    [Header("Debug")]
    [SerializeField] private bool logSetupWarnings = true;

    private Bounds calculatedMapBounds;
    private Vector3 lastPointerWorldPosition;
    private Vector2 pointerStartScreenPosition;
    private Vector3 velocity;
    private bool isDragging;
    private bool hasDraggedPastThreshold;
    private bool inputLocked;
    private Coroutine inputLockRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        RefreshMapBounds();
        ClampCameraInsideBounds();
        LogSetupWarnings();
    }

    private void LateUpdate()
    {
        if (targetCamera == null || inputLocked || !allowDrag)
        {
            velocity = Vector3.zero;
            return;
        }

        HandlePointerInput();
        ApplyInertia();
    }

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;

        if (locked)
        {
            isDragging = false;
            hasDraggedPastThreshold = false;
            velocity = Vector3.zero;
        }
    }

    public void LockInputForSeconds(float seconds)
    {
        if (inputLockRoutine != null)
            StopCoroutine(inputLockRoutine);

        inputLockRoutine = StartCoroutine(InputLockRoutine(seconds));
    }

    public void SnapInsideBounds()
    {
        RefreshMapBounds();
        ClampCameraInsideBounds();
    }

    private IEnumerator InputLockRoutine(float seconds)
    {
        SetInputLocked(true);
        yield return new WaitForSeconds(seconds);
        SetInputLocked(false);
        inputLockRoutine = null;
    }

    private void HandlePointerInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
                BeginDrag(touch.position, touch.fingerId);
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                ContinueDrag(touch.position);
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                EndDrag();

            return;
        }

        if (Input.GetMouseButtonDown(0))
            BeginDrag(Input.mousePosition, -1);
        else if (Input.GetMouseButton(0))
            ContinueDrag(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0))
            EndDrag();
    }

    private void BeginDrag(Vector2 screenPosition, int pointerId)
    {
        if (blockDragWhenCookingPanelOpen &&
            CookingPotPanelUI.Instance != null &&
            CookingPotPanelUI.Instance.IsOpen)
        {
            return;
        }

        if (blockDragOverUI && IsPointerOverUI(pointerId))
            return;

        isDragging = true;
        hasDraggedPastThreshold = false;
        pointerStartScreenPosition = screenPosition;
        lastPointerWorldPosition = ScreenToWorldOnCameraPlane(screenPosition);
        velocity = Vector3.zero;
    }

    private void ContinueDrag(Vector2 screenPosition)
    {
        if (!isDragging)
            return;

        if (!hasDraggedPastThreshold &&
            Vector2.Distance(pointerStartScreenPosition, screenPosition) < minDragDistancePixels)
        {
            return;
        }

        hasDraggedPastThreshold = true;

        Vector3 currentPointerWorldPosition = ScreenToWorldOnCameraPlane(screenPosition);
        Vector3 delta = lastPointerWorldPosition - currentPointerWorldPosition;
        delta.z = 0f;
        delta *= dragSensitivity;

        targetCamera.transform.position += delta;
        ClampCameraInsideBounds();

        velocity = delta / Mathf.Max(Time.deltaTime, 0.0001f);
        velocity = Vector3.ClampMagnitude(velocity, maxInertiaSpeed);
        lastPointerWorldPosition = ScreenToWorldOnCameraPlane(screenPosition);
    }

    private void EndDrag()
    {
        isDragging = false;
        hasDraggedPastThreshold = false;

        if (!useInertia)
            velocity = Vector3.zero;
    }

    private void ApplyInertia()
    {
        if (isDragging || !useInertia || velocity.sqrMagnitude <= 0.001f)
            return;

        targetCamera.transform.position += velocity * Time.deltaTime;
        ClampCameraInsideBounds();
        velocity = Vector3.Lerp(velocity, Vector3.zero, inertiaDamping * Time.deltaTime);
    }

    private Vector3 ScreenToWorldOnCameraPlane(Vector2 screenPosition)
    {
        Vector3 worldPosition = targetCamera.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, -targetCamera.transform.position.z)
        );

        worldPosition.z = targetCamera.transform.position.z;
        return worldPosition;
    }

    private bool IsPointerOverUI(int pointerId)
    {
        if (EventSystem.current == null)
            return false;

        if (pointerId >= 0)
            return EventSystem.current.IsPointerOverGameObject(pointerId);

        return EventSystem.current.IsPointerOverGameObject();
    }

    private void RefreshMapBounds()
    {
        if (mapBoundsCollider != null)
        {
            calculatedMapBounds = mapBoundsCollider.bounds;
            return;
        }

        if (mapRoot == null)
        {
            calculatedMapBounds = new Bounds(Vector3.zero, Vector3.zero);
            return;
        }

        Renderer[] renderers = mapRoot.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            calculatedMapBounds = new Bounds(mapRoot.position, Vector3.zero);
            return;
        }

        calculatedMapBounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            calculatedMapBounds.Encapsulate(renderers[i].bounds);
    }

    private void ClampCameraInsideBounds()
    {
        if (calculatedMapBounds.size == Vector3.zero || targetCamera == null || !targetCamera.orthographic)
            return;

        float cameraHeight = targetCamera.orthographicSize;
        float cameraWidth = cameraHeight * targetCamera.aspect;

        float minX = calculatedMapBounds.min.x + cameraWidth;
        float maxX = calculatedMapBounds.max.x - cameraWidth;
        float minY = calculatedMapBounds.min.y + cameraHeight;
        float maxY = calculatedMapBounds.max.y - cameraHeight;

        Vector3 position = targetCamera.transform.position;

        position.x = minX > maxX
            ? calculatedMapBounds.center.x
            : Mathf.Clamp(position.x, minX, maxX);

        position.y = minY > maxY
            ? calculatedMapBounds.center.y
            : Mathf.Clamp(position.y, minY, maxY);

        targetCamera.transform.position = position;
    }

    private void LogSetupWarnings()
    {
        if (!logSetupWarnings)
            return;

        if (targetCamera == null)
        {
            Debug.LogWarning($"{name}: CameraMapDragController chưa có Target Camera.");
            return;
        }

        if (!targetCamera.orthographic)
            Debug.LogWarning($"{name}: CameraMapDragController đang thiết kế cho Orthographic Camera.");

        if (mapBoundsCollider == null && mapRoot == null)
            Debug.LogWarning($"{name}: Chưa gán Map Bounds Collider hoặc Map Root nên camera sẽ không có vùng kéo rõ ràng.");

        if (calculatedMapBounds.size == Vector3.zero)
            return;

        float cameraHeight = targetCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * targetCamera.aspect;

        if (calculatedMapBounds.size.x <= cameraWidth && calculatedMapBounds.size.y <= cameraHeight)
        {
            Debug.LogWarning(
                $"{name}: Map bounds nhỏ hơn hoặc bằng khung camera. Camera sẽ bị clamp ở giữa nên nhìn như không kéo được. " +
                "Hãy tăng Size của BoxCollider2D phủ toàn bộ map lớn hơn màn hình."
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        RefreshMapBounds();

        if (calculatedMapBounds.size == Vector3.zero)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(calculatedMapBounds.center, calculatedMapBounds.size);
    }
}
