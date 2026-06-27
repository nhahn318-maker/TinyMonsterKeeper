using System.Collections;
using TMPro;
using UnityEngine;

public class CoinRewardPopupUI : MonoBehaviour {
    [Header("References")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI rewardText;

    [Header("Animation")]
    [SerializeField] private float duration = 0.7f;
    [SerializeField] private float moveUpDistance = 40f;

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Show(int amount)
    {
        if (rewardText != null)
        {
            rewardText.text = $"+{amount}";
        }

        StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 endPosition = startPosition + Vector2.up * moveUpDistance;

        float timer = 0f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}