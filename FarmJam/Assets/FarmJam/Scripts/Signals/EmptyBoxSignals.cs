namespace Signals
{
    public class EmptyBoxSignals
    {
        public static Signal<EmptyBoxMovement> OnTheBoxHasCompletedTheMovementToTheStartingPosition = new Signal<EmptyBoxMovement>();
        public static Signal<EmptyBox> OnTheEmptyBoxRemoved = new Signal<EmptyBox>();

        public static Signal<EmptyBox> OnAddedTetrisSpacingLayoutList = new Signal<EmptyBox>();
        public static Signal<EmptyBox> OnRemovedTetrisLayoutList = new Signal<EmptyBox>();
        
        public static Signal OnUpdateTetrisLayout = new Signal();
    }
}
