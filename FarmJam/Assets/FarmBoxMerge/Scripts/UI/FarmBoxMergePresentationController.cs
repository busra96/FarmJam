using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FarmBoxMergePresentationController : MonoBehaviour
{
    [SerializeField] private FarmBoxMergeGameController gameController;
    [SerializeField] private FarmBoxMergeLevelRuntime levelRuntime;
    [SerializeField] private TextMeshProUGUI levelLabel;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (gameController != null)
        {
            gameController.AttemptReady += RefreshLevelLabel;
        }

        RefreshLevelLabel();
    }

    private void OnDisable()
    {
        if (gameController != null)
        {
            gameController.AttemptReady -= RefreshLevelLabel;
        }
    }

    public void RefreshLevelLabel()
    {
        if (levelLabel == null)
        {
            return;
        }

        int displayIndex = levelRuntime != null ? levelRuntime.CurrentLevelIndex + 1 : 1;
        levelLabel.text = $"LEVEL {displayIndex}";
    }

    private void ResolveReferences()
    {
        gameController ??= GetComponent<FarmBoxMergeGameController>();
        gameController ??= FarmBoxMergeObjectUtility.FindSceneComponent<FarmBoxMergeGameController>();
        levelRuntime ??= FarmBoxMergeObjectUtility.FindSceneComponent<FarmBoxMergeLevelRuntime>();

        if (levelLabel == null)
        {
            Transform labelTransform = transform.Find("LevelLabel");
            if (labelTransform != null)
            {
                levelLabel = labelTransform.GetComponent<TextMeshProUGUI>();
            }
        }
    }
}
