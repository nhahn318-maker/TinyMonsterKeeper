using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class TinyMonsterTouch : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TinyMonsterController controller;

    public TinyMonsterController Controller => controller;
    public string MonsterName => controller != null ? controller.MonsterName : "Unknown";
    public int Friendship => controller != null ? controller.Friendship : 0;

    private void Awake()
    {
        // Không tự tìm - yêu cầu kéo thả trong Inspector
    }

    public void AddFriendship(int amount)
    {
        if (controller != null)
        {
            controller.AddFriendship(amount);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        MonsterUIPanel.Instance.Show(this);
    }
}
