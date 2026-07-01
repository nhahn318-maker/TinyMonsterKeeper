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

    public bool IsWalking => isWalking;

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
        UpdateFlipDirection();

        if (isPaused || !isRoaming)
        {
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

    public void StopMovement()
    {
        isRoaming = false;
        isWalking = false;

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

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, sampleDistance, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);

                isWalking = true;
                stateTimer = Random.Range(walkTimeRange.x, walkTimeRange.y);
                return;
            }
        }

        EnterIdleState();
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

        Vector3 delta = transform.position - lastPosition;

        if (Mathf.Abs(delta.x) > 0.001f)
        {
            bool movingLeft = delta.x < 0f;
            bool shouldFlip = movingLeft == flipWhenMovingLeft;

            if (invertMovementFlip)
                shouldFlip = !shouldFlip;

            spriteRenderer.flipX = shouldFlip;
        }
    }
}
