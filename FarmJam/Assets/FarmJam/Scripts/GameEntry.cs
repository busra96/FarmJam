using System;
using VContainer;
using VContainer.Unity;

public class GameEntry : IStartable,IDisposable
{
    [Inject] private InputManager _inputManager;
    
    public void Start()
    {
        _inputManager.Init();
    }

    public void Dispose()
    {
        _inputManager.DisableInput();
    }
}
