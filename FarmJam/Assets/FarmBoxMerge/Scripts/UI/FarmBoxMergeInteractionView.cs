using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshProUGUI))]
public sealed class FarmBoxMergeInteractionView : MonoBehaviour
{
    [SerializeField] private CardMergeBoard board;
    [SerializeField, Min(0.1f)] private float messageDuration = 2.5f;
    private TextMeshProUGUI _label;
    private Coroutine _restoreRoutine;
    private FarmBoxMergeCanvasLocalization _localization;

    private void Awake() => _label = GetComponent<TextMeshProUGUI>();

    private void OnEnable()
    {
        _label ??= GetComponent<TextMeshProUGUI>();
        _localization ??= GetComponentInParent<FarmBoxMergeCanvasLocalization>();
        if (_localization != null) _localization.Changed += RestoreLabel;
        board ??= GetComponentInParent<Canvas>()?.GetComponentInChildren<CardMergeBoard>(true);
        if (board != null) board.InteractionHintChanged += ShowMessage;
        RestoreLabel();
    }

    private void OnDisable()
    {
        if (_localization != null) _localization.Changed -= RestoreLabel;
        if (board != null) board.InteractionHintChanged -= ShowMessage;
        if (_restoreRoutine != null) StopCoroutine(_restoreRoutine);
        _restoreRoutine = null;
    }

    private void ShowMessage(string message)
    {
        if (_restoreRoutine != null) StopCoroutine(_restoreRoutine);
        _label.text = (_localization?.Service?.Get("merge_title") ?? "MERGE CARDS") + "\n<size=58%>" + message + "</size>";
        _restoreRoutine = StartCoroutine(RestoreAfterDelay());
    }

    private IEnumerator RestoreAfterDelay()
    {
        yield return new WaitForSecondsRealtime(messageDuration);
        RestoreLabel();
        _restoreRoutine = null;
    }

    private void RestoreLabel()
    {
        _label.text = _localization?.Service?.Get("merge_rules") ?? "MERGE CARDS\n<size=58%>1 + 1 = 2   •   2 + 2 = 3   •   3 + 3 = 4</size>";
    }
}
