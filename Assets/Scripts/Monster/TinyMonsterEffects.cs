using UnityEngine;

public class TinyMonsterEffects : MonoBehaviour
{
    [SerializeField] private HeartEffect[] heartEffects;

    public void PlayHeartEffect()
    {
        if (heartEffects == null || heartEffects.Length == 0) return;

        foreach (var heart in heartEffects)
        {
            if (heart != null)
            {
                heart.Play();
            }
        }
    }
}
