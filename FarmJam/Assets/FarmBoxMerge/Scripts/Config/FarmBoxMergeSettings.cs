using UnityEngine;

[CreateAssetMenu(fileName = "FarmBoxMergeSettings", menuName = "FarmBoxMerge/Settings")]
public sealed class FarmBoxMergeSettings : ScriptableObject
{
    [Header("Feature Defaults")]
    [field: SerializeField] public bool SoundEnabled { get; private set; } = true;
    [field: SerializeField] public bool MusicEnabled { get; private set; } = true;
    [field: SerializeField] public bool ParticlesEnabled { get; private set; } = true;
    [field: SerializeField] public bool HapticsEnabled { get; private set; } = true;
    [field: SerializeField] public bool CameraFeedbackEnabled { get; private set; } = true;

    [Header("Audio Defaults")]
    [field: SerializeField, Range(0f, 1f)] public float SfxVolume { get; private set; } = 0.72f;
    [field: SerializeField, Range(0f, 1f)] public float MusicVolume { get; private set; } = 0.075f;

    [Header("Attempt Defaults")]
    [field: SerializeField, Min(0)] public int AddCardUses { get; private set; } = 3;
    [field: SerializeField, Min(0)] public int TrashUses { get; private set; } = 3;

    [Header("Persistence")]
    [field: SerializeField] public string PlayerPrefsPrefix { get; private set; } = "FarmBoxMerge.Settings";
}
