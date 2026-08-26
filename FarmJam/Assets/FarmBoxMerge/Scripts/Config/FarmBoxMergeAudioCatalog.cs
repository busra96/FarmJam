using UnityEngine;

[CreateAssetMenu(fileName = "FarmBoxMergeAudioCatalog", menuName = "FarmBoxMerge/Audio Catalog")]
public sealed class FarmBoxMergeAudioCatalog : ScriptableObject
{
    [field: SerializeField] public AudioClip Button { get; private set; }
    [field: SerializeField] public AudioClip Merge { get; private set; }
    [field: SerializeField] public AudioClip Spawn { get; private set; }
    [field: SerializeField] public AudioClip ItemLand { get; private set; }
    [field: SerializeField] public AudioClip Trash { get; private set; }
    [field: SerializeField] public AudioClip BoxClear { get; private set; }
    [field: SerializeField] public AudioClip Win { get; private set; }
    [field: SerializeField] public AudioClip Confetti { get; private set; }
    [field: SerializeField] public AudioClip Fail { get; private set; }
    [field: SerializeField] public AudioClip GameplayMusic { get; private set; }
}
