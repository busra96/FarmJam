using UnityEngine;
using VContainer;
using VContainer.Unity;

[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class FarmBoxMergeLifetimeScope : LifetimeScope
{
    [Header("Configuration")]
    [SerializeField] private FarmBoxMergeAudioCatalog audioCatalog;
    [SerializeField] private FarmBoxMergePrefabCatalog prefabCatalog;
    [SerializeField] private FarmBoxMergeSettings settings;

    protected override void Configure(IContainerBuilder builder)
    {
        if (prefabCatalog == null || settings == null)
        {
            Debug.LogError("FarmBoxMerge prefab catalog or settings asset is missing.", this);
            return;
        }

        builder.RegisterInstance(prefabCatalog);
        builder.RegisterInstance(settings);

        builder.Register<FarmBoxMergeSettingsService>(Lifetime.Singleton)
            .As<IFarmBoxMergeSettingsService>();
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

    private void RegisterOptionalFeatures(IContainerBuilder builder)
    {
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
    }
}
