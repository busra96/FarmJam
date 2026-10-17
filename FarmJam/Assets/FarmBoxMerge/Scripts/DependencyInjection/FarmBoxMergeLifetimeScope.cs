using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class FarmBoxMergeLifetimeScope : LifetimeScope
{
    [Header("Configuration")]
    [SerializeField] private FarmBoxMergeAudioCatalog audioCatalog;
    [SerializeField] private FarmBoxMergePrefabCatalog prefabCatalog;
    [SerializeField] private FarmBoxMergeSettings settings;
    [SerializeField] private FarmBoxMergeAdsSettings adsSettings;
    [SerializeField] private FarmBoxMergeAnalyticsSettings analyticsSettings;

    protected override void Configure(IContainerBuilder builder)
    {
        List<string> missingConfiguration = GetMissingConfiguration();
        if (missingConfiguration.Count > 0)
        {
            throw new InvalidOperationException(
                "FarmBoxMerge configuration is incomplete: " + string.Join(", ", missingConfiguration));
        }

        builder.RegisterInstance(prefabCatalog);
        builder.RegisterInstance(settings);
        builder.RegisterInstance(settings.Localization);
        builder.Register<FarmBoxMergeLocalizationService>(Lifetime.Singleton).As<IFarmBoxMergeLocalizationService>();
        builder.RegisterComponentInHierarchy<FarmBoxMergeCanvasLocalization>();
        builder.RegisterInstance(adsSettings);
        builder.RegisterInstance(analyticsSettings);
        builder.Register<FarmBoxMergeTutorialProgress>(Lifetime.Singleton)
            .As<IFarmBoxMergeTutorialProgress>();

        builder.Register<FarmBoxMergeSettingsService>(Lifetime.Singleton)
            .As<IFarmBoxMergeSettingsService>();
#if UNITY_WEBGL || FARMBOXMERGE_ITCHIO
        builder.Register<FarmBoxMergeOfflineAdsService>(Lifetime.Singleton)
            .As<IFarmBoxMergeAdsService>();
        builder.Register<FarmBoxMergeOfflineAnalyticsService>(Lifetime.Singleton)
            .As<IFarmBoxMergeAnalyticsService>();
#else
        builder.Register<FarmBoxMergeLevelPlayAdsService>(Lifetime.Singleton)
            .As<IFarmBoxMergeAdsService>();
        builder.Register<FarmBoxMergeAnalyticsService>(Lifetime.Singleton)
            .As<IFarmBoxMergeAnalyticsService>();
#endif
        builder.Register<FarmBoxMergeAnalyticsTracker>(Lifetime.Singleton);
        builder.Register<FarmBoxMergeBoxRegistry>(Lifetime.Singleton)
            .As<IFarmBoxMergeBoxRegistry>();
        builder.Register<FarmBoxMergeCardFactory>(Lifetime.Singleton)
            .As<ICardFactory>();
        builder.Register<FarmBoxMergeBoxFactory>(Lifetime.Singleton)
            .As<IBoxFactory>();
        builder.Register<FarmBoxMergeItemFactory>(Lifetime.Singleton)
            .As<IMergeItemFactory>();

        RegisterOptionalFeatures(builder);
        builder.RegisterComponentInHierarchy<CardSpawner>();
        builder.RegisterComponentInHierarchy<CardMergeBoard>();
        builder.RegisterComponentInHierarchy<MergeItemSpawner>();
        builder.RegisterComponentInHierarchy<FarmBoxMergeActionBudget>();
        builder.RegisterComponentInHierarchy<FarmBoxMergeLevelRuntime>();
        builder.RegisterComponentInHierarchy<FarmBoxMergeGameController>();

        builder.RegisterEntryPoint<FarmBoxMergeBootstrapper>();
        builder.RegisterEntryPoint<FarmBoxMergeRuntimeTicker>();
    }

    private List<string> GetMissingConfiguration()
    {
        var missing = new List<string>();
        if (prefabCatalog == null) missing.Add(nameof(prefabCatalog));
        if (settings == null) missing.Add(nameof(settings));
        else if (settings.Localization == null) missing.Add(nameof(settings) + ".Localization");
        if (adsSettings == null) missing.Add(nameof(adsSettings));
        if (analyticsSettings == null) missing.Add(nameof(analyticsSettings));
        return missing;
    }

    private void RegisterOptionalFeatures(IContainerBuilder builder)
    {
        FarmBoxMergeTutorialController tutorial =
            Object.FindFirstObjectByType<FarmBoxMergeTutorialController>(FindObjectsInactive.Include);
        if (tutorial != null)
            builder.RegisterComponent(tutorial).AsSelf().As<IFarmBoxMergeTutorialFeature>();
        else
            builder.Register<FarmBoxMergeNullTutorialFeature>(Lifetime.Singleton).As<IFarmBoxMergeTutorialFeature>();

        FarmBoxMergeFeedbackController feedbackController =
            Object.FindFirstObjectByType<FarmBoxMergeFeedbackController>(FindObjectsInactive.Include);
        if (feedbackController != null && audioCatalog != null)
        {
            builder.RegisterInstance(audioCatalog);
            builder.RegisterComponent(feedbackController)
                .AsSelf()
                .As<IFarmBoxMergeFeedbackService>();
        }
        else
        {
            if (feedbackController != null && audioCatalog == null)
            {
                Debug.LogWarning(
                    "FarmBoxMerge audio catalog is missing; feedback feature will stay disabled.",
                    this);
            }

            builder.Register<FarmBoxMergeNullFeedbackService>(Lifetime.Singleton)
                .As<IFarmBoxMergeFeedbackService>();
        }

        FarmBoxMergeOutcomeController outcomeController =
            Object.FindFirstObjectByType<FarmBoxMergeOutcomeController>(FindObjectsInactive.Include);
        if (outcomeController != null)
        {
            builder.RegisterComponent(outcomeController)
                .AsSelf()
                .As<IFarmBoxMergeOutcomeMonitor>();
        }
        else
        {
            builder.Register<FarmBoxMergeNullOutcomeMonitor>(Lifetime.Singleton)
                .As<IFarmBoxMergeOutcomeMonitor>();
        }

        FarmBoxMergeAdaptiveLayout adaptiveLayout =
            Object.FindFirstObjectByType<FarmBoxMergeAdaptiveLayout>(FindObjectsInactive.Include);
        if (adaptiveLayout != null)
        {
            builder.RegisterComponent(adaptiveLayout)
                .AsSelf()
                .As<IFarmBoxMergeLayoutController>();
        }
        else
        {
            builder.Register<FarmBoxMergeNullLayoutController>(Lifetime.Singleton)
                .As<IFarmBoxMergeLayoutController>();
        }

        FarmBoxMergeSettingsPanelController settingsPanel =
            Object.FindFirstObjectByType<FarmBoxMergeSettingsPanelController>(FindObjectsInactive.Include);
        if (settingsPanel != null)
        {
            builder.RegisterComponent(settingsPanel)
                .AsSelf()
                .As<IFarmBoxMergeSettingsPanel>();
        }
        else
        {
            builder.Register<FarmBoxMergeNullSettingsPanel>(Lifetime.Singleton)
                .As<IFarmBoxMergeSettingsPanel>();
        }
    }
}
