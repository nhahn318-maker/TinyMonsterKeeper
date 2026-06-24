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

    [Header("State Timers")]
    [SerializeField] private float happyDuration = 0.15f;

    private MonsterState currentState;
    private float stateTimer;

    public MonsterState CurrentState => currentState;

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
