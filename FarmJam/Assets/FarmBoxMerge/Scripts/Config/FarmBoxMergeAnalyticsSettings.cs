using UnityEngine;

[CreateAssetMenu(fileName = "FarmBoxMergeAnalyticsSettings", menuName = "FarmBoxMerge/Analytics Settings")]
public sealed class FarmBoxMergeAnalyticsSettings : ScriptableObject
{
    [field: SerializeField] public bool AnalyticsEnabled { get; private set; } = true;
    [field: SerializeField] public bool LogEventsInEditor { get; private set; }
    [field: SerializeField, Range(0, 128)] public int MaxPendingEvents { get; private set; } = 32;
}
