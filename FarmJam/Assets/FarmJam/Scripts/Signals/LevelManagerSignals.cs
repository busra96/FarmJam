namespace Signals
{
    public class LevelManagerSignals 
    {
        public static Signal OnLoadCurrentLevel = new Signal();
        public static Signal OnLoadNextLevel = new Signal();

        public static Signal OnLevelWinFailCheckTimerRestart = new Signal();
    }
}