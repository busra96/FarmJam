using System;
using VContainer.Unity;

public interface IFarmBoxMergeOutcomeMonitor
{
    event Action OutcomeShown;
    void Initialize();
    void Tick();
}

public interface IFarmBoxMergeSettingsPanel
{
    void Initialize();
}

public interface IFarmBoxMergeLayoutController
{
    void Initialize();
    void Tick();
}

public sealed class FarmBoxMergeNullOutcomeMonitor : IFarmBoxMergeOutcomeMonitor
{
    public event Action OutcomeShown
    {
        add { }
        remove { }
    }

    public void Initialize() { }
    public void Tick() { }
}

public sealed class FarmBoxMergeNullSettingsPanel : IFarmBoxMergeSettingsPanel
{
    public void Initialize() { }
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
