using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private GridTileManager _gridTileManager;
    
    protected override void Configure(IContainerBuilder builder)
    {
         builder.RegisterComponent(_gridTileManager);
        
         builder.Register<InputManager>(Lifetime.Singleton);
         builder.Register<GridTileFactory>(Lifetime.Singleton);
        
         builder.RegisterEntryPoint<GameEntry>().As<IStartable, IDisposable>();
    }
}