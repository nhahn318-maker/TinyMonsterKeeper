using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BookOpenUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image bookImage;
    [SerializeField] private Sprite[] openFrames;
    [SerializeField] private float openDuration = 0.45f;

    private Coroutine openRoutine;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        if (panelRoot == null)
            return;

        panelRoot.SetActive(true);

        if (openRoutine != null)
            StopCoroutine(openRoutine);

        openRoutine = StartCoroutine(PlayOpenAnimation());
    }

    public void Hide()
    {
        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
            openRoutine = null;
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private IEnumerator PlayOpenAnimation()
    {
        if (bookImage == null || openFrames == null || openFrames.Length == 0)
            yield break;

        float frameDelay = openDuration / openFrames.Length;

        for (int i = 0; i < openFrames.Length; i++)
        {
            bookImage.sprite = openFrames[i];
            yield return new WaitForSeconds(frameDelay);
        }

        openRoutine = null;
    }
}
