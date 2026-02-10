using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FailPanelController : MonoBehaviour
{
    private const float FADE_DURATION = 0.5f;
    private const float SCALE_DURATION = 0.3f;
    private const float OVERLAY_ALPHA = 0.7f;

    [Header("UI Elements")]
    public Image RedOverlay;
    public CanvasGroup PanelCanvasGroup;
    public Transform PanelTransform;

    [Header("Colors")]
    public Color RedColor = new Color(1f, 0.2f, 0.2f, 0.7f);

    private void Awake()
    {
        if (RedOverlay != null)
            RedOverlay.color = new Color(RedColor.r, RedColor.g, RedColor.b, 0f);

        if (PanelCanvasGroup != null)
            PanelCanvasGroup.alpha = 0f;

        if (PanelTransform != null)
            PanelTransform.localScale = Vector3.zero;
    }

    private void OnEnable()
    {
        PlayFailAnimation();
    }

    private void PlayFailAnimation()
    {
        Sequence failSequence = DOTween.Sequence();

        if (RedOverlay != null)
        {
            failSequence.Append(RedOverlay.DOColor(RedColor, FADE_DURATION).SetEase(Ease.OutQuad));
        }

        if (PanelCanvasGroup != null)
        {
            failSequence.Join(PanelCanvasGroup.DOFade(1f, FADE_DURATION).SetEase(Ease.OutQuad));
        }

        if (PanelTransform != null)
        {
            failSequence.Join(PanelTransform.DOScale(Vector3.one, SCALE_DURATION).SetEase(Ease.OutBack));
        }

        failSequence.Play();
    }

    private void OnDisable()
    {
        DOTween.Kill(RedOverlay);
        DOTween.Kill(PanelCanvasGroup);
        DOTween.Kill(PanelTransform);

        if (RedOverlay != null)
            RedOverlay.color = new Color(RedColor.r, RedColor.g, RedColor.b, 0f);

        if (PanelCanvasGroup != null)
            PanelCanvasGroup.alpha = 0f;

        if (PanelTransform != null)
            PanelTransform.localScale = Vector3.zero;
    }
}
