using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BootSplashController : MonoBehaviour
{
    [SerializeField] private CanvasGroup logoGroup;
    [SerializeField] private float fadeInDuration = 0.35f;
    [SerializeField] private float holdDuration = 1.35f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private string nextSceneName = "MainMenuScene";

    private IEnumerator Start()
    {
        logoGroup.alpha = 0f;
        yield return Fade(0f, 1f, fadeInDuration);
        yield return new WaitForSecondsRealtime(holdDuration);
        yield return Fade(1f, 0f, fadeOutDuration);
        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            logoGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            logoGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        logoGroup.alpha = to;
    }
}
