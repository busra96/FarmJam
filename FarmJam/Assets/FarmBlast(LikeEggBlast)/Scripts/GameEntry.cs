using System;
using System.Runtime.InteropServices;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FarmBlast
{
    public class GameEntry : IStartable,IDisposable
    {
        [Inject] private InputManager _inputManager;
        [Inject] private SelectManager _selectManager;
        [Inject] private GridTileManager _gridTileManager;
        [Inject] private UnitBoxManager _unitBoxManager;
        [Inject] private CollectableBoxManager _collectableBoxManager;
        [Inject] private CollectableSpawnManager _collectableSpawnManager;
    
        public void Start()
        {
            _inputManager.Init();
            _collectableBoxManager.Init();
            _gridTileManager.Init();
            _selectManager.Init();
            _collectableSpawnManager.Init();
        }

        public void Dispose()
        {
            _collectableBoxManager.Disable();
            _inputManager.DisableInput();
            _gridTileManager.Disable();
            _selectManager.Disable();
            _collectableSpawnManager.Disable();
        }
    }
}
