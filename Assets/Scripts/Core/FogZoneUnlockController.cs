using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FogZoneUnlockController : MonoBehaviour
{
    private enum RevealDirection
    {
        Right,
        Left,
        Up,
        Down,
        Custom
    }

    [Header("Unlock")]
    [SerializeField] private int unlockCost = 10;
    [SerializeField] private GameTextDatabase textDatabase;
    [SerializeField] private GameTextKey confirmMessageKey = GameTextKey.FogUnlockConfirm;
    [SerializeField] private GameTextKey notEnoughCoinMessageKey = GameTextKey.NotEnoughCoin;
    [SerializeField] private string confirmMessageFallback = "Ban muon mo khu vuc nay voi gia {0} coin?";
    [SerializeField] private string notEnoughCoinFallback = "Khong du coin.";

    [Header("References")]
    [SerializeField] private Button unlockButton;
    [SerializeField] private FogUnlockConfirmDialogUI confirmDialog;
    [SerializeField] private FogTilemapRevealController fogReveal;
    [SerializeField] private Collider2D revealBounds;
    [SerializeField] private Sprite unlockedButtonSprite;
    [SerializeField] private float unlockVisualDuration = 0.25f;

    [Header("Reveal Motion")]
    [SerializeField] private RevealDirection revealDirection = RevealDirection.Right;
    [SerializeField] private float revealDistance = 0.75f;
    [SerializeField] private Vector3 customRevealDriftOffset = new Vector3(0.75f, 0f, 0f);
    [SerializeField] private bool revealWholeTilemap = true;

    private bool isUnlocking;
    private bool isUnlocked;

    private void Awake()
    {
        if (unlockButton == null)
            unlockButton = GetComponent<Button>();

        if (fogReveal == null)
            fogReveal = GetComponent<FogTilemapRevealController>();

        if (revealBounds == null)
            revealBounds = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        if (unlockButton != null)
            unlockButton.onClick.AddListener(OnClickUnlock);
    }

    private void OnDisable()
    {
        if (unlockButton != null)
            unlockButton.onClick.RemoveListener(OnClickUnlock);
    }

    private void OnMouseDown()
    {
        if (unlockButton == null)
            OnClickUnlock();
    }

    public void OnClickUnlock()
    {
        if (isUnlocking || isUnlocked)
            return;

        FogUnlockConfirmDialogUI dialog = confirmDialog != null ? confirmDialog : FogUnlockConfirmDialogUI.Instance;
        if (dialog == null)
        {
            Debug.LogWarning("Fog unlock confirm dialog is missing. Unlocking directly for testing.");
            TryUnlock();
            return;
        }

        dialog.ShowConfirm(GetText(confirmMessageKey, confirmMessageFallback, unlockCost), TryUnlock);
    }

    private void TryUnlock()
    {
        if (isUnlocking || isUnlocked)
            return;

        if (CurrencyManager.Instance == null)
        {
            Debug.LogWarning("CurrencyManager is missing. Cannot unlock fog zone.");
            return;
        }

        if (unlockCost > 0 && !CurrencyManager.Instance.SpendCoin(unlockCost))
        {
            FogUnlockConfirmDialogUI dialog = confirmDialog != null ? confirmDialog : FogUnlockConfirmDialogUI.Instance;
            if (dialog != null)
                dialog.ShowMessage(GetText(notEnoughCoinMessageKey, notEnoughCoinFallback));
            return;
        }

        StartCoroutine(UnlockFogRoutine());
    }

    private IEnumerator UnlockFogRoutine()
    {
        isUnlocking = true;
        isUnlocked = true;

        if (unlockButton != null && unlockedButtonSprite != null)
        {
            Image image = unlockButton.targetGraphic as Image;
            if (image == null)
                image = unlockButton.GetComponent<Image>();

            if (image != null)
                image.sprite = unlockedButtonSprite;
        }

        if (unlockButton != null)
            unlockButton.interactable = false;

        float waitDuration = Mathf.Max(0f, unlockVisualDuration);
        if (waitDuration > 0f)
            yield return new WaitForSeconds(waitDuration);

        if (unlockButton != null)
            unlockButton.gameObject.SetActive(false);

        if (fogReveal == null)
        {
            Debug.LogWarning("Fog reveal controller is missing.");
            yield break;
        }

        if (revealWholeTilemap || revealBounds == null)
            fogReveal.RevealAll(GetRevealDriftOffset());
        else
            fogReveal.RevealColliderBounds(revealBounds, GetRevealDriftOffset());
    }

    private Vector3 GetRevealDriftOffset()
    {
        float distance = Mathf.Max(0f, revealDistance);

        switch (revealDirection)
        {
            case RevealDirection.Left:
                return Vector3.left * distance;
            case RevealDirection.Up:
                return Vector3.up * distance;
            case RevealDirection.Down:
                return Vector3.down * distance;
            case RevealDirection.Custom:
                return customRevealDriftOffset;
            default:
                return Vector3.right * distance;
        }
    }

    private string GetText(GameTextKey key, string fallback, params object[] args)
    {
        if (textDatabase != null)
            return textDatabase.Get(key, fallback, args);

        return args == null || args.Length == 0 ? fallback : string.Format(fallback, args);
    }
}
