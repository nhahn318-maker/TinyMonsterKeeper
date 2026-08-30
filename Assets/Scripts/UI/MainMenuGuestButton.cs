using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class MainMenuGuestButton : MonoBehaviour
{
    private const string LoadingSceneName = "LoadingScene";

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(PlayAsGuest);
    }

    private void OnDestroy()
    {
        GetComponent<Button>().onClick.RemoveListener(PlayAsGuest);
    }

    public void PlayAsGuest()
    {
        SceneManager.LoadScene(LoadingSceneName);
    }
}
