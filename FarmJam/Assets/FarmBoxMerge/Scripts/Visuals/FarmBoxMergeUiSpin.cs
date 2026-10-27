using UnityEngine;

/// <summary>Continuous rotation and optional pulse for decorative UI.</summary>
[DisallowMultipleComponent]
public sealed class FarmBoxMergeUiSpin : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 18f;
    [SerializeField] private float pulseAmount = 0.04f;
    [SerializeField] private float pulseSpeed = 2f;

    private Vector3 _baseScale = Vector3.one;

    private void Awake()
    {
        _baseScale = transform.localScale;
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, -degreesPerSecond * Time.unscaledDeltaTime);
        float pulse = 1f + pulseAmount * Mathf.Sin(Time.unscaledTime * pulseSpeed);
        transform.localScale = _baseScale * pulse;
    }
}
