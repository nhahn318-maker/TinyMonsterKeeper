using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuTitleIntro : MonoBehaviour
{
    [SerializeField] private RectTransform titleRect;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Animator titleAnimator;
    [SerializeField] private float introDuration = 2.2f;
    [SerializeField] private float startOffsetY = 140f;
    [SerializeField] private RectTransform[] buttonRects;
    [SerializeField] private CanvasGroup[] buttonCanvasGroups;
    [SerializeField] private float buttonStartDelay = 2.2f;
    [SerializeField] private float buttonStagger = 0.2f;
    [SerializeField] private float buttonIntroDuration = 1.35f;
    [SerializeField] private float buttonStartOffsetY = 72f;

    private void Awake()
    {
        if (titleRect == null)
            titleRect = transform as RectTransform;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (titleAnimator == null)
            titleAnimator = GetComponent<Animator>();

        if (titleAnimator != null)
            titleAnimator.enabled = false;
    }

    private IEnumerator Start()
    {
        if (titleRect == null || canvasGroup == null)
            yield break;

        Vector2 destination = titleRect.anchoredPosition;
        Vector2 start = destination + Vector2.up * Mathf.Max(0f, startOffsetY);
        float duration = Mathf.Max(0.01f, introDuration);
        Vector2[] buttonDestinations = CacheButtonDestinations();

        titleRect.anchoredPosition = start;
        canvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            titleRect.anchoredPosition = Vector2.LerpUnclamped(start, destination, eased);
            canvasGroup.alpha = eased;
            UpdateButtonIntro(elapsed, buttonDestinations);
            yield return null;
        }

        titleRect.anchoredPosition = destination;
        canvasGroup.alpha = 1f;

        if (titleAnimator != null)
            titleAnimator.enabled = true;

        float buttonEndTime = buttonStartDelay + Mathf.Max(0, (buttonRects?.Length ?? 0) - 1) * buttonStagger + buttonIntroDuration;
        while (elapsed < buttonEndTime)
        {
            elapsed += Time.unscaledDeltaTime;
            UpdateButtonIntro(elapsed, buttonDestinations);
            yield return null;
        }

        UpdateButtonIntro(float.MaxValue, buttonDestinations);
    }

    private Vector2[] CacheButtonDestinations()
    {
        if (buttonRects == null)
            return new Vector2[0];

        Vector2[] destinations = new Vector2[buttonRects.Length];
        for (int i = 0; i < buttonRects.Length; i++)
        {
            if (buttonRects[i] == null)
                continue;

            destinations[i] = buttonRects[i].anchoredPosition;
            buttonRects[i].anchoredPosition = destinations[i] + Vector2.up * Mathf.Max(0f, buttonStartOffsetY);

            if (buttonCanvasGroups != null && i < buttonCanvasGroups.Length && buttonCanvasGroups[i] != null)
                buttonCanvasGroups[i].alpha = 0f;
        }

        return destinations;
    }

    private void UpdateButtonIntro(float elapsed, Vector2[] destinations)
    {
        if (buttonRects == null)
            return;

        for (int i = 0; i < buttonRects.Length; i++)
        {
            RectTransform buttonRect = buttonRects[i];
            if (buttonRect == null || i >= destinations.Length)
                continue;

            float delay = buttonStartDelay + i * buttonStagger;
            float t = Mathf.Clamp01((elapsed - delay) / Mathf.Max(0.01f, buttonIntroDuration));
            float eased = Mathf.SmoothStep(0f, 1f, t);
            Vector2 start = destinations[i] + Vector2.up * Mathf.Max(0f, buttonStartOffsetY);
            buttonRect.anchoredPosition = Vector2.LerpUnclamped(start, destinations[i], eased);

            if (buttonCanvasGroups != null && i < buttonCanvasGroups.Length && buttonCanvasGroups[i] != null)
                buttonCanvasGroups[i].alpha = eased;
        }
    }
}
