using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class TinyMonsterTouch : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TinyMonsterController controller;
    [SerializeField] private string monsterName = "Leafy";

    public TinyMonsterController Controller => controller;
    public string MonsterName => monsterName;

    private void Awake()
    {
        // Không tự tìm - yêu cầu kéo thả trong Inspector
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        MonsterUIPanel.Instance.Show(this);
    }
}