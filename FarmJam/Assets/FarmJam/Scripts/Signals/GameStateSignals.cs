namespace Signals
{
    public class GameStateSignals
    {
        public static Signal OnGameLoad = new Signal();
        public static Signal OnGamePlay = new Signal();
        public static Signal OnGameWin = new Signal();
        public static Signal OnGameFail = new Signal();
    }
}
