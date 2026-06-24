using TMPro;
using UnityEngine;

public class ActionMenuUI : MonoBehaviour {
    public static ActionMenuUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform actionMenuPanel;
    [SerializeField] private TextMeshProUGUI infoLabel;

    [Header("Position")]
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, 90f);

    private RectTransform canvasRect;
    private TinyMonsterTouch selectedMonster;
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

        EnsureInfoLabel();
        Hide(false);
    }

    private void Update()
    {
        if (selectedMonster == null || actionMenuPanel == null || !actionMenuPanel.gameObject.activeSelf)
            return;

        if (WasPointerPressedOutsidePanel())
        {
            Hide(true);
        }
    }

    public void Show(TinyMonsterTouch monster)
    {
        if (monster == null) return;
        if (canvas == null || canvasRect == null || actionMenuPanel == null)
        {
            Debug.LogWarning("ActionMenuUI is missing Canvas or ActionMenuPanel.");
            return;
        }

        if (selectedMonster != null && selectedMonster != monster && selectedMonster.Controller != null)
        {
            selectedMonster.Controller.ResumeAfterMenu();
        }

        selectedMonster = monster;
        ShowInfo(monster);

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

        actionMenuPanel.anchoredPosition = localPoint;
        actionMenuPanel.gameObject.SetActive(true);
    }

    public void Hide(bool resumeMonster = true)
    {
        if (actionMenuPanel != null)
            actionMenuPanel.gameObject.SetActive(false);

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

    public void OnClickInfo()
    {
        if (selectedMonster == null) return;

        ShowInfo(selectedMonster);
    }

    private void ShowInfo(TinyMonsterTouch monster)
    {
        EnsureInfoLabel();

        if (infoLabel != null)
        {
            infoLabel.text = monster.MonsterName;
        }

        Debug.Log($"Info: {monster.MonsterName}");
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
        return RectTransformUtility.RectangleContainsScreenPoint(
            actionMenuPanel,
            screenPoint,
            CanvasCamera
        );
    }

    private void EnsureInfoLabel()
    {
        if (infoLabel != null || actionMenuPanel == null)
            return;

        GameObject labelObject = new GameObject("InfoLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(actionMenuPanel, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 1f);
        labelRect.anchorMax = new Vector2(0.5f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -12f);
        labelRect.sizeDelta = new Vector2(260f, 36f);

        infoLabel = labelObject.GetComponent<TextMeshProUGUI>();
        infoLabel.alignment = TextAlignmentOptions.Center;
        infoLabel.fontSize = 24f;
        infoLabel.color = Color.white;
        infoLabel.raycastTarget = false;
    }
}
