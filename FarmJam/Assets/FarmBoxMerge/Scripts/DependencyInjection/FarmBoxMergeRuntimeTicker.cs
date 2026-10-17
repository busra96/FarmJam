using System;
using VContainer.Unity;

public interface IFarmBoxMergeOutcomeMonitor
{
    event Action OutcomeShown;
    event Action<bool> OutcomeResolved;
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

    public event Action<bool> OutcomeResolved
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
    private readonly CardMergeBoard _cardMergeBoard;

    public FarmBoxMergeRuntimeTicker(
        IFarmBoxMergeOutcomeMonitor outcomeMonitor,
        IFarmBoxMergeLayoutController layoutController,
        CardMergeBoard cardMergeBoard)
    {
        _outcomeMonitor = outcomeMonitor;
        _layoutController = layoutController;
        _cardMergeBoard = cardMergeBoard;
    }

    public void Tick()
    {
        _outcomeMonitor.Tick();
        _layoutController.Tick();
        _cardMergeBoard.TickCardVisuals();
    }
}
