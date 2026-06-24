using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MonsterUIPanel : MonoBehaviour
{
    public static MonsterUIPanel Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform panelContainer;
    [SerializeField] private RectTransform infoPanel;
    [SerializeField] private RectTransform actionMenuPanel;

    [Header("Info Panel Elements")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image friendshipBarFill;
    [SerializeField] private TextMeshProUGUI friendshipText;

    [Header("Position")]
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, 100f);

    private TinyMonsterTouch selectedMonster;
    private RectTransform canvasRect;
    private Camera CanvasCamera => canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
        ? canvas.worldCamera
        : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

        Hide(false);
    }

    private void Update()
    {
        if (selectedMonster == null || !panelContainer.gameObject.activeSelf)
            return;

        if (WasPointerPressedOutsidePanel())
        {
            Hide(true);
        }
    }

    public void Show(TinyMonsterTouch monster)
    {
        if (monster == null || canvas == null || canvasRect == null || panelContainer == null)
            return;

        if (selectedMonster != null && selectedMonster != monster && selectedMonster.Controller != null)
        {
            selectedMonster.Controller.ResumeAfterMenu();
        }

        selectedMonster = monster;
        UpdateInfo(monster);

        if (selectedMonster.Controller != null)
        {
            selectedMonster.Controller.PauseForMenu();
        }

        Vector2 screenPosition = Camera.main.WorldToScreenPoint(monster.transform.position);
        screenPosition += screenOffset;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            CanvasCamera,
            out Vector2 localPoint
        );

        panelContainer.anchoredPosition = localPoint;
        panelContainer.gameObject.SetActive(true);
    }

    public void Hide(bool resumeMonster = true)
    {
        if (panelContainer != null)
            panelContainer.gameObject.SetActive(false);

        if (resumeMonster && selectedMonster != null && selectedMonster.Controller != null)
        {
            selectedMonster.Controller.ResumeAfterMenu();
        }

        selectedMonster = null;
    }

    public void OnClickFeed()
    {
        if (selectedMonster == null) return;

        TinyMonsterTouch monster = selectedMonster;
        Hide(false);

        if (monster.Controller != null)
        {
            monster.Controller.ResumeAfterMenu();
            monster.Controller.PlayHappy();
        }

        Debug.Log($"Feed {monster.MonsterName}");
    }

    public void OnClickPlay()
    {
        if (selectedMonster == null) return;

        TinyMonsterTouch monster = selectedMonster;
        Hide(false);

        if (monster.Controller != null)
        {
            monster.Controller.ResumeAfterMenu();
            monster.Controller.PlayHappy();
        }

        Debug.Log($"Play with {monster.MonsterName}");
    }

    private void UpdateInfo(TinyMonsterTouch monster)
    {
        if (nameText != null)
        {
            nameText.text = monster.MonsterName;
        }

        // TODO: Lấy friendship từ monster data
        if (friendshipBarFill != null)
        {
            friendshipBarFill.fillAmount = 0.7f; // Temporary value (0.0 - 1.0)
        }

        if (friendshipText != null)
        {
            friendshipText.text = "Friendship";
        }
    }

    private bool WasPointerPressedOutsidePanel()
    {
        if (Input.GetMouseButtonDown(0))
            return !IsScreenPointInsidePanel(Input.mousePosition);

        if (Input.touchCount <= 0)
            return false;

        Touch touch = Input.GetTouch(0);
        return touch.phase == TouchPhase.Began && !IsScreenPointInsidePanel(touch.position);
    }

    private bool IsScreenPointInsidePanel(Vector2 screenPoint)
    {
        // Check nếu click vào panel container
        if (RectTransformUtility.RectangleContainsScreenPoint(panelContainer, screenPoint, CanvasCamera))
            return true;

        // Check nếu click vào info panel
        if (infoPanel != null && RectTransformUtility.RectangleContainsScreenPoint(infoPanel, screenPoint, CanvasCamera))
            return true;

        // Check nếu click vào action menu panel
        if (actionMenuPanel != null && RectTransformUtility.RectangleContainsScreenPoint(actionMenuPanel, screenPoint, CanvasCamera))
            return true;

        return false;
    }
}
