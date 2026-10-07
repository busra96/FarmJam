using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FarmBoxMergePresentationController : MonoBehaviour
{
    [SerializeField] private FarmBoxMergeGameController gameController;
    [SerializeField] private FarmBoxMergeLevelRuntime levelRuntime;
    [SerializeField] private TextMeshProUGUI levelLabel;
    private FarmBoxMergeCanvasLocalization _localization;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (_localization != null) _localization.Changed += RefreshLevelLabel;
        if (gameController != null)
        {
            gameController.AttemptReady += RefreshLevelLabel;
        }

        RefreshLevelLabel();
    }

    private void OnDisable()
    {
        if (_localization != null) _localization.Changed -= RefreshLevelLabel;
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
        levelLabel.text = _localization?.Service != null ? _localization.Service.Format("level", displayIndex) : $"LEVEL {displayIndex}";
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
        if (levelLabel != null) _localization ??= levelLabel.GetComponentInParent<FarmBoxMergeCanvasLocalization>();
    }
}
