using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FogUnlockConfirmDialogUI : MonoBehaviour
{
    public static FogUnlockConfirmDialogUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private float messageAutoHideDuration = 2f;

    private Action confirmAction;
    private Coroutine messageRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple FogUnlockConfirmDialogUI instances found. Using the first one.");
        }
        else
        {
            Instance = this;
        }

        if (panelRoot == null)
            panelRoot = gameObject;

        if (messageText == null)
            messageText = GetComponentInChildren<TextMeshProUGUI>(true);

        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        ClearButtonListeners();
    }

    public void ShowConfirm(string message, Action onConfirm)
    {
        StopMessageRoutine();
        confirmAction = onConfirm;
        FitPanelToSafeArea();

        if (messageText != null)
            messageText.text = message;

        if (yesButton != null)
        {
            yesButton.gameObject.SetActive(true);
            yesButton.onClick.RemoveListener(Confirm);
            yesButton.onClick.AddListener(Confirm);
        }

        if (noButton != null)
        {
            noButton.gameObject.SetActive(true);
            noButton.onClick.RemoveListener(Hide);
            noButton.onClick.AddListener(Hide);
        }

        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void ShowMessage(string message)
    {
        StopMessageRoutine();
        confirmAction = null;
        FitPanelToSafeArea();

        if (messageText != null)
            messageText.text = message;

        if (yesButton != null)
            yesButton.gameObject.SetActive(false);

        if (noButton != null)
            noButton.gameObject.SetActive(false);

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (messageAutoHideDuration > 0f)
            messageRoutine = StartCoroutine(HideMessageAfterDelay(messageAutoHideDuration));
    }

    public void Hide()
    {
        StopMessageRoutine();
        confirmAction = null;
        ClearButtonListeners();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void Confirm()
    {
        Action action = confirmAction;
        Hide();
        action?.Invoke();
    }

    private void ClearButtonListeners()
    {
        if (yesButton != null)
            yesButton.onClick.RemoveListener(Confirm);

        if (noButton != null)
            noButton.onClick.RemoveListener(Hide);
    }

    private IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        messageRoutine = null;
        Hide();
    }

    private void StopMessageRoutine()
    {
        if (messageRoutine == null)
            return;

        StopCoroutine(messageRoutine);
        messageRoutine = null;
    }

    private void FitPanelToSafeArea()
    {
        if (panelRoot == null)
            return;

        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        Canvas canvas = panelRoot.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        if (panelRect == null || canvasRect == null || Screen.width <= 0 || Screen.height <= 0)
            return;

        Rect safeArea = Screen.safeArea;
        Vector2 canvasSize = canvasRect.rect.size;
        float maxWidth = safeArea.width * canvasSize.x / Screen.width * 0.9f;
        float maxHeight = safeArea.height * canvasSize.y / Screen.height * 0.55f;
        Vector2 size = panelRect.sizeDelta;
        size.x = Mathf.Min(size.x, maxWidth);
        size.y = Mathf.Min(size.y, maxHeight);
        panelRect.sizeDelta = size;

        float safeCenterOffsetX = (safeArea.center.x - Screen.width * 0.5f)
            * canvasSize.x / Screen.width;
        float safeBottom = safeArea.yMin * canvasSize.y / Screen.height;
        float bottomMargin = Mathf.Max(24f, canvasSize.y * 0.02f);

        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(safeCenterOffsetX, safeBottom + bottomMargin);
    }
}
