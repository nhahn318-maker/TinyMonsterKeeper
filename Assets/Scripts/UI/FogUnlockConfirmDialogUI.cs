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
}
