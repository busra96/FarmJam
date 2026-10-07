using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class FarmBoxMergeLanguageSelector : MonoBehaviour
{
    [Serializable]
    public sealed class Choice
    {
        public FarmBoxMergeLanguage language;
        public Button button;
        [NonSerialized] public UnityAction listener;
        [NonSerialized] public TMP_Text label;
    }
    [SerializeField] private Choice[] choices = Array.Empty<Choice>();
    [SerializeField] private Color selectedColor = new Color(0.96f, 0.76f, 0.29f);
    [SerializeField] private Color normalColor = new Color(0.24f, 0.57f, 0.31f);
    private IFarmBoxMergeLocalizationService _service;

    public void Initialize(IFarmBoxMergeLocalizationService service)
    {
        if (_service != null) return;
        _service = service;
        foreach (Choice choice in choices)
        {
            if (choice.button == null) continue;
            choice.listener = () => _service.SetLanguage(choice.language);
            choice.label = choice.button.GetComponentInChildren<TMP_Text>();
            choice.button.onClick.AddListener(choice.listener);
        }
        _service.Changed += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        foreach (Choice choice in choices)
        {
            if (choice.button == null) continue;
            bool selected = choice.language == _service.Language;
            choice.button.interactable = !selected;
            if (choice.button.targetGraphic != null) choice.button.targetGraphic.color = selected ? selectedColor : normalColor;
            if (choice.label != null) choice.label.color = selected ? new Color(0.25f, 0.21f, 0.16f) : Color.white;
        }
    }

    private void OnDestroy()
    {
        if (_service != null) _service.Changed -= Refresh;
        foreach (Choice choice in choices)
            if (choice.button != null && choice.listener != null) choice.button.onClick.RemoveListener(choice.listener);
    }
}
