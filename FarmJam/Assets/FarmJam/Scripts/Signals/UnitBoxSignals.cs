namespace Signals
{
    public class UnitBoxSignals
    {
        public static Signal<UnitBox> OnThisUnitBoxDestroyed = new Signal<UnitBox>();
        public static Signal OnThisUnitBoxIsFullCheck = new Signal();
    }
}
