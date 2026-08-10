using UnityEngine;
using UnityEngine.AI;

public class TinyMonsterNavRoam : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Facing")]
    [SerializeField] private bool flipWhenMovingLeft = true;
    [SerializeField] private bool invertMovementFlip;

    [Header("Roaming Area")]
    [SerializeField] private Collider2D gardenBounds;
    [SerializeField] private float sampleDistance = 1.5f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 0.8f;
    [SerializeField] private float stoppingDistance = 0.05f;

    [Header("Timers")]
    [SerializeField] private Vector2 idleTimeRange = new Vector2(2f, 4f);
    [SerializeField] private Vector2 walkTimeRange = new Vector2(2f, 5f);

    private bool isRoaming = false;
    private bool isPaused = false;
    private bool isWalking = false;
    private float stateTimer;
    private Vector3 lastPosition;
    private Vector3 movementDelta;

    public bool IsWalking => isWalking;
    public bool IsMovingNorth => isWalking && movementDelta.y > 0.001f;
    public bool IsPaused => isPaused;
    public int AgentAreaMask => agent != null ? agent.areaMask : NavMesh.AllAreas;

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        SetupAgent2D();
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        movementDelta = transform.position - lastPosition;
        UpdateFlipDirection();

        if (isPaused || !isRoaming)
        {
            movementDelta = Vector3.zero;
            lastPosition = transform.position;
            return;
        }

        stateTimer -= Time.deltaTime;

        if (isWalking)
        {
            bool reachedDestination =
                !agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance;

            bool walkTimeout = stateTimer <= 0f;

            if (reachedDestination || walkTimeout)
            {
                EnterIdleState();
            }
        }
        else
        {
            if (stateTimer <= 0f)
            {
                TryMoveToRandomPoint();
            }
        }

        lastPosition = transform.position;
    }

    private void SetupAgent2D()
    {
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        agent.speed = moveSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = true;
    }

    public void StartRoaming()
    {
        isRoaming = true;
        isPaused = false;
        EnterIdleState();
    }

    public void SetGardenBounds(Collider2D bounds)
    {
        gardenBounds = bounds;
    }

    public void WarpTo(Vector3 position)
    {
        Vector3 targetPosition = position;
        if (TryGetNearestNavMeshPosition(position, Mathf.Max(sampleDistance, 2f), out Vector3 navMeshPosition))
            targetPosition = navMeshPosition;

        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
                agent.Warp(targetPosition);
            else
                transform.position = targetPosition;
        }
        else
        {
            transform.position = targetPosition;
        }

        lastPosition = transform.position;
        movementDelta = Vector3.zero;
    }

    public bool TryGetNearestNavMeshPosition(Vector3 position, float maxDistance, out Vector3 navMeshPosition)
    {
        int areaMask = AgentAreaMask;
        if (NavMesh.SamplePosition(position, out NavMeshHit hit, maxDistance, areaMask))
        {
            navMeshPosition = hit.position;
            navMeshPosition.z = position.z;
            return true;
        }

        navMeshPosition = position;
        return false;
    }

    public void StopMovement()
    {
        isRoaming = false;
        isWalking = false;
        movementDelta = Vector3.zero;

        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    public void PauseForMenu()
    {
        isPaused = true;

        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    public void ResumeAfterMenu()
    {
        isPaused = false;
    }

    private void TryMoveToRandomPoint()
    {
        if (gardenBounds == null)
        {
            Debug.LogWarning($"{name}: Chưa gán gardenBounds.");
            EnterIdleState();
            return;
        }

        for (int i = 0; i < 20; i++)
        {
            Bounds bounds = gardenBounds.bounds;

            Vector3 randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                transform.position.z
            );

            if (!gardenBounds.OverlapPoint(randomPoint))
                continue;

            if (FogAreaBlocker.BlocksPoint(randomPoint) ||
                FogAreaBlocker.BlocksPath(transform.position, randomPoint))
            {
                continue;
            }

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, sampleDistance, AgentAreaMask))
            {
                if (agent == null || !agent.enabled)
                {
                    EnterIdleState();
                    return;
                }

                if (!agent.isOnNavMesh)
                    WarpTo(transform.position);

                if (!agent.isOnNavMesh)
                {
                    EnterIdleState();
                    return;
                }

                if (!TryGetUnblockedPath(hit.position, out NavMeshPath path))
                    continue;

                agent.isStopped = false;
                agent.SetPath(path);

                isWalking = true;
                stateTimer = Random.Range(walkTimeRange.x, walkTimeRange.y);
                return;
            }
        }

        EnterIdleState();
    }

    private bool TryGetUnblockedPath(Vector3 destination, out NavMeshPath path)
    {
        path = new NavMeshPath();

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return false;

        if (!agent.CalculatePath(destination, path) || path.status != NavMeshPathStatus.PathComplete)
            return false;

        Vector3[] corners = path.corners;
        if (corners == null || corners.Length == 0)
            return false;

        for (int i = 0; i < corners.Length; i++)
        {
            if (FogAreaBlocker.BlocksPoint(corners[i]))
                return false;

            if (i > 0 && FogAreaBlocker.BlocksPath(corners[i - 1], corners[i]))
                return false;
        }

        return true;
    }

    private void EnterIdleState()
    {
        isWalking = false;
        stateTimer = Random.Range(idleTimeRange.x, idleTimeRange.y);

        if (agent.enabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    private void UpdateFlipDirection()
    {
        if (spriteRenderer == null)
            return;

        if (Mathf.Abs(movementDelta.x) > 0.001f)
        {
            bool movingLeft = movementDelta.x < 0f;
            bool shouldFlip = movingLeft == flipWhenMovingLeft;

            if (invertMovementFlip)
                shouldFlip = !shouldFlip;

            spriteRenderer.flipX = shouldFlip;
        }
    }
}
