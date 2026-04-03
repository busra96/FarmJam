namespace Signals
{
    public class FB_UnitBoxSignals
    {
        public static Signal<FarmBlast.UnitBox> OnThisUnitBoxDestroyed = new Signal<FarmBlast.UnitBox>();
        public static Signal OnThisUnitBoxIsFullCheck = new Signal();
    }
}