using UnityEngine;
using System;

public class TinyMonsterCoinProducer : MonoBehaviour {
    [Header("References")]
    [SerializeField] private TinyMonsterController controller;
    [SerializeField] private GameObject coinBubbleObject;
    [SerializeField] private Transform coinPopupAnchor;

    [Header("Debug")]
    [SerializeField] private int storedCoin;

    private float timer;

    public event Action StoredCoinChanged;
    public int StoredCoin => storedCoin;
    public bool HasCoinToCollect => storedCoin > 0;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<TinyMonsterController>();

        UpdateCoinBubble();
    }

    private void Update()
    {
        if (controller == null || controller.Data == null) return;

        if (storedCoin >= controller.Data.maxStoredCoin)
        {
            storedCoin = controller.Data.maxStoredCoin;
            UpdateCoinBubble();
            return;
        }

        timer += Time.deltaTime;

        if (timer >= controller.Data.coinTickInterval)
        {
            timer -= controller.Data.coinTickInterval;
            AddStoredCoin(controller.Data.coinPerTick);
        }
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
        timer = 0f;
        UpdateCoinBubble();
        StoredCoinChanged?.Invoke();
    }

    public void CollectCoin()
    {
        if (storedCoin <= 0) return;

        int collectAmount = storedCoin;
        storedCoin = 0;
        timer = 0f;

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
}
