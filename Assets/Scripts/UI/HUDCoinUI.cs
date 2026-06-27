using TMPro;
using UnityEngine;

public class HUDCoinUI : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI coinText;

    private int lastCoin = -1;

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (CurrencyManager.Instance == null)
        {
            return;
        }

        if (coinText == null)
        {
            return;
        }

        int coin = CurrencyManager.Instance.Coin;

        if (coin == lastCoin)
        {
            return;
        }

        lastCoin = coin;
        coinText.text = coin.ToString();

        Debug.Log("HUD coin updated: " + coin);
    }
}