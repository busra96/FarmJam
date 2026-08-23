using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class FarmBoxMergeButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField, Range(0.8f, 1f)] private float pressedScale = 0.93f;
    [SerializeField, Min(0.01f)] private float releaseDuration = 0.14f;

    private Button _button;
    private RectTransform _rectTransform;
    private Vector3 _baseScale;
    private Coroutine _releaseRoutine;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _rectTransform = transform as RectTransform;
        _baseScale = transform.localScale;
    }

    private void OnDisable()
    {
        if (_releaseRoutine != null)
        {
            StopCoroutine(_releaseRoutine);
            _releaseRoutine = null;
        }

        transform.localScale = _baseScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_button == null || !_button.interactable)
        {
            return;
        }

        StopReleaseRoutine();
        transform.localScale = _baseScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_button != null && _button.interactable)
        {
            FarmBoxMergeFeedbackController.PlayButtonClick();
        }
    }

    private void Release()
    {
        if (!isActiveAndEnabled)
        {
            transform.localScale = _baseScale;
            return;
        }

        StopReleaseRoutine();
        _releaseRoutine = StartCoroutine(ReleaseRoutine());
    }

    private IEnumerator ReleaseRoutine()
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < releaseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = FarmBoxMergeMath.EaseOutCubic(Mathf.Clamp01(elapsed / releaseDuration));
            transform.localScale = Vector3.LerpUnclamped(startScale, _baseScale, progress);
            yield return null;
        }

        transform.localScale = _baseScale;
        _releaseRoutine = null;
    }

    private void StopReleaseRoutine()
    {
        if (_releaseRoutine == null)
        {
            return;
        }

        StopCoroutine(_releaseRoutine);
        _releaseRoutine = null;
    }
}
