using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameEntry : IStartable,IDisposable
{
    [Inject] private InputManager _inputManager;
    [Inject] private SelectManager _selectManager;
    [Inject] private GridTileManager _gridTileManager;
    [Inject] private CollectableBoxManager _collectableBoxManager;
    [Inject] private GameStateManager _gameStateManager;
    [Inject] private EmptyBoxSpawner _emptyBoxSpawner;
    [Inject] private UIManager _uiManager;
    [Inject] private LevelManager _levelManager;
  
    
    public void Start()
    {
        _inputManager.Init();
        _selectManager.Init();
        _gridTileManager.Init();
        _collectableBoxManager.Init();
        _uiManager.Init();
        _gameStateManager.Init();
        _emptyBoxSpawner.Init();
        _levelManager.Init();

    }

    public void Dispose()
    {
        _inputManager.DisableInput();
        _selectManager.Disable();
        _gridTileManager.Disable();
        _collectableBoxManager.Disable();
        _uiManager.Disable();
        _gameStateManager.Disable();
        _levelManager.Disable();
    }
}