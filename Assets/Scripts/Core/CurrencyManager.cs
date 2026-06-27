using UnityEngine;
using System;

public class CurrencyManager : MonoBehaviour {
    public static CurrencyManager Instance { get; private set; }

    public event Action<int> OnCoinChanged;

    [SerializeField] private int coin;

    public int Coin => coin;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddCoin(int amount)
    {
        if (amount <= 0) return;

        coin += amount;
        OnCoinChanged?.Invoke(coin);

        Debug.Log($"Added {amount} coin. Total coin: {coin}");
    }

    public bool SpendCoin(int amount)
    {
        if (amount <= 0) return false;

        if (coin < amount)
        {
            Debug.Log("Not enough coin!");
            return false;
        }

        coin -= amount;
        OnCoinChanged?.Invoke(coin);

        return true;
    }
}