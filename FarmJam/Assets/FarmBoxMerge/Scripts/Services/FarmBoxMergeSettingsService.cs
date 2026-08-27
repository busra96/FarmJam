using System;
using UnityEngine;

public interface IFarmBoxMergeSettingsService
{
    bool SoundEnabled { get; }
    bool MusicEnabled { get; }
    bool ParticlesEnabled { get; }
    bool HapticsEnabled { get; }
    bool CameraFeedbackEnabled { get; }
    float SfxVolume { get; }
    float MusicVolume { get; }
    int AddCardUses { get; }
    int TrashUses { get; }
    event Action Changed;
    void SetAudioEnabled(bool value);
    void SetSoundEnabled(bool value);
    void SetMusicEnabled(bool value);
    void SetParticlesEnabled(bool value);
    void SetHapticsEnabled(bool value);
    void SetCameraFeedbackEnabled(bool value);
    void SetSfxVolume(float value);
    void SetMusicVolume(float value);
}

public sealed class FarmBoxMergeSettingsService : IFarmBoxMergeSettingsService
{
    private readonly FarmBoxMergeSettings _defaults;
    private readonly string _prefix;

    private bool _soundEnabled;
    private bool _musicEnabled;
    private bool _particlesEnabled;
    private bool _hapticsEnabled;
    private bool _cameraFeedbackEnabled;

    public bool SoundEnabled => _soundEnabled;
    public bool MusicEnabled => _musicEnabled;
    public bool ParticlesEnabled => _particlesEnabled;
    public bool HapticsEnabled => _hapticsEnabled;
    public bool CameraFeedbackEnabled => _cameraFeedbackEnabled;
    public float SfxVolume { get; private set; }
    public float MusicVolume { get; private set; }
    public int AddCardUses => _defaults.AddCardUses;
    public int TrashUses => _defaults.TrashUses;
    public event Action Changed;

    public FarmBoxMergeSettingsService(FarmBoxMergeSettings defaults)
    {
        _defaults = defaults;
        _prefix = string.IsNullOrWhiteSpace(defaults.PlayerPrefsPrefix)
            ? "FarmBoxMerge.Settings"
            : defaults.PlayerPrefsPrefix.Trim();

        _soundEnabled = LoadBool(nameof(SoundEnabled), defaults.SoundEnabled);
        _musicEnabled = LoadBool(nameof(MusicEnabled), defaults.MusicEnabled);
        _particlesEnabled = LoadBool(nameof(ParticlesEnabled), defaults.ParticlesEnabled);
        _hapticsEnabled = LoadBool(nameof(HapticsEnabled), defaults.HapticsEnabled);
        _cameraFeedbackEnabled = LoadBool(nameof(CameraFeedbackEnabled), defaults.CameraFeedbackEnabled);
        SfxVolume = LoadFloat(nameof(SfxVolume), defaults.SfxVolume);
        MusicVolume = LoadFloat(nameof(MusicVolume), defaults.MusicVolume);
    }

    public void SetSoundEnabled(bool value) => SetBool(nameof(SoundEnabled), value, ref _soundEnabled);
    public void SetMusicEnabled(bool value) => SetBool(nameof(MusicEnabled), value, ref _musicEnabled);
    public void SetParticlesEnabled(bool value) => SetBool(nameof(ParticlesEnabled), value, ref _particlesEnabled);
    public void SetHapticsEnabled(bool value) => SetBool(nameof(HapticsEnabled), value, ref _hapticsEnabled);
    public void SetCameraFeedbackEnabled(bool value) => SetBool(nameof(CameraFeedbackEnabled), value, ref _cameraFeedbackEnabled);

    public void SetAudioEnabled(bool value)
    {
        if (_soundEnabled == value && _musicEnabled == value)
        {
            return;
        }

        _soundEnabled = value;
        _musicEnabled = value;
        PlayerPrefs.SetInt(Key(nameof(SoundEnabled)), value ? 1 : 0);
        PlayerPrefs.SetInt(Key(nameof(MusicEnabled)), value ? 1 : 0);
        SaveAndNotify();
    }

    public void SetSfxVolume(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(SfxVolume, value)) return;
        SfxVolume = value;
        PlayerPrefs.SetFloat(Key(nameof(SfxVolume)), value);
        SaveAndNotify();
    }

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(MusicVolume, value)) return;
        MusicVolume = value;
        PlayerPrefs.SetFloat(Key(nameof(MusicVolume)), value);
        SaveAndNotify();
    }

    private void SetBool(string name, bool value, ref bool field)
    {
        if (field == value) return;
        field = value;
        PlayerPrefs.SetInt(Key(name), value ? 1 : 0);
        SaveAndNotify();
    }

    private bool LoadBool(string name, bool fallback)
    {
        return PlayerPrefs.GetInt(Key(name), fallback ? 1 : 0) != 0;
    }

    private float LoadFloat(string name, float fallback)
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(Key(name), fallback));
    }

    private string Key(string name) => $"{_prefix}.{name}";

    private void SaveAndNotify()
    {
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
