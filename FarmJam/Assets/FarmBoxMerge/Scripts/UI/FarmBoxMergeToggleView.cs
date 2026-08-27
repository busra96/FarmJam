using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class FarmBoxMergeToggleView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private Color enabledColor = new Color(0.39f, 0.72f, 0.31f, 1f);
    [SerializeField] private Color disabledColor = new Color(0.335f, 0.335f, 0.335f, 1f);
    [SerializeField, Min(0f)] private float handleOffset = 35f;

    private bool _isOn;
    private bool _initialized;

    public bool IsOn => _isOn;
    public event Action<bool> ValueChanged;

    public void Initialize(bool initialValue)
    {
        ResolveReferences();
        if (!_initialized && button != null)
        {
            button.onClick.AddListener(HandleClicked);
        }

        _initialized = true;
        SetIsOn(initialValue, notify: false);
    }

    public void SetIsOn(bool value, bool notify = false)
    {
        bool changed = _isOn != value;
        _isOn = value;
        RefreshVisual();

        if (changed && notify)
        {
            ValueChanged?.Invoke(value);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
        }
    }

    private void OnValidate()
    {
        ResolveReferences();
        RefreshVisual();
    }

    private void HandleClicked()
    {
        SetIsOn(!_isOn, notify: true);
    }

    private void ResolveReferences()
    {
        button ??= GetComponent<Button>();
        background ??= GetComponent<Image>();

        if (handle == null && transform.childCount > 0)
        {
            handle = transform.GetChild(0) as RectTransform;
        }
    }

    private void RefreshVisual()
    {
        if (background != null)
        {
            background.color = _isOn ? enabledColor : disabledColor;
        }

        if (handle != null)
        {
            Vector2 position = handle.anchoredPosition;
            position.x = (_isOn ? 1f : -1f) * Mathf.Abs(handleOffset);
            handle.anchoredPosition = position;
        }
    }
}
