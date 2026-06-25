using UnityEngine;

public class TinyMonsterController : MonoBehaviour
{
    public enum MonsterState
    {
        Idle,
        Walk,
        Happy,
        Sleep
    }

    [Header("Components")]
    [SerializeField] private TinyMonsterNavRoam navRoam;
    [SerializeField] private TinyMonsterAnimationController animController;
    [SerializeField] private TinyMonsterEffects effects;

    [Header("Data")]
    [SerializeField] private MonsterData monsterData;

    [Header("State Timers")]
    [SerializeField] private float happyDuration = 3.0f;

    private MonsterState currentState;
    private float stateTimer;
    private int friendship = 50;

    public MonsterState CurrentState => currentState;
    public MonsterData Data => monsterData;
    public string MonsterName => monsterData != null ? monsterData.monsterName : "Unknown";
    public int Friendship => friendship;

    public void AddFriendship(int amount)
    {
        friendship = Mathf.Clamp(friendship + amount, 0, 100);
    }

    private void Awake()
    {
        // Không tự tìm - yêu cầu kéo thả trong Inspector
    }

    private void Start()
    {
        EnterIdle();
    }

    private void Update()
    {
        stateTimer -= Time.deltaTime;

        switch (currentState)
        {
            case MonsterState.Happy:
                if (stateTimer <= 0f)
                {
                    Debug.Log("Happy duration finished, triggering Idle");
                    EnterIdle();
                    
                    if (animController != null)
                    {
                        animController.TriggerIdle();
                    }
                }
                break;

            case MonsterState.Sleep:
                break;
        }
    }

    public void EnterIdle()
    {
        currentState = MonsterState.Idle;
        
        if (navRoam != null)
        {
            navRoam.StartRoaming();
        }
    }

    public void PlayHappy()
    {
        Debug.Log($"PlayHappy called, duration: {happyDuration}s");
        currentState = MonsterState.Happy;
        stateTimer = happyDuration;

        if (navRoam != null)
        {
            navRoam.StopMovement();
        }

        if (animController != null)
        {
            animController.TriggerHappy();
        }

        if (effects != null)
        {
            effects.PlayHeartEffect();
        }
    }

    public void Sleep()
    {
        currentState = MonsterState.Sleep;

        if (navRoam != null)
        {
            navRoam.StopMovement();
        }

        if (animController != null)
        {
            animController.TriggerSleep();
        }
    }

    public void WakeUp()
    {
        EnterIdle();

        if (animController != null)
        {
            animController.TriggerIdle();
        }
    }

    public void PauseForMenu()
    {
        if (navRoam != null)
        {
            navRoam.PauseForMenu();
        }

        currentState = MonsterState.Idle;
    }

    public void ResumeAfterMenu()
    {
        EnterIdle();
    }
}
