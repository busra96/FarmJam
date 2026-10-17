using UnityEngine;
using VContainer;
using VContainer.Unity;

public interface ICardFactory
{
    Card Create(Transform parent);
}

public interface IBoxFactory
{
    Box Create(Transform parent);
}

public interface IMergeItemFactory
{
    MergeItem Create(Transform parent);
}

public abstract class FarmBoxMergePrefabFactory<T> where T : Component
{
    private readonly IObjectResolver _resolver;
    private readonly T _prefab;

    protected FarmBoxMergePrefabFactory(IObjectResolver resolver, T prefab)
    {
        _resolver = resolver;
        _prefab = prefab;
    }

    protected T CreateInstance(Transform parent)
    {
        return _prefab != null ? _resolver.Instantiate(_prefab, parent, false) : null;
    }
}

public sealed class FarmBoxMergeCardFactory : FarmBoxMergePrefabFactory<Card>, ICardFactory
{
    public FarmBoxMergeCardFactory(IObjectResolver resolver, FarmBoxMergePrefabCatalog catalog)
        : base(resolver, catalog.Card)
    {
    }

    public Card Create(Transform parent) => CreateInstance(parent);
}

public sealed class FarmBoxMergeBoxFactory : FarmBoxMergePrefabFactory<Box>, IBoxFactory
{
    public FarmBoxMergeBoxFactory(IObjectResolver resolver, FarmBoxMergePrefabCatalog catalog)
        : base(resolver, catalog.Box)
    {
    }

    public Box Create(Transform parent) => CreateInstance(parent);
}

public sealed class FarmBoxMergeItemFactory : FarmBoxMergePrefabFactory<MergeItem>, IMergeItemFactory
{
    public FarmBoxMergeItemFactory(IObjectResolver resolver, FarmBoxMergePrefabCatalog catalog)
        : base(resolver, catalog.MergeItem)
    {
    }

    public MergeItem Create(Transform parent) => CreateInstance(parent);
}
