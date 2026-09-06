using System;

public sealed class FarmBoxMergeAnalyticsTracker : IDisposable
{
    private readonly IFarmBoxMergeAnalyticsService _analytics;
    private readonly FarmBoxMergeLevelRuntime _levelRuntime;
    private readonly IFarmBoxMergeOutcomeMonitor _outcomeMonitor;
    private bool _initialized;

    public FarmBoxMergeAnalyticsTracker(
        IFarmBoxMergeAnalyticsService analytics,
        FarmBoxMergeLevelRuntime levelRuntime,
        IFarmBoxMergeOutcomeMonitor outcomeMonitor)
    {
        _analytics = analytics;
        _levelRuntime = levelRuntime;
        _outcomeMonitor = outcomeMonitor;
    }

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        if (_levelRuntime != null)
        {
            _levelRuntime.CurrentLevelSpawned += HandleLevelSpawned;
        }

        if (_outcomeMonitor != null)
        {
            _outcomeMonitor.OutcomeResolved += HandleOutcomeResolved;
        }
    }

    public void Dispose()
    {
        if (!_initialized)
        {
            return;
        }

        _initialized = false;
        if (_levelRuntime != null)
        {
            _levelRuntime.CurrentLevelSpawned -= HandleLevelSpawned;
        }

        if (_outcomeMonitor != null)
        {
            _outcomeMonitor.OutcomeResolved -= HandleOutcomeResolved;
        }
    }

    private void HandleLevelSpawned(FarmBoxMergeLevelDefinition level)
    {
        _analytics?.RecordLevelStarted(CurrentLevelNumber);
    }

    private void HandleOutcomeResolved(bool won)
    {
        if (won)
        {
            _analytics?.RecordLevelWon(CurrentLevelNumber);
            return;
        }

        _analytics?.RecordLevelFailed(CurrentLevelNumber);
    }

    private int CurrentLevelNumber => _levelRuntime != null
        ? _levelRuntime.CurrentLevelIndex + 1
        : 1;
}
