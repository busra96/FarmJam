using VContainer.Unity;

public sealed class FarmBoxMergeRuntimeTicker : ITickable
{
    private readonly FarmBoxMergeOutcomeController _outcomeController;
    private readonly FarmBoxMergeAdaptiveLayout _adaptiveLayout;

    public FarmBoxMergeRuntimeTicker(
        FarmBoxMergeOutcomeController outcomeController,
        FarmBoxMergeAdaptiveLayout adaptiveLayout)
    {
        _outcomeController = outcomeController;
        _adaptiveLayout = adaptiveLayout;
    }

    public void Tick()
    {
        _outcomeController.Tick();
        _adaptiveLayout.Tick();
    }
}
