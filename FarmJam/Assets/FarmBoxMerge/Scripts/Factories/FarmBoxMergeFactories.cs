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

public sealed class FarmBoxMergeCardFactory : ICardFactory
{
    private readonly IObjectResolver _resolver;
    private readonly FarmBoxMergePrefabCatalog _catalog;

    public FarmBoxMergeCardFactory(IObjectResolver resolver, FarmBoxMergePrefabCatalog catalog)
    {
        _resolver = resolver;
        _catalog = catalog;
    }

    public Card Create(Transform parent)
    {
        return _catalog.Card != null ? _resolver.Instantiate(_catalog.Card, parent, false) : null;
    }
}

public sealed class FarmBoxMergeBoxFactory : IBoxFactory
{
    private readonly IObjectResolver _resolver;
    private readonly FarmBoxMergePrefabCatalog _catalog;

    public FarmBoxMergeBoxFactory(IObjectResolver resolver, FarmBoxMergePrefabCatalog catalog)
    {
        _resolver = resolver;
        _catalog = catalog;
    }

    public Box Create(Transform parent)
    {
        return _catalog.Box != null ? _resolver.Instantiate(_catalog.Box, parent, false) : null;
    }
}

public sealed class FarmBoxMergeItemFactory : IMergeItemFactory
{
    private readonly IObjectResolver _resolver;
    private readonly FarmBoxMergePrefabCatalog _catalog;

    public FarmBoxMergeItemFactory(IObjectResolver resolver, FarmBoxMergePrefabCatalog catalog)
    {
        _resolver = resolver;
        _catalog = catalog;
    }

    public MergeItem Create(Transform parent)
    {
        return _catalog.MergeItem != null ? _resolver.Instantiate(_catalog.MergeItem, parent, false) : null;
    }
}
