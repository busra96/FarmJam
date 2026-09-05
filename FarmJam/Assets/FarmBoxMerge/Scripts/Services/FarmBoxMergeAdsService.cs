using System;
using System.Threading;
using Unity.Services.LevelPlay;
using UnityEngine;

public interface IFarmBoxMergeAdsService
{
    event Action StateChanged;

    bool IsShowingAd { get; }
    bool IsRewardedReady { get; }
    bool IsInterstitialReady { get; }
    string AddCardPlacement { get; }
    string TrashPlacement { get; }
    string RefreshPlacement { get; }
    string RetryPlacement { get; }

    void Initialize();
    bool ShowRewarded(string placement, Action onRewarded, Action onFailed = null);
    bool ShowInterstitialAfterLevel(int completedLevelNumber, Action onClosed);
}

public sealed class FarmBoxMergeLevelPlayAdsService : IFarmBoxMergeAdsService, IDisposable
{
    private readonly FarmBoxMergeAdsSettings _settings;

    private LevelPlayRewardedAd _rewardedAd;
    private LevelPlayInterstitialAd _interstitialAd;
    private SynchronizationContext _mainThreadContext;
    private Action _pendingRewardedSuccess;
    private Action _pendingRewardedFailure;
    private Action _pendingInterstitialClosed;
    private bool _initialized;
    private bool _sdkReady;
    private bool _rewardEarned;
    private bool _rewardCloseQueued;
    private double _rewardedShownAt;

    public FarmBoxMergeLevelPlayAdsService(FarmBoxMergeAdsSettings settings)
    {
        _settings = settings;
    }

    public event Action StateChanged;

    public bool IsShowingAd { get; private set; }
    public bool IsRewardedReady => !IsShowingAd && _rewardedAd != null && _rewardedAd.IsAdReady();
    public bool IsInterstitialReady => !IsShowingAd && _interstitialAd != null && _interstitialAd.IsAdReady();
    public string AddCardPlacement => _settings != null ? _settings.AddCardPlacement : "add_card";
    public string TrashPlacement => _settings != null ? _settings.TrashPlacement : "trash_card";
    public string RefreshPlacement => _settings != null ? _settings.RefreshPlacement : "refresh_level";
    public string RetryPlacement => _settings != null ? _settings.RetryPlacement : "retry_level";

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _mainThreadContext = SynchronizationContext.Current;

        if (_settings == null || !_settings.AdsEnabled)
        {
            Debug.LogWarning("FarmBoxMerge ads are disabled or Ads Settings is missing.");
            StateChanged?.Invoke();
            return;
        }

        if (!_settings.HasPlatformConfiguration)
        {
            Debug.LogWarning(
                "FarmBoxMerge LevelPlay credentials are missing. Fill the platform App Key and Ad Unit IDs in FarmBoxMergeAdsSettings.");
            StateChanged?.Invoke();
            return;
        }

        LevelPlay.OnInitSuccess += HandleInitializationSucceeded;
        LevelPlay.OnInitFailed += HandleInitializationFailed;
        LevelPlay.SetPauseGame(true);
        LevelPlay.Init(_settings.AppKey);
    }

    public bool ShowRewarded(string placement, Action onRewarded, Action onFailed = null)
    {
        if (!IsRewardedReady)
        {
            TryLoadRewarded();
            onFailed?.Invoke();
            StateChanged?.Invoke();
            return false;
        }

        _pendingRewardedSuccess = onRewarded;
        _pendingRewardedFailure = onFailed;
        _rewardEarned = false;
        _rewardCloseQueued = false;
        _rewardedShownAt = Time.realtimeSinceStartupAsDouble;
        SetAdShowing(true);

        try
        {
            _rewardedAd.ShowAd(NormalizePlacement(placement));
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            CompleteRewarded(success: false);
            return false;
        }
    }

    public bool ShowInterstitialAfterLevel(int completedLevelNumber, Action onClosed)
    {
        int interval = _settings != null ? Mathf.Max(1, _settings.InterstitialEveryLevels) : 15;
        bool shouldShow = completedLevelNumber > 0 && completedLevelNumber % interval == 0;
        if (!shouldShow || !IsInterstitialReady)
        {
            TryLoadInterstitial();
            return false;
        }

        _pendingInterstitialClosed = onClosed;
        SetAdShowing(true);

        try
        {
            _interstitialAd.ShowAd(NormalizePlacement(_settings.LevelCompletePlacement));
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            _pendingInterstitialClosed = null;
            SetAdShowing(false);
            TryLoadInterstitial();
            return false;
        }
    }

    public void Dispose()
    {
        LevelPlay.OnInitSuccess -= HandleInitializationSucceeded;
        LevelPlay.OnInitFailed -= HandleInitializationFailed;

        DisposeRewardedAd();
        DisposeInterstitialAd();

        _pendingRewardedSuccess = null;
        _pendingRewardedFailure = null;
        _pendingInterstitialClosed = null;
        IsShowingAd = false;
    }

    private void HandleInitializationSucceeded(LevelPlayConfiguration configuration)
    {
        _sdkReady = true;
        CreateRewardedAd();
        CreateInterstitialAd();
        StateChanged?.Invoke();
    }

    private void HandleInitializationFailed(LevelPlayInitError error)
    {
        _sdkReady = false;
        Debug.LogError($"FarmBoxMerge LevelPlay initialization failed: {error}");
        StateChanged?.Invoke();
    }

    private void CreateRewardedAd()
    {
        DisposeRewardedAd();
        _rewardedAd = new LevelPlayRewardedAd(_settings.RewardedAdUnitId);
        _rewardedAd.OnAdLoaded += HandleRewardedLoaded;
        _rewardedAd.OnAdLoadFailed += HandleRewardedLoadFailed;
        _rewardedAd.OnAdDisplayFailed += HandleRewardedDisplayFailed;
        _rewardedAd.OnAdRewarded += HandleRewardedEarned;
        _rewardedAd.OnAdClosed += HandleRewardedClosed;
        _rewardedAd.LoadAd();
    }

    private void CreateInterstitialAd()
    {
        DisposeInterstitialAd();
        _interstitialAd = new LevelPlayInterstitialAd(_settings.InterstitialAdUnitId);
        _interstitialAd.OnAdLoaded += HandleInterstitialLoaded;
        _interstitialAd.OnAdLoadFailed += HandleInterstitialLoadFailed;
        _interstitialAd.OnAdDisplayFailed += HandleInterstitialDisplayFailed;
        _interstitialAd.OnAdClosed += HandleInterstitialClosed;
        _interstitialAd.LoadAd();
    }

    private void DisposeRewardedAd()
    {
        if (_rewardedAd == null)
        {
            return;
        }

        _rewardedAd.OnAdLoaded -= HandleRewardedLoaded;
        _rewardedAd.OnAdLoadFailed -= HandleRewardedLoadFailed;
        _rewardedAd.OnAdDisplayFailed -= HandleRewardedDisplayFailed;
        _rewardedAd.OnAdRewarded -= HandleRewardedEarned;
        _rewardedAd.OnAdClosed -= HandleRewardedClosed;
        _rewardedAd.Dispose();
        _rewardedAd = null;
    }

    private void DisposeInterstitialAd()
    {
        if (_interstitialAd == null)
        {
            return;
        }

        _interstitialAd.OnAdLoaded -= HandleInterstitialLoaded;
        _interstitialAd.OnAdLoadFailed -= HandleInterstitialLoadFailed;
        _interstitialAd.OnAdDisplayFailed -= HandleInterstitialDisplayFailed;
        _interstitialAd.OnAdClosed -= HandleInterstitialClosed;
        _interstitialAd.Dispose();
        _interstitialAd = null;
    }

    private void HandleRewardedLoaded(LevelPlayAdInfo adInfo)
    {
        StateChanged?.Invoke();
    }

    private void HandleRewardedLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"FarmBoxMerge rewarded ad failed to load: {error}");
        StateChanged?.Invoke();
    }

    private void HandleRewardedDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogWarning($"FarmBoxMerge rewarded ad failed to display: {error}");
        CompleteRewarded(success: false);
    }

    private void HandleRewardedEarned(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        _rewardEarned = DidRewardedAdComplete();
    }

    private void HandleRewardedClosed(LevelPlayAdInfo adInfo)
    {
        if (_rewardCloseQueued)
        {
            return;
        }

        _rewardCloseQueued = true;

        // LevelPlay's Editor mock raises OnAdClosed immediately before OnAdRewarded.
        // Posting completion keeps Editor and device callback ordering equivalent.
        if (_mainThreadContext != null)
        {
            _mainThreadContext.Post(_ => CompleteRewarded(_rewardEarned), null);
            return;
        }

        CompleteRewarded(_rewardEarned);
    }

    private void HandleInterstitialLoaded(LevelPlayAdInfo adInfo)
    {
        StateChanged?.Invoke();
    }

    private void HandleInterstitialLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"FarmBoxMerge interstitial ad failed to load: {error}");
        StateChanged?.Invoke();
    }

    private void HandleInterstitialDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogWarning($"FarmBoxMerge interstitial ad failed to display: {error}");
        CompleteInterstitial();
    }

    private void HandleInterstitialClosed(LevelPlayAdInfo adInfo)
    {
        CompleteInterstitial();
    }

    private void CompleteRewarded(bool success)
    {
        Action completion = success ? _pendingRewardedSuccess : _pendingRewardedFailure;
        _pendingRewardedSuccess = null;
        _pendingRewardedFailure = null;
        _rewardEarned = false;
        _rewardCloseQueued = false;
        _rewardedShownAt = 0d;
        SetAdShowing(false);
        completion?.Invoke();
        TryLoadRewarded();
    }

    private void CompleteInterstitial()
    {
        Action completion = _pendingInterstitialClosed;
        _pendingInterstitialClosed = null;
        SetAdShowing(false);
        completion?.Invoke();
        TryLoadInterstitial();
    }

    private void TryLoadRewarded()
    {
        if (_sdkReady && _rewardedAd != null && !_rewardedAd.IsAdReady())
        {
            _rewardedAd.LoadAd();
        }
    }

    private void TryLoadInterstitial()
    {
        if (_sdkReady && _interstitialAd != null && !_interstitialAd.IsAdReady())
        {
            _interstitialAd.LoadAd();
        }
    }

    private void SetAdShowing(bool showing)
    {
        if (IsShowingAd == showing)
        {
            return;
        }

        IsShowingAd = showing;
        StateChanged?.Invoke();
    }

    private static string NormalizePlacement(string placement)
    {
        return string.IsNullOrWhiteSpace(placement) ? null : placement.Trim();
    }

    private bool DidRewardedAdComplete()
    {
#if UNITY_EDITOR
        if (_settings != null && _settings.UseEditorMockAds)
        {
            double watchedDuration = Time.realtimeSinceStartupAsDouble - _rewardedShownAt;
            return watchedDuration >= _settings.EditorMockRewardDuration;
        }
#endif

        // On a device, LevelPlay only emits OnAdRewarded after the network confirms
        // that the rewarded creative reached its configured completion point.
        return true;
    }
}
