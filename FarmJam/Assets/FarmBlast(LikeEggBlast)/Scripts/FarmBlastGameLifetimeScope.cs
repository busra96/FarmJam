using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FarmBlast
{
    public class FarmBlastGameLifetimeScope : LifetimeScope
    {
        [SerializeField] private InputManager _inputManager;
        [SerializeField] private GridTileManager _gridTileManager;
    
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_inputManager);
            builder.RegisterComponent(_gridTileManager);
            
            builder.Register<GridTileFactory>(Lifetime.Singleton);
            
            builder.RegisterEntryPoint<GameEntry>().As<IStartable, IDisposable>();
        }
    }
}


