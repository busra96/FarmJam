namespace Signals
{
    public class EmptyBoxSignals
    {
        public static Signal<EmptyBoxMovement> OnTheBoxHasCompletedTheMovementToTheStartingPosition = new Signal<EmptyBoxMovement>();
        public static Signal<EmptyBox> OnTheEmptyBoxRemoved = new Signal<EmptyBox>();
    }
}
