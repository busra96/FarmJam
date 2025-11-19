using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private GridTileManager _gridTileManager;
    [SerializeField] private UnitBoxManager _unitBoxManager;
    [SerializeField] private EmptyBoxSpawner _emptyBoxSpawner;
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private LevelManager _levelManager;

    [SerializeField] private EmptyBoxContainer _emptyBoxContainer;
    
    protected override void Configure(IContainerBuilder builder)
    {
     
         builder.RegisterComponent(_gridTileManager);
         builder.RegisterComponent(_unitBoxManager);
         builder.RegisterComponent(_emptyBoxSpawner);
         builder.RegisterComponent(_uiManager);
         builder.RegisterComponent(_levelManager);
         
         builder.RegisterComponent(_emptyBoxContainer);
        
         builder.Register<CollectableBoxParentFactory>(Lifetime.Singleton);
         builder.Register<CollectableBoxManager>(Lifetime.Singleton);
         builder.Register<InputManager>(Lifetime.Singleton);
         builder.Register<GridTileFactory>(Lifetime.Singleton);
         builder.Register<SelectManager>(Lifetime.Singleton);
         builder.Register<GameStateManager>(Lifetime.Singleton);
        
         builder.RegisterEntryPoint<GameEntry>().As<IStartable, IDisposable>();
    }
}