using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuSettingsUI : MonoBehaviour
{
    [SerializeField] private GameObject panelSetting;
    [SerializeField] private GameObject dimBlocker;
    [SerializeField] private Button openButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Image musicImage;
    [SerializeField] private Image sfxImage;
    [SerializeField] private Image alertsImage;
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;

    private bool musicEnabled = true;
    private bool sfxEnabled = true;
    private bool alertsEnabled = true;

    private void Awake()
    {
        if (openButton != null)
            openButton.onClick.AddListener(Open);
        if (backButton != null)
            backButton.onClick.AddListener(Close);

        BindToggle(musicImage, ToggleMusic);
        BindToggle(sfxImage, ToggleSfx);
        BindToggle(alertsImage, ToggleAlerts);

        ApplyToggleSprites();
        Close();
    }

    private void OnDestroy()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(Open);
        if (backButton != null)
            backButton.onClick.RemoveListener(Close);
    }

    public void Open()
    {
        if (dimBlocker != null)
            dimBlocker.SetActive(true);

        if (panelSetting != null)
            panelSetting.SetActive(true);
    }

    public void Close()
    {
        if (panelSetting != null)
            panelSetting.SetActive(false);

        if (dimBlocker != null)
            dimBlocker.SetActive(false);
    }

    private void ToggleMusic()
    {
        musicEnabled = !musicEnabled;
        ApplyToggleSprite(musicImage, musicEnabled);
    }

    private void ToggleSfx()
    {
        sfxEnabled = !sfxEnabled;
        ApplyToggleSprite(sfxImage, sfxEnabled);
    }

    private void ToggleAlerts()
    {
        alertsEnabled = !alertsEnabled;
        ApplyToggleSprite(alertsImage, alertsEnabled);
    }

    private static void BindToggle(Image image, UnityEngine.Events.UnityAction callback)
    {
        if (image == null)
            return;

        Button button = image.GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(callback);
    }

    private void ApplyToggleSprites()
    {
        ApplyToggleSprite(musicImage, musicEnabled);
        ApplyToggleSprite(sfxImage, sfxEnabled);
        ApplyToggleSprite(alertsImage, alertsEnabled);
    }

    private void ApplyToggleSprite(Image image, bool isEnabled)
    {
        if (image != null)
            image.sprite = isEnabled ? onSprite : offSprite;
    }
}
