using VContainer.Unity;

public interface IFarmBoxMergeOutcomeMonitor
{
    void Initialize();
    void Tick();
}

public interface IFarmBoxMergeLayoutController
{
    void Initialize();
    void Tick();
}

public sealed class FarmBoxMergeNullOutcomeMonitor : IFarmBoxMergeOutcomeMonitor
{
    public void Initialize() { }
    public void Tick() { }
}

public sealed class FarmBoxMergeNullLayoutController : IFarmBoxMergeLayoutController
{
    public void Initialize() { }
    public void Tick() { }
}

public sealed class FarmBoxMergeRuntimeTicker : ITickable
{
    private readonly IFarmBoxMergeOutcomeMonitor _outcomeMonitor;
    private readonly IFarmBoxMergeLayoutController _layoutController;

    public FarmBoxMergeRuntimeTicker(
        IFarmBoxMergeOutcomeMonitor outcomeMonitor,
        IFarmBoxMergeLayoutController layoutController)
    {
        _outcomeMonitor = outcomeMonitor;
        _layoutController = layoutController;
    }

    public void Tick()
    {
        _outcomeMonitor.Tick();
        _layoutController.Tick();
    }
}
