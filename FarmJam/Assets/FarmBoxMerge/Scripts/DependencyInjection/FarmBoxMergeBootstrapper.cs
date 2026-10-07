using VContainer.Unity;

public sealed class FarmBoxMergeBootstrapper : IStartable
{
    private readonly IFarmBoxMergeFeedbackService _feedback;
    private readonly IFarmBoxMergeAnalyticsService _analytics;
    private readonly FarmBoxMergeAnalyticsTracker _analyticsTracker;
    private readonly IFarmBoxMergeAdsService _ads;
    private readonly FarmBoxMergeLevelRuntime _levelRuntime;
    private readonly CardMergeBoard _board;
    private readonly MergeItemSpawner _itemSpawner;
    private readonly CardSpawner _cardSpawner;
    private readonly FarmBoxMergeGameController _gameController;
    private readonly IFarmBoxMergeOutcomeMonitor _outcomeMonitor;
    private readonly IFarmBoxMergeLayoutController _layoutController;
    private readonly IFarmBoxMergeSettingsPanel _settingsPanel;
    private readonly IFarmBoxMergeTutorialFeature _tutorial;

    public FarmBoxMergeBootstrapper(
        IFarmBoxMergeFeedbackService feedback,
        IFarmBoxMergeAnalyticsService analytics,
        FarmBoxMergeAnalyticsTracker analyticsTracker,
        IFarmBoxMergeAdsService ads,
        FarmBoxMergeLevelRuntime levelRuntime,
        CardMergeBoard board,
        MergeItemSpawner itemSpawner,
        CardSpawner cardSpawner,
        FarmBoxMergeGameController gameController,
        IFarmBoxMergeOutcomeMonitor outcomeMonitor,
        IFarmBoxMergeLayoutController layoutController,
        IFarmBoxMergeSettingsPanel settingsPanel,
        IFarmBoxMergeTutorialFeature tutorial)
    {
        _feedback = feedback;
        _analytics = analytics;
        _analyticsTracker = analyticsTracker;
        _ads = ads;
        _levelRuntime = levelRuntime;
        _board = board;
        _itemSpawner = itemSpawner;
        _cardSpawner = cardSpawner;
        _gameController = gameController;
        _outcomeMonitor = outcomeMonitor;
        _layoutController = layoutController;
        _settingsPanel = settingsPanel;
        _tutorial = tutorial;
    }

    public void Start()
    {
        _analytics.Initialize();
        _analyticsTracker.Initialize();
        _ads.Initialize();

        if (_feedback is FarmBoxMergeFeedbackController feedbackController)
        {
            feedbackController.Initialize();
        }
        _levelRuntime.Initialize();
        _board.Initialize();
        _itemSpawner.Initialize();
        _cardSpawner.Initialize();
        _gameController.Initialize();
        _outcomeMonitor.Initialize();
        _layoutController.Initialize();
        _settingsPanel.Initialize();
        _tutorial.Initialize();
    }
}
