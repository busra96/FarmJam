using System;
using VContainer;
using VContainer.Unity;

public class GameEntry : IStartable,IDisposable
{
    [Inject] private InputManager _inputManager;
    [Inject] private GridTileManager _gridTileManager;
    
    public void Start()
    {
        _inputManager.Init();
        _gridTileManager.Init();
    }

    public void Dispose()
    {
        _inputManager.DisableInput();
        _gridTileManager.Disable();
    }
}
