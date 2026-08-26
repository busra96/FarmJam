using UnityEngine;

[CreateAssetMenu(fileName = "FarmBoxMergePrefabCatalog", menuName = "FarmBoxMerge/Prefab Catalog")]
public sealed class FarmBoxMergePrefabCatalog : ScriptableObject
{
    [field: SerializeField] public Card Card { get; private set; }
    [field: SerializeField] public Box Box { get; private set; }
    [field: SerializeField] public MergeItem MergeItem { get; private set; }
}
