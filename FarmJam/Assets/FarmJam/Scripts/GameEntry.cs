using System;
using VContainer;
using VContainer.Unity;

public class GameEntry : IStartable,IDisposable
{
    [Inject] private InputManager _inputManager;
    [Inject] private SelectManager _selectManager;
    [Inject] private GridTileManager _gridTileManager;
  
    
    public void Start()
    {
        _inputManager.Init();
        _selectManager.Init();
        _gridTileManager.Init();
    }

    public void Dispose()
    {
        _inputManager.DisableInput();
        _selectManager.Disable();
        _gridTileManager.Disable();
    }
}
