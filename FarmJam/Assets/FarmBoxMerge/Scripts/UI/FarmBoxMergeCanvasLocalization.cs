using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VContainer;

[DisallowMultipleComponent]
public sealed class FarmBoxMergeCanvasLocalization : MonoBehaviour
{
    [Serializable]
    public sealed class TextBinding { public TMP_Text text; public string key; }
    [SerializeField] private TextBinding[] bindings = Array.Empty<TextBinding>();
    private readonly Dictionary<TMP_Text, (TMP_FontAsset font, Material material)> _originalFonts = new();
    public IFarmBoxMergeLocalizationService Service { get; private set; }
    public event Action Changed;

    [Inject]
    public void Initialize(IFarmBoxMergeLocalizationService service)
    {
        if (Service == service) return;
        if (Service != null) Service.Changed -= Refresh;
        Service = service;
        Service.Changed += Refresh;
        Refresh();
    }

    public void RegisterText(TMP_Text text, TMP_Text source = null)
    {
        if (text == null) return;
        if (!_originalFonts.ContainsKey(text))
        {
            var originalState = source != null && _originalFonts.TryGetValue(source, out var state)
                ? state : (text.font, text.fontSharedMaterial);
            _originalFonts.Add(text, originalState);
        }
        if (Service == null) return;
        FarmBoxMergeRewardedAdBadge.ApplyLocalization(text, Service);
        var original = _originalFonts[text];
        TMP_FontAsset font = Service.Font != null ? Service.Font : original.font;
        if (text.font == font) return;
        text.font = font;
        text.fontSharedMaterial = Service.Font != null ? font.material : original.material;
    }

    public void UnregisterText(TMP_Text text)
    {
        if (text != null) _originalFonts.Remove(text);
    }

    public void Refresh()
    {
        // Scan only at initialization/language changes; includes inactive outcome/settings text.
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true)) RegisterText(text);
        var destroyed = new List<TMP_Text>();
        foreach (var entry in _originalFonts) if (entry.Key == null) destroyed.Add(entry.Key);
        foreach (TMP_Text text in destroyed) _originalFonts.Remove(text);
        foreach (TextBinding binding in bindings)
            if (binding.text != null) binding.text.text = Service.Get(binding.key);
        Changed?.Invoke();
    }

    private void OnDestroy()
    {
        if (Service != null) Service.Changed -= Refresh;
        _originalFonts.Clear();
    }
}
