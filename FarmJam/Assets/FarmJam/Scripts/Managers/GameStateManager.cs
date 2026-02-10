using Signals;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
   public GameStateType GameStateType;

   public void Init()
   { 
      GameStateSignals.OnGameLoad.AddListener(OnGameLoad);
      GameStateSignals.OnGamePlay.AddListener(OnGamePlay);
      GameStateSignals.OnGameWin.AddListener(OnGameWin);
      GameStateSignals.OnGameFail.AddListener(OnGameFail);
   }

   public void Disable()
   {
      GameStateSignals.OnGameLoad.RemoveListener(OnGameLoad);
      GameStateSignals.OnGamePlay.RemoveListener(OnGamePlay);
      GameStateSignals.OnGameWin.RemoveListener(OnGameWin);
      GameStateSignals.OnGameFail.RemoveListener(OnGameFail);
   }

   private void OnGameLoad()
   {
      if(GameStateType == GameStateType.Load) return;
      GameStateType = GameStateType.Load;
   }

   private void OnGamePlay()
   {
      if(GameStateType == GameStateType.Gameplay) return;
      GameStateType = GameStateType.Gameplay;
      UISignals.OnGameplayPanelActive?.Dispatch();
   }

   private void OnGameWin()
   {
      if(GameStateType == GameStateType.Win || GameStateType == GameStateType.Fail) return;
      GameStateType = GameStateType.Win;
      AudioSignals.OnGameWinSoundPlay?.Dispatch();
      UISignals.OnWinPanelActive?.Dispatch();
   }

   private void OnGameFail()
   {
      if(GameStateType == GameStateType.Fail || GameStateType == GameStateType.Win) return;
      GameStateType = GameStateType.Fail;
      AudioSignals.OnGameFailSoundPlay?.Dispatch();
      UISignals.OnFailPanelActive?.Dispatch();
   }
}