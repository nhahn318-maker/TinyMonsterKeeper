using UnityEngine;
using UnityEngine.AI;

public class TinyMonsterNavRoam : MonoBehaviour {
    public enum MonsterState {
        Idle,
        Walk,
        Happy,
        Sleep
    }

    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Roaming Area")]
    [SerializeField] private Collider2D gardenBounds;
    [SerializeField] private float sampleDistance = 1.5f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 0.8f;
    [SerializeField] private float stoppingDistance = 0.05f;

    [Header("Timers")]
    [SerializeField] private Vector2 idleTimeRange = new Vector2(2f, 4f);
    [SerializeField] private Vector2 walkTimeRange = new Vector2(2f, 5f);


    [SerializeField] private bool isPausedByMenu = false;

    private MonsterState currentState;
    private float stateTimer;
    private Vector3 lastPosition;

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        SetupAgent2D();
    }

    private void Start()
    {
        lastPosition = transform.position;
        EnterIdle();
    }

    private void Update()
    {
        UpdateFlipDirection();
        UpdateAnimator();

        stateTimer -= Time.deltaTime;

        if (isPausedByMenu)
        {
            UpdateAnimator();
            lastPosition = transform.position;
            return;
        }

        switch (currentState)
        {
            case MonsterState.Idle:
                if (stateTimer <= 0f)
                {
                    TryMoveToRandomPoint();
                }
                break;

            case MonsterState.Walk:
                bool reachedDestination =
                    !agent.pathPending &&
                    agent.remainingDistance <= agent.stoppingDistance;

                bool walkTimeout = stateTimer <= 0f;

                if (reachedDestination || walkTimeout)
                {
                    EnterIdle();
                }
                break;

            case MonsterState.Happy:
                if (stateTimer <= 0f)
                {
                    EnterIdle();
                }
                break;

            case MonsterState.Sleep:
                break;
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

    public void PauseForMenu()
    {
        isPausedByMenu = true;

        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        currentState = MonsterState.Idle;
    }

    public void ResumeAfterMenu()
    {
        isPausedByMenu = false;
        EnterIdle();
    }

    private void TryMoveToRandomPoint()
    {
        if (gardenBounds == null)
        {
            Debug.LogWarning($"{name}: Chưa gán gardenBounds.");
            EnterIdle();
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

                currentState = MonsterState.Walk;
                stateTimer = Random.Range(walkTimeRange.x, walkTimeRange.y);
                return;
            }
        }

        EnterIdle();
    }

    private void EnterIdle()
    {
        currentState = MonsterState.Idle;
        stateTimer = Random.Range(idleTimeRange.x, idleTimeRange.y);

        if (agent.enabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    public void PlayHappy(float duration = 1.2f)
    {
        currentState = MonsterState.Happy;
        stateTimer = duration;

        if (agent.enabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    public void Sleep()
    {
        currentState = MonsterState.Sleep;

        if (agent.enabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    public void WakeUp()
    {
        EnterIdle();
    }

    private void UpdateFlipDirection()
    {
        Vector3 delta = transform.position - lastPosition;

        if (Mathf.Abs(delta.x) > 0.001f)
        {
            spriteRenderer.flipX = delta.x < 0f;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetBool("IsWalking", currentState == MonsterState.Walk);
        animator.SetBool("IsSleeping", currentState == MonsterState.Sleep);
        animator.SetBool("IsHappy", currentState == MonsterState.Happy);
    }
}