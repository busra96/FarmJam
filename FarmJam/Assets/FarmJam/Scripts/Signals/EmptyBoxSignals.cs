namespace Signals
{
    public class EmptyBoxSignals
    {
        public static Signal<EmptyBoxMovement> OnTheBoxHasCompletedTheMovementToTheStartingPosition = new Signal<EmptyBoxMovement>();
        public static Signal<EmptyBox> OnTheEmptyBoxRemoved = new Signal<EmptyBox>();

        public static Signal OnUpdateTetrisLayout = new Signal();
        public static Signal<EmptyBox> OnAddedEmptyBox = new Signal<EmptyBox>();
        public static Signal<EmptyBox> OnRemovedEmptyBox = new Signal<EmptyBox>();
        
    }
}