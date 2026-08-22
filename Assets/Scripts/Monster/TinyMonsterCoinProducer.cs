using UnityEngine;
using System;

public class TinyMonsterCoinProducer : MonoBehaviour {
    [Header("References")]
    [SerializeField] private TinyMonsterController controller;
    [SerializeField] private GameObject coinBubbleObject;
    [SerializeField] private Transform coinPopupAnchor;

    [Header("Debug")]
    [SerializeField] private int storedCoin;

    private long nextCoinAtUnix;

    public event Action StoredCoinChanged;
    public int StoredCoin => storedCoin;
    public bool HasCoinToCollect => storedCoin > 0;
    public long NextCoinAtUnix => nextCoinAtUnix;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<TinyMonsterController>();

        UpdateCoinBubble();
    }

    private void Update()
    {
        ReconcileCoinProduction();
    }

    private void AddStoredCoin(int amount)
    {
        if (amount <= 0) return;

        int maxCoin = controller.Data.maxStoredCoin;

        storedCoin = Mathf.Clamp(storedCoin + amount, 0, maxCoin);

        UpdateCoinBubble();
        StoredCoinChanged?.Invoke();

        Debug.Log($"{controller.MonsterName} stored coin: {storedCoin}/{maxCoin}");
    }

    public void SetStoredCoin(int amount)
    {
        int maxCoin = controller != null && controller.Data != null ? controller.Data.maxStoredCoin : int.MaxValue;
        storedCoin = Mathf.Clamp(amount, 0, maxCoin);
        EnsureNextCoinTime();
        UpdateCoinBubble();
        StoredCoinChanged?.Invoke();
    }

    public void SetPersistentState(int amount, long savedNextCoinAtUnix)
    {
        int maxCoin = controller != null && controller.Data != null ? controller.Data.maxStoredCoin : int.MaxValue;
        storedCoin = Mathf.Clamp(amount, 0, maxCoin);
        nextCoinAtUnix = System.Math.Max(0L, savedNextCoinAtUnix);
        ReconcileCoinProduction();
        UpdateCoinBubble();
        StoredCoinChanged?.Invoke();
    }

    public void CollectCoin()
    {
        if (storedCoin <= 0) return;

        int collectAmount = storedCoin;
        storedCoin = 0;
        nextCoinAtUnix = TimedSaveUtility.SecondsFromNow(controller != null && controller.Data != null ? controller.Data.coinTickInterval : 1f);

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCoin(collectAmount);
        }
        else
        {
            Debug.LogWarning("CurrencyManager is missing!");
        }

        if (RewardPopupManager.Instance != null)
        {
            Vector3 popupPosition = coinPopupAnchor != null
                ? coinPopupAnchor.position
                : transform.position;

            RewardPopupManager.Instance.ShowCoinPopup(popupPosition, collectAmount);
        }

        UpdateCoinBubble();
        StoredCoinChanged?.Invoke();

        Debug.Log($"Collected {collectAmount} coin from {controller.MonsterName}");
    }

    private void UpdateCoinBubble()
    {
        if (coinBubbleObject != null)
            coinBubbleObject.SetActive(storedCoin > 0);
    }

    private void ReconcileCoinProduction()
    {
        if (controller == null || controller.Data == null)
            return;

        int maxCoin = controller.Data.maxStoredCoin;
        if (storedCoin >= maxCoin)
        {
            storedCoin = maxCoin;
            EnsureNextCoinTime();
            UpdateCoinBubble();
            return;
        }

        EnsureNextCoinTime();
        long now = TimedSaveUtility.NowUnix;
        if (now < nextCoinAtUnix)
            return;

        long interval = Mathf.Max(1, Mathf.CeilToInt(controller.Data.coinTickInterval));
        long ticks = 1 + ((now - nextCoinAtUnix) / interval);
        long potentialGain = ticks * controller.Data.coinPerTick;
        int gain = potentialGain >= int.MaxValue ? int.MaxValue : (int)potentialGain;
        nextCoinAtUnix += ticks * interval;
        AddStoredCoin(gain);
    }

    private void EnsureNextCoinTime()
    {
        if (nextCoinAtUnix <= 0L)
            nextCoinAtUnix = TimedSaveUtility.SecondsFromNow(controller != null && controller.Data != null ? controller.Data.coinTickInterval : 1f);
    }
}
