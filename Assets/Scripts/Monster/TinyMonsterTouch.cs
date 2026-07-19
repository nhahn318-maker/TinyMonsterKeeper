using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class TinyMonsterTouch : MonoBehaviour, IPointerClickHandler {
    [SerializeField] private TinyMonsterController controller;
    [SerializeField] private TinyMonsterCoinProducer coinProducer;

    public TinyMonsterController Controller => controller;

    public string MonsterName => controller != null ? controller.MonsterName : "Unknown";
    public int Friendship => controller != null ? controller.Friendship : 0;
    public int MaxFriendship => controller != null ? controller.MaxFriendship : 100;
    public int BerryCostPerFeed => controller != null ? controller.BerryCostPerFeed : 1;
    public int FeedFriendshipGain => controller != null ? controller.FeedFriendshipGain : 10;

    private void Awake()
    {
        if (coinProducer == null)
            coinProducer = GetComponent<TinyMonsterCoinProducer>();
    }

    public void AddFriendship(int amount)
    {
        if (controller == null)
        {
            Debug.LogWarning("TinyMonsterController is missing on TinyMonsterTouch!");
            return;
        }

        controller.AddFriendship(amount);
    }

    public void PlayHappy()
    {
        if (controller != null)
        {
            controller.PlayHappy();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (BookOpenUI.IsOpen)
            return;

        if (coinProducer != null && coinProducer.HasCoinToCollect)
        {
            coinProducer.CollectCoin();
            return;
        }

        if (MonsterUIPanel.Instance == null)
        {
            Debug.LogWarning("MonsterUIPanel Instance is missing!");
            return;
        }

        MonsterUIPanel.Instance.Show(this);
    }
}
