using UnityEngine;

public class HeartEffect : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float duration = 1f;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    public void Play()
    {
        gameObject.SetActive(true);
        
        if (animator != null)
        {
            animator.Play("Heart", 0, 0f);
        }
        
        Invoke(nameof(Hide), duration);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
