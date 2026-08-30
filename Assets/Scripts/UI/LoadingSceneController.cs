using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public sealed class LoadingSceneController : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "GameplayScene";
    [SerializeField, Min(0.5f)] private float minimumDisplaySeconds = 2.5f;
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private Transform fillTransform;
    [SerializeField] private Transform leafyTransform;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private float fillStartX = -2.75f;
    [SerializeField] private float fillEndX = 2.75f;
    [SerializeField] private float leafyLeadOffsetX = -0.38f;

    private Vector3 fillFullScale;
    private float leafySceneY;
    private float leafySceneZ;

    private void Awake()
    {
        if (leafyTransform != null)
        {
            leafySceneY = leafyTransform.localPosition.y;
            leafySceneZ = leafyTransform.localPosition.z;
        }

        // The monster prefab normally receives a large world Y-sort order.
        // Loading art must always render in front of the complete monster.
        SortingGroup leafySorting = leafyTransform != null
            ? leafyTransform.GetComponent<SortingGroup>()
            : null;
        if (leafySorting != null)
        {
            leafySorting.sortingLayerID = 0;
            leafySorting.sortingOrder = 1;
        }
    }

    private IEnumerator Start()
    {
        FitBackgroundToCamera();
        fillFullScale = fillTransform.localScale;
        SetVisualProgress(0f);

        AsyncOperation load = SceneManager.LoadSceneAsync(targetSceneName);
        if (load == null)
            yield break;

        load.allowSceneActivation = false;
        float elapsed = 0f;
        float displayedProgress = 0f;

        while (!load.isDone)
        {
            elapsed += Time.unscaledDeltaTime;
            float sceneProgress = Mathf.Clamp01(load.progress / 0.9f);
            float timeProgress = Mathf.Clamp01(elapsed / minimumDisplaySeconds);
            float targetProgress = Mathf.Min(sceneProgress, timeProgress);
            displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, Time.unscaledDeltaTime * 0.7f);
            SetVisualProgress(displayedProgress);

            if (sceneProgress >= 1f && timeProgress >= 1f && displayedProgress >= 0.995f)
            {
                SetVisualProgress(1f);
                load.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private void SetVisualProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);
        Vector3 scale = fillFullScale;
        scale.x = fillFullScale.x * progress;
        fillTransform.localScale = scale;

        float fillCenter = Mathf.Lerp(fillStartX, fillEndX, progress) * 0.5f + fillStartX * 0.5f;
        Vector3 fillPosition = fillTransform.localPosition;
        fillPosition.x = fillCenter;
        fillTransform.localPosition = fillPosition;

        Vector3 leafyPosition = leafyTransform.localPosition;
        leafyPosition.x = Mathf.Lerp(fillStartX, fillEndX, progress) + leafyLeadOffsetX;
        leafyPosition.y = leafySceneY;
        leafyPosition.z = leafySceneZ;
        leafyTransform.localPosition = leafyPosition;

        if (loadingText != null)
            loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100f)}%";
    }

    private void FitBackgroundToCamera()
    {
        if (backgroundRenderer == null || backgroundRenderer.sprite == null || Camera.main == null)
            return;

        Vector2 spriteSize = backgroundRenderer.sprite.bounds.size;
        float worldHeight = Camera.main.orthographicSize * 2f;
        float worldWidth = worldHeight * Camera.main.aspect;
        float scale = Mathf.Max(worldWidth / spriteSize.x, worldHeight / spriteSize.y);
        backgroundRenderer.transform.localScale = new Vector3(scale, scale, 1f);
    }
}
