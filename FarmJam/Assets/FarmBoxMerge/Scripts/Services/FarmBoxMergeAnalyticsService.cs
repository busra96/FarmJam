using System;
using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UnityConsent;

public interface IFarmBoxMergeAnalyticsService
{
    bool IsCollectionEnabled { get; }
    void Initialize();
    void SetConsent(bool granted);
    void RecordLevelStarted(int levelNumber);
    void RecordLevelWon(int levelNumber);
    void RecordLevelFailed(int levelNumber);
}

public sealed class FarmBoxMergeAnalyticsService : IFarmBoxMergeAnalyticsService, IDisposable
{
    private readonly FarmBoxMergeAnalyticsSettings _settings;
    private readonly Queue<Action> _pendingEvents = new Queue<Action>();
    private bool _initializationStarted;
    private bool _serviceInitializationStarted;
    private bool _servicesReady;
    private bool _consentGranted;
    private bool _disposed;

    public FarmBoxMergeAnalyticsService(FarmBoxMergeAnalyticsSettings settings)
    {
        _settings = settings;
    }

    public bool IsCollectionEnabled => !_disposed && IsEnabled && _servicesReady && _consentGranted;
    private bool IsEnabled => _settings != null && _settings.AnalyticsEnabled;

    public void Initialize()
    {
        if (_disposed || _initializationStarted || !IsEnabled)
        {
            return;
        }

        _initializationStarted = true;
        EndUserConsent.consentStateChanged += HandleConsentChanged;
        _consentGranted = EndUserConsent.GetConsentState().AnalyticsIntent == ConsentStatus.Granted;
        if (_consentGranted) InitializeServices();
    }

    private async void InitializeServices()
    {
        if (_disposed || _serviceInitializationStarted) return;
        _serviceInitializationStarted = true;

        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (_disposed) return;
            _servicesReady = UnityServices.State == ServicesInitializationState.Initialized;
            DrainPendingEvents();
        }
        catch (Exception exception)
        {
            if (_disposed) return;
            _servicesReady = false;
            _pendingEvents.Clear();
            Debug.LogWarning($"FarmBoxMerge Analytics initialization failed: {exception.Message}");
        }
    }

    public void SetConsent(bool granted)
    {
        ConsentState state = EndUserConsent.GetConsentState();
        state.AnalyticsIntent = granted ? ConsentStatus.Granted : ConsentStatus.Denied;
        EndUserConsent.SetConsentState(state);
    }

    public void RecordLevelStarted(int levelNumber)
    {
        RecordLevelEvent("fbm_level_start", levelNumber);
    }

    public void RecordLevelWon(int levelNumber)
    {
        RecordLevelEvent("fbm_level_win", levelNumber);
    }

    public void RecordLevelFailed(int levelNumber)
    {
        RecordLevelEvent("fbm_level_fail", levelNumber);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_initializationStarted)
        {
            EndUserConsent.consentStateChanged -= HandleConsentChanged;
        }

        if (IsCollectionEnabled)
        {
            AnalyticsService.Instance.Flush();
        }
        _disposed = true;
        _pendingEvents.Clear();
    }

    private void RecordLevelEvent(string eventName, int levelNumber)
    {
        FarmBoxMergeLevelEvent analyticsEvent = new FarmBoxMergeLevelEvent(eventName)
        {
            LevelNumber = Mathf.Max(1, levelNumber)
        };
        Record(analyticsEvent, $"{eventName} level={levelNumber}");
    }

    private void HandleConsentChanged(ConsentState state)
    {
        _consentGranted = state.AnalyticsIntent == ConsentStatus.Granted;
        if (!_consentGranted)
        {
            _pendingEvents.Clear();
            return;
        }

        InitializeServices();
        DrainPendingEvents();
    }

    private void Record(Unity.Services.Analytics.Event analyticsEvent, string debugSummary)
    {
        if (!IsEnabled || analyticsEvent == null || _disposed)
        {
            return;
        }

#if UNITY_EDITOR
        if (_settings.LogEventsInEditor)
        {
            Debug.Log($"[FarmBoxMerge Analytics] {debugSummary}");
        }
#endif

        if (!_consentGranted)
        {
            return;
        }

        if (_servicesReady)
        {
            SafeRecord(analyticsEvent);
            return;
        }

        int capacity = Mathf.Max(0, _settings.MaxPendingEvents);
        if (!_initializationStarted || capacity == 0)
        {
            return;
        }

        while (_pendingEvents.Count >= capacity)
        {
            _pendingEvents.Dequeue();
        }

        _pendingEvents.Enqueue(() => SafeRecord(analyticsEvent));
    }

    private void DrainPendingEvents()
    {
        if (!IsCollectionEnabled)
        {
            return;
        }

        while (_pendingEvents.Count > 0)
        {
            _pendingEvents.Dequeue().Invoke();
        }
    }

    private static void SafeRecord(Unity.Services.Analytics.Event analyticsEvent)
    {
        try
        {
            AnalyticsService.Instance.RecordEvent(analyticsEvent);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"FarmBoxMerge Analytics event could not be recorded: {exception.Message}");
        }
    }

    private sealed class FarmBoxMergeLevelEvent : Unity.Services.Analytics.Event
    {
        public FarmBoxMergeLevelEvent(string eventName) : base(eventName)
        {
        }

        public int LevelNumber
        {
            set => SetParameter("level_number", value);
        }
    }
}
