using System;
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
    
        public void Start()
        {
            _inputManager.Init();
            _gridTileManager.Init();
            _selectManager.Init();
        }

        public void Dispose()
        {
            _inputManager.DisableInput();
            _gridTileManager.Disable();
            _selectManager.Disable();
        }
    }
}
