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
        if (audioCatalog == null || prefabCatalog == null || settings == null)
        {
            Debug.LogError("FarmBoxMerge VContainer configuration assets are missing.", this);
            return;
        }

        builder.RegisterInstance(audioCatalog);
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

        builder.RegisterComponentInHierarchy<FarmBoxMergeFeedbackController>()
            .AsSelf()
            .As<IFarmBoxMergeFeedbackService>();
        builder.RegisterComponentInHierarchy<CardSpawner>();
        builder.RegisterComponentInHierarchy<CardMergeBoard>();
        builder.RegisterComponentInHierarchy<MergeItemSpawner>();
        builder.RegisterComponentInHierarchy<FarmBoxMergeActionBudget>();
        builder.RegisterComponentInHierarchy<FarmBoxMergeOutcomeController>();
        builder.RegisterComponentInHierarchy<FarmBoxMergeAdaptiveLayout>();
        builder.RegisterComponentInHierarchy<FarmBoxMergeLevelRuntime>();
        builder.RegisterComponentInHierarchy<FarmBoxMergeGameController>();

        builder.RegisterEntryPoint<FarmBoxMergeBootstrapper>();
        builder.RegisterEntryPoint<FarmBoxMergeRuntimeTicker>();
    }
}
