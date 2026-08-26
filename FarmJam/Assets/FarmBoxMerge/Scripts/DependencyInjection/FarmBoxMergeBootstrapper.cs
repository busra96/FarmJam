using VContainer.Unity;

public sealed class FarmBoxMergeBootstrapper : IStartable
{
    private readonly FarmBoxMergeFeedbackController _feedback;
    private readonly FarmBoxMergeLevelRuntime _levelRuntime;
    private readonly CardMergeBoard _board;
    private readonly MergeItemSpawner _itemSpawner;
    private readonly CardSpawner _cardSpawner;
    private readonly FarmBoxMergeGameController _gameController;
    private readonly FarmBoxMergeOutcomeController _outcomeController;
    private readonly FarmBoxMergeAdaptiveLayout _adaptiveLayout;

    public FarmBoxMergeBootstrapper(
        FarmBoxMergeFeedbackController feedback,
        FarmBoxMergeLevelRuntime levelRuntime,
        CardMergeBoard board,
        MergeItemSpawner itemSpawner,
        CardSpawner cardSpawner,
        FarmBoxMergeGameController gameController,
        FarmBoxMergeOutcomeController outcomeController,
        FarmBoxMergeAdaptiveLayout adaptiveLayout)
    {
        _feedback = feedback;
        _levelRuntime = levelRuntime;
        _board = board;
        _itemSpawner = itemSpawner;
        _cardSpawner = cardSpawner;
        _gameController = gameController;
        _outcomeController = outcomeController;
        _adaptiveLayout = adaptiveLayout;
    }

    public void Start()
    {
        _feedback.Initialize();
        _levelRuntime.Initialize();
        _board.Initialize();
        _itemSpawner.Initialize();
        _cardSpawner.Initialize();
        _gameController.Initialize();
        _outcomeController.Initialize();
        _adaptiveLayout.Initialize();
    }
}
