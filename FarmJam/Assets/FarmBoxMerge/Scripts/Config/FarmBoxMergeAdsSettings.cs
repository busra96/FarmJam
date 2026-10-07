using UnityEngine;

[CreateAssetMenu(fileName = "FarmBoxMergeAdsSettings", menuName = "FarmBoxMerge/Ads Settings")]
public sealed class FarmBoxMergeAdsSettings : ScriptableObject
{
    [Header("General")]
    [field: SerializeField] public bool AdsEnabled { get; private set; } = true;
    [field: SerializeField] public bool UseEditorMockAds { get; private set; } = true;
    [field: SerializeField, Min(0.1f)] public float EditorMockRewardDuration { get; private set; } = 5f;

    [Header("Android LevelPlay")]
    [field: SerializeField] public string AndroidAppKey { get; private set; }
    [field: SerializeField] public string AndroidRewardedAdUnitId { get; private set; }
    [field: SerializeField] public string AndroidInterstitialAdUnitId { get; private set; }

    [Header("iOS LevelPlay")]
    [field: SerializeField] public string IosAppKey { get; private set; }
    [field: SerializeField] public string IosRewardedAdUnitId { get; private set; }
    [field: SerializeField] public string IosInterstitialAdUnitId { get; private set; }

    [Header("Rewarded Placements")]
    [field: SerializeField] public string AddCardPlacement { get; private set; } = "add_card";
    [field: SerializeField] public string TrashPlacement { get; private set; } = "trash_card";
    [field: SerializeField] public string RefreshPlacement { get; private set; } = "refresh_level";
    [field: SerializeField] public string RetryPlacement { get; private set; } = "retry_level";

    [Header("Interstitial")]
    [field: SerializeField] public string LevelCompletePlacement { get; private set; } = "level_complete";
    [field: SerializeField, Min(1)] public int InterstitialEveryLevels { get; private set; } = 15;

    public string AppKey => ResolvePlatformValue(AndroidAppKey, IosAppKey, "editor_mock_app_key");
    public string RewardedAdUnitId => ResolvePlatformValue(
        AndroidRewardedAdUnitId,
        IosRewardedAdUnitId,
        "editor_mock_rewarded");
    public string InterstitialAdUnitId => ResolvePlatformValue(
        AndroidInterstitialAdUnitId,
        IosInterstitialAdUnitId,
        "editor_mock_interstitial");

    public bool HasPlatformConfiguration => !string.IsNullOrWhiteSpace(AppKey)
        && !string.IsNullOrWhiteSpace(RewardedAdUnitId)
        && !string.IsNullOrWhiteSpace(InterstitialAdUnitId);

    public bool HasAndroidConfiguration => HasConfiguration(
        AndroidAppKey,
        AndroidRewardedAdUnitId,
        AndroidInterstitialAdUnitId);

    public bool HasIosConfiguration => HasConfiguration(
        IosAppKey,
        IosRewardedAdUnitId,
        IosInterstitialAdUnitId);

    private string ResolvePlatformValue(string androidValue, string iosValue, string editorMockValue)
    {
#if UNITY_EDITOR
        if (UseEditorMockAds)
        {
            return editorMockValue;
        }
#endif

#if UNITY_IOS
        return iosValue != null ? iosValue.Trim() : string.Empty;
#else
        return androidValue != null ? androidValue.Trim() : string.Empty;
#endif
    }

    private static bool HasConfiguration(string appKey, string rewardedAdUnitId, string interstitialAdUnitId)
    {
        return !string.IsNullOrWhiteSpace(appKey)
            && !string.IsNullOrWhiteSpace(rewardedAdUnitId)
            && !string.IsNullOrWhiteSpace(interstitialAdUnitId);
    }

    private void OnValidate()
    {
        EditorMockRewardDuration = Mathf.Max(0.1f, EditorMockRewardDuration);
        InterstitialEveryLevels = Mathf.Max(1, InterstitialEveryLevels);
    }
}
