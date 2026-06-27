using UnityEngine;

public class TinyMonsterController : MonoBehaviour {
    public enum MonsterState {
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

    [Header("Friendship")]
    [SerializeField] private int friendship = 50;
    [SerializeField] private int maxFriendship = 100;

    [Header("State Timers")]
    [SerializeField] private float happyDuration = 3.0f;

    private MonsterState currentState;
    private float stateTimer;

    public MonsterState CurrentState => currentState;
    public MonsterData Data => monsterData;
    public string MonsterName => monsterData != null ? monsterData.monsterName : "Unknown";

    public int Friendship => friendship;
    public int MaxFriendship => maxFriendship;

    private void Start()
    {
        friendship = Mathf.Clamp(friendship, 0, maxFriendship);
        EnterIdle();
    }

    private void Update()
    {
        if (currentState != MonsterState.Happy)
            return;

        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            EnterIdle();

            if (animController != null)
            {
                animController.TriggerIdle();
            }
        }
    }

    public void AddFriendship(int amount)
    {
        friendship = Mathf.Clamp(friendship + amount, 0, maxFriendship);

        Debug.Log($"{MonsterName} friendship: {friendship}/{maxFriendship}");
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
    }

    public void ResumeAfterMenu()
    {
        if (currentState == MonsterState.Sleep)
            return;

        EnterIdle();
    }
}