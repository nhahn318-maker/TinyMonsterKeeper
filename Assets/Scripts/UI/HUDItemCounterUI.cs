using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDItemCounterUI : MonoBehaviour {
    [Header("Item")]
    [SerializeField] private ItemData itemData;
    [SerializeField] private TMP_Text countText;

    [Header("Panel Sprite")]
    [SerializeField] private Image panelImage;
    [SerializeField] private Sprite normalPanelSprite;
    [SerializeField] private Sprite warningPanelSprite;

    [Header("Text Color")]
    [SerializeField] private Color normalTextColor = new Color32(0x2E, 0x1F, 0x17, 0xFF);
    [SerializeField] private Color warningTextColor = new Color32(0xD8, 0x43, 0x5E, 0xFF);

    [Header("Flash")]
    [SerializeField] private int flashCount = 3;
    [SerializeField] private float flashInterval = 0.12f;

    private int lastAmount = -1;
    private Coroutine flashRoutine;

    private void Awake()
    {
        if (panelImage == null)
            panelImage = GetComponent<Image>();

        if (panelImage != null && normalPanelSprite == null)
            normalPanelSprite = panelImage.sprite;
    }

    private void Start()
    {
        SetNormal();
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (InventoryManager.Instance == null) return;
        if (itemData == null) return;
        if (countText == null) return;

        int amount = InventoryManager.Instance.GetItemAmount(itemData);

        if (amount == lastAmount) return;

        lastAmount = amount;
        countText.text = amount.ToString();
    }

    public void PlayWarningFlash()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(WarningFlashRoutine());
    }

    private IEnumerator WarningFlashRoutine()
    {
        for (int i = 0; i < flashCount; i++)
        {
            SetWarning();
            yield return new WaitForSeconds(flashInterval);

            SetNormal();
            yield return new WaitForSeconds(flashInterval);
        }

        SetNormal();
        flashRoutine = null;
    }

    private void SetWarning()
    {
        if (panelImage != null && warningPanelSprite != null)
            panelImage.sprite = warningPanelSprite;

        SetTextColor(warningTextColor);
    }

    private void SetNormal()
    {
        if (panelImage != null && normalPanelSprite != null)
            panelImage.sprite = normalPanelSprite;

        SetTextColor(normalTextColor);
    }

    private void SetTextColor(Color color)
    {
        if (countText == null) return;

        countText.enableVertexGradient = false;
        countText.color = color;
        countText.faceColor = color;
        countText.outlineColor = Color.clear;
        countText.alpha = 1f;

        countText.SetAllDirty();
        countText.ForceMeshUpdate();
    }
}