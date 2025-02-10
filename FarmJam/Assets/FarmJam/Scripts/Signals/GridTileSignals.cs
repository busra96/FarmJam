namespace Signals
{
    public class GridTileSignals
    {
        public static Signal OnGridMaterialCheck = new Signal();
        
        public static Signal<UnitBox> OnAddedUnitBox = new Signal<UnitBox>();
        public static Signal<UnitBox> OnRemovedUnitBox = new Signal<UnitBox>();
    }
}