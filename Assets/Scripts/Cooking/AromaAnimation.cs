using System.Collections;
using UnityEngine;

public class AromaAnimation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 8f;
    [SerializeField] private float endScaleMultiplier = 1.15f;
    [SerializeField] private float floatUpDistance = 0.15f;

    private Coroutine playRoutine;
    private Vector3 initialLocalPosition;
    private Vector3 initialLocalScale;

    public float Duration => frames != null && frames.Length > 0
        ? frames.Length / Mathf.Max(0.1f, fps)
        : 0f;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        initialLocalPosition = transform.localPosition;
        initialLocalScale = transform.localScale;
        Hide();
    }

    public void Play()
    {
        if (frames == null || frames.Length == 0 || spriteRenderer == null)
            return;

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(PlayRoutine());
    }

    public void Hide()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (spriteRenderer == null)
            return;

        transform.localPosition = initialLocalPosition;
        transform.localScale = initialLocalScale;
        spriteRenderer.enabled = false;
        spriteRenderer.color = Color.white;
    }

    private IEnumerator PlayRoutine()
    {
        spriteRenderer.enabled = true;
        transform.localPosition = initialLocalPosition;
        transform.localScale = initialLocalScale;

        float frameDuration = 1f / Mathf.Max(0.1f, fps);
        float elapsed = 0f;
        float duration = Duration;

        for (int i = 0; i < frames.Length; i++)
        {
            spriteRenderer.sprite = frames[i];

            float frameElapsed = 0f;
            while (frameElapsed < frameDuration)
            {
                frameElapsed += Time.deltaTime;
                elapsed += Time.deltaTime;

                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                float eased = 1f - Mathf.Pow(1f - t, 2f);
                transform.localScale = Vector3.Lerp(initialLocalScale, initialLocalScale * endScaleMultiplier, eased);
                transform.localPosition = initialLocalPosition + Vector3.up * Mathf.Lerp(0f, floatUpDistance, eased);

                Color color = Color.white;
                color.a = t < 0.72f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.72f) / 0.28f);
                spriteRenderer.color = color;

                yield return null;
            }
        }

        ApplyHiddenState();
        playRoutine = null;
    }

    private void ApplyHiddenState()
    {
        if (spriteRenderer == null)
            return;

        transform.localPosition = initialLocalPosition;
        transform.localScale = initialLocalScale;
        spriteRenderer.enabled = false;
        spriteRenderer.color = Color.white;
    }
}
