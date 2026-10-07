using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

public interface IFarmBoxMergeLocalizationService
{
    FarmBoxMergeLanguage Language { get; }
    TMP_FontAsset Font { get; }
    event Action Changed;
    string Get(string key);
    string Format(string key, params object[] arguments);
    void SetLanguage(FarmBoxMergeLanguage language);
}

public sealed class FarmBoxMergeLocalizationService : IFarmBoxMergeLocalizationService
{
    private readonly FarmBoxMergeLocalizationCatalog _catalog;
    private readonly string _preferenceKey;
    private readonly Dictionary<string, FarmBoxMergeLocalizationCatalog.Translation> _translations = new();
    public FarmBoxMergeLanguage Language { get; private set; }
    public TMP_FontAsset Font => _catalog.GetFont(Language);
    public event Action Changed;

    public FarmBoxMergeLocalizationService(FarmBoxMergeLocalizationCatalog catalog, FarmBoxMergeSettings settings)
    {
        _catalog = catalog;
        string prefix = string.IsNullOrWhiteSpace(settings.PlayerPrefsPrefix) ? "FarmBoxMerge.Settings" : settings.PlayerPrefsPrefix.Trim();
        _preferenceKey = prefix + ".Language";
        int saved = PlayerPrefs.GetInt(_preferenceKey, (int)FarmBoxMergeLanguage.English);
        Language = Enum.IsDefined(typeof(FarmBoxMergeLanguage), saved) ? (FarmBoxMergeLanguage)saved : FarmBoxMergeLanguage.English;
        foreach (var entry in catalog.translations)
            if (entry != null && !string.IsNullOrWhiteSpace(entry.key)) _translations[entry.key] = entry;
    }

    public string Get(string key)
    {
        if (!_translations.TryGetValue(key, out var entry)) return key;
        string value = entry.Get(Language);
        return string.IsNullOrWhiteSpace(value) ? entry.english : value;
    }

    public string Format(string key, params object[] arguments)
    {
        try { return string.Format(CultureInfo.InvariantCulture, Get(key), arguments); }
        catch (FormatException)
        {
            Debug.LogWarning("Invalid localization placeholders for key: " + key);
            if (!_translations.TryGetValue(key, out var entry)) return key;
            try { return string.Format(CultureInfo.InvariantCulture, entry.english, arguments); }
            catch (FormatException) { return entry.english; }
        }
    }

    public void SetLanguage(FarmBoxMergeLanguage language)
    {
        if (!Enum.IsDefined(typeof(FarmBoxMergeLanguage), language) || Language == language) return;
        Language = language;
        PlayerPrefs.SetInt(_preferenceKey, (int)language);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
