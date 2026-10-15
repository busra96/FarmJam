using System;

public sealed class FarmBoxMergeOfflineAdsService : IFarmBoxMergeAdsService
{
    public event Action StateChanged { add { } remove { } }
    public bool IsShowingAd => false;
    public bool IsRewardedReady => true;
    public bool IsInterstitialReady => false;
    public string AddCardPlacement => "add_card";
    public string TrashPlacement => "trash_card";
    public string RefreshPlacement => "refresh_level";
    public string RetryPlacement => "retry_level";
    public void Initialize() { }

    public bool ShowRewarded(string placement, Action onRewarded, Action onFailed = null)
    {
        if (placement != RefreshPlacement && placement != RetryPlacement)
        {
            onFailed?.Invoke();
            return false;
        }
        onRewarded?.Invoke();
        return true;
    }

    public bool ShowInterstitialAfterLevel(int completedLevelNumber, Action onClosed) => false;
}

public sealed class FarmBoxMergeOfflineAnalyticsService : IFarmBoxMergeAnalyticsService
{
    public bool IsCollectionEnabled => false;
    public void Initialize() { }
    public void SetConsent(bool granted) { }
    public void RecordLevelStarted(int levelNumber) { }
    public void RecordLevelWon(int levelNumber) { }
    public void RecordLevelFailed(int levelNumber) { }
}
