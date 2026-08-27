using UnityEngine;
using UnityEngine.UI;
using VContainer;

[DisallowMultipleComponent]
public sealed class FarmBoxMergeSettingsPanelController : MonoBehaviour, IFarmBoxMergeSettingsPanel
{
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private FarmBoxMergeToggleView soundToggle;
    [SerializeField] private FarmBoxMergeToggleView hapticToggle;

    private IFarmBoxMergeSettingsService _settings;
    private IFarmBoxMergeOutcomeMonitor _outcomeMonitor;
    private bool _initialized;

    [Inject]
    public void Construct(
        IFarmBoxMergeSettingsService settings,
        IFarmBoxMergeOutcomeMonitor outcomeMonitor)
    {
        _settings = settings;
        _outcomeMonitor = outcomeMonitor;
    }

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        settingsPanel ??= gameObject;

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(Open);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(Close);
        }

        if (soundToggle != null)
        {
            soundToggle.ValueChanged += HandleSoundChanged;
        }

        if (hapticToggle != null)
        {
            hapticToggle.ValueChanged += HandleHapticChanged;
        }

        if (_settings != null)
        {
            _settings.Changed += SyncToggleState;
        }

        if (_outcomeMonitor != null)
        {
            _outcomeMonitor.OutcomeShown += Close;
        }

        SyncToggleState();
        Close();
    }

    public void Open()
    {
        SyncToggleState();
        settingsPanel?.SetActive(true);
    }

    public void Close()
    {
        settingsPanel?.SetActive(false);
    }

    private void OnDestroy()
    {
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(Open);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(Close);
        }

        if (soundToggle != null)
        {
            soundToggle.ValueChanged -= HandleSoundChanged;
        }

        if (hapticToggle != null)
        {
            hapticToggle.ValueChanged -= HandleHapticChanged;
        }

        if (_settings != null)
        {
            _settings.Changed -= SyncToggleState;
        }

        if (_outcomeMonitor != null)
        {
            _outcomeMonitor.OutcomeShown -= Close;
        }
    }

    private void HandleSoundChanged(bool enabled)
    {
        _settings?.SetAudioEnabled(enabled);
    }

    private void HandleHapticChanged(bool enabled)
    {
        _settings?.SetHapticsEnabled(enabled);
    }

    private void SyncToggleState()
    {
        if (_settings == null)
        {
            return;
        }

        soundToggle?.Initialize(_settings.SoundEnabled && _settings.MusicEnabled);
        hapticToggle?.Initialize(_settings.HapticsEnabled);
    }
}
