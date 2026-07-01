using UnityEngine;

public class TinyMonsterAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private TinyMonsterNavRoam navRoam;
    private bool supportsNorthWalk;

    private static readonly int IsNorthWalkingHash = Animator.StringToHash("IsNorthWalking");

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        navRoam = GetComponent<TinyMonsterNavRoam>();
        supportsNorthWalk = HasAnimatorParameter(IsNorthWalkingHash);
    }

    private void Update()
    {
        if (animator == null || navRoam == null)
            return;

        if (supportsNorthWalk)
            animator.SetBool(IsNorthWalkingHash, navRoam.IsMovingNorth);
    }

    public void TriggerHappy()
    {
        if (animator != null)
        {
            animator.SetTrigger("IsHappy");
        }
    }

    public void TriggerSleep()
    {
        if (animator != null)
        {
            animator.SetTrigger("OnSleep");
        }
    }

    public void TriggerIdle()
    {
        if (animator != null)
        {
            Debug.Log("TriggerIdle called!");
            animator.SetTrigger("OnIdle");
        }
        else
        {
            Debug.LogWarning("Animator is null in TriggerIdle!");
        }
    }

    private bool HasAnimatorParameter(int parameterHash)
    {
        if (animator == null)
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].nameHash == parameterHash)
                return true;
        }

        return false;
    }
}
