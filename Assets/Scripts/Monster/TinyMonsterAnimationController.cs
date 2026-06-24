using UnityEngine;

public class TinyMonsterAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private TinyMonsterController controller;
    private TinyMonsterNavRoam navRoam;

    private void Awake()
    {
        // Không tự tìm - yêu cầu kéo thả trong Inspector
    }

    private void Update()
    {
        if (animator == null || navRoam == null) return;

        UpdateAnimationState();
    }

    private void UpdateAnimationState()
    {
        // Sync IsWalking với NavRoam
        animator.SetBool("IsWalking", navRoam.IsWalking);
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
}
