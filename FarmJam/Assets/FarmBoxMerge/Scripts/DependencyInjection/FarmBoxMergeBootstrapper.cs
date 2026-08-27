using VContainer.Unity;

public sealed class FarmBoxMergeBootstrapper : IStartable
{
    private readonly IFarmBoxMergeFeedbackService _feedback;
    private readonly FarmBoxMergeLevelRuntime _levelRuntime;
    private readonly CardMergeBoard _board;
    private readonly MergeItemSpawner _itemSpawner;
    private readonly CardSpawner _cardSpawner;
    private readonly FarmBoxMergeGameController _gameController;
    private readonly IFarmBoxMergeOutcomeMonitor _outcomeMonitor;
    private readonly IFarmBoxMergeLayoutController _layoutController;
    private readonly IFarmBoxMergeSettingsPanel _settingsPanel;

    public FarmBoxMergeBootstrapper(
        IFarmBoxMergeFeedbackService feedback,
        FarmBoxMergeLevelRuntime levelRuntime,
        CardMergeBoard board,
        MergeItemSpawner itemSpawner,
        CardSpawner cardSpawner,
        FarmBoxMergeGameController gameController,
        IFarmBoxMergeOutcomeMonitor outcomeMonitor,
        IFarmBoxMergeLayoutController layoutController,
        IFarmBoxMergeSettingsPanel settingsPanel)
    {
        _feedback = feedback;
        _levelRuntime = levelRuntime;
        _board = board;
        _itemSpawner = itemSpawner;
        _cardSpawner = cardSpawner;
        _gameController = gameController;
        _outcomeMonitor = outcomeMonitor;
        _layoutController = layoutController;
        _settingsPanel = settingsPanel;
    }

    public void Start()
    {
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
    }
}
