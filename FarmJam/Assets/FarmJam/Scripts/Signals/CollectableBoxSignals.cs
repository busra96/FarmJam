namespace Signals
{
    public class CollectableBoxSignals
    {
        public static Signal OnCollectableBoxControl = new Signal();
        public static Signal<CollectableBox> OnCollectableBoxDestroyed = new Signal<CollectableBox>();
    }
}