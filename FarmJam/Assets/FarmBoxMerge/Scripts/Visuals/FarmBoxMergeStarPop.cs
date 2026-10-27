using UnityEngine;

/// <summary>Staggered pop-in for the win stars whenever the panel opens.</summary>
[DisallowMultipleComponent]
public sealed class FarmBoxMergeStarPop : MonoBehaviour
{
    [SerializeField] private float delay = 0.15f;
    [SerializeField] private float duration = 0.38f;
    [SerializeField] private float overshoot = 1.25f;

    private float _start;
    private Vector3 _baseScale = Vector3.one;
    private bool _captured;

    private void OnEnable()
    {
        if (!_captured)
        {
            _baseScale = transform.localScale;
            _captured = true;
        }

        _start = Time.unscaledTime + delay;
        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        float t = (Time.unscaledTime - _start) / Mathf.Max(0.01f, duration);
        if (t < 0f)
        {
            transform.localScale = Vector3.zero;
            return;
        }

        if (t >= 1f)
        {
            transform.localScale = _baseScale * (1f + 0.03f * Mathf.Sin(Time.unscaledTime * 3f + delay * 10f));
            return;
        }

        float s = t < 0.6f
            ? Mathf.Lerp(0f, overshoot, FarmBoxMergeMath.EaseOutCubic(t / 0.6f))
            : Mathf.Lerp(overshoot, 1f, FarmBoxMergeMath.SmoothStep((t - 0.6f) / 0.4f));
        transform.localScale = _baseScale * s;
        transform.localRotation = Quaternion.Euler(0f, 0f, (1f - t) * 40f);
    }
}
