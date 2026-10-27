using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Places a rounded pill behind a TMP label (for example the level title) and
/// keeps it aligned when adaptive/safe-area layout moves the label.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class FarmBoxMergeLabelBadge : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Vector2 padding = new Vector2(64f, 22f);
    [SerializeField] private float minWidth = 260f;

    private RectTransform _rect;

    public void Configure(TextMeshProUGUI target)
    {
        label = target;
    }

    private void Awake()
    {
        _rect = (RectTransform)transform;
    }

    private void LateUpdate()
    {
        if (label == null)
        {
            return;
        }

        RectTransform source = label.rectTransform;
        _rect.anchorMin = source.anchorMin;
        _rect.anchorMax = source.anchorMax;
        _rect.pivot = source.pivot;
        _rect.anchoredPosition = source.anchoredPosition;
        Vector2 textSize = label.GetRenderedValues(false);
        if (textSize.x <= 0f || float.IsNaN(textSize.x))
        {
            textSize = new Vector2(source.rect.width * 0.5f, label.fontSize);
        }

        float width = Mathf.Max(minWidth, textSize.x + padding.x);
        float height = Mathf.Max(label.fontSize, textSize.y) + padding.y;
        Vector2 anchorSpan = Vector2.Scale(source.anchorMax - source.anchorMin,
            _rect.parent is RectTransform parent ? parent.rect.size : Vector2.zero);
        _rect.sizeDelta = new Vector2(width - anchorSpan.x, height - anchorSpan.y);
        if (_rect.GetSiblingIndex() > source.GetSiblingIndex())
        {
            _rect.SetSiblingIndex(source.GetSiblingIndex());
        }
    }
}
