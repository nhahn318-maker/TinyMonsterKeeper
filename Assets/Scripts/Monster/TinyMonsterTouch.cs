using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class TinyMonsterTouch : MonoBehaviour, IPointerClickHandler {
    [SerializeField] private TinyMonsterNavRoam roam;
    [SerializeField] private string monsterName = "Leafy";

    public TinyMonsterNavRoam Roam => roam;
    public string MonsterName => monsterName;

    private void Awake()
    {
        if (roam == null)
            roam = GetComponent<TinyMonsterNavRoam>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ActionMenuUI.Instance.Show(this);
    }
}