using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Purely cosmetic card dressing: glossy highlight, value pips and a pulsing
/// glow on max-value cards. It never touches the "Background Color" image
/// colour or the counter text, because <see cref="Card"/> derives its colour and
/// value from those views. All generated graphics ignore raycasts.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Card))]
public sealed class FarmBoxMergeCardDecor : MonoBehaviour
{
    [SerializeField] private Sprite glossSprite;
    [SerializeField] private Sprite pipSprite;
    [SerializeField] private Sprite glowSprite;
    [SerializeField] private Color maxGlowColor = new Color(1f, 0.86f, 0.32f, 0.9f);
    [SerializeField, Range(0f, 1f)] private float glossAlpha = 0.32f;
    [SerializeField, Range(0f, 1f)] private float emptyPipAlpha = 0.28f;
    [SerializeField, Min(4f)] private float pipSize = 19f;

    private const int PipCount = FarmBoxMergeRules.MaxCardCounter;

    private Card _card;
    private Image _gloss;
    private Image _shade;
    private Image _glow;
    private readonly Image[] _pips = new Image[PipCount];
    private int _shownValue = -1;
    private bool _built;

    public void Configure(Sprite gloss, Sprite pip, Sprite glow)
    {
        glossSprite = gloss;
        pipSprite = pip;
        glowSprite = glow;
    }

    private void Awake()
    {
        _card = GetComponent<Card>();
        Build();
    }

    private void Build()
    {
        if (_built || _card == null)
        {
            return;
        }

        Image background = _card.BackgroundColorImg;
        if (background == null)
        {
            return;
        }

        _built = true;
        RectTransform backgroundRect = background.rectTransform;
        RectTransform panel = backgroundRect.parent as RectTransform;

        _glow = CreateImage("FBM_MaxGlow", transform as RectTransform, glowSprite, maxGlowColor);
        Stretch(_glow.rectTransform, -0.12f, -0.14f);
        _glow.rectTransform.SetAsFirstSibling();
        _glow.enabled = false;

        _shade = CreateImage("FBM_Shade", backgroundRect, background.sprite, new Color(0f, 0f, 0f, 0.14f));
        _shade.type = background.type;
        SetAnchors(_shade.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.30f));

        _gloss = CreateImage("FBM_Gloss", backgroundRect, glossSprite != null ? glossSprite : background.sprite,
            new Color(1f, 1f, 1f, glossAlpha));
        SetAnchors(_gloss.rectTransform, new Vector2(0.03f, 0.48f), new Vector2(0.97f, 0.97f));

        RectTransform pipRow = new GameObject("FBM_Pips", typeof(RectTransform)).GetComponent<RectTransform>();
        pipRow.SetParent(panel != null ? panel : backgroundRect, false);
        // A small vertical "level meter" on the right edge, clear of the crate icon.
        SetAnchors(pipRow, new Vector2(0.80f, 0.40f), new Vector2(0.95f, 0.90f));
        float spacing = pipSize * 1.35f;
        float start = -spacing * (PipCount - 1) * 0.5f;
        for (int i = 0; i < PipCount; i++)
        {
            Image pip = CreateImage("Pip" + (i + 1), pipRow, pipSprite, Color.white);
            RectTransform rect = pip.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(pipSize, pipSize);
            rect.anchoredPosition = new Vector2(0f, start + i * spacing);
            _pips[i] = pip;
        }

        Refresh(force: true);
    }

    private void LateUpdate()
    {
        Refresh(force: false);
        if (_glow != null && _glow.enabled)
        {
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 3.2f);
            Color color = maxGlowColor;
            color.a *= pulse;
            _glow.color = color;
        }
    }

    private void Refresh(bool force)
    {
        if (!_built)
        {
            Build();
            if (!_built)
            {
                return;
            }
        }

        int value = _card.CounterValue;
        if (!force && value == _shownValue)
        {
            return;
        }

        _shownValue = value;
        for (int i = 0; i < PipCount; i++)
        {
            if (_pips[i] == null)
            {
                continue;
            }

            Color color = Color.white;
            color.a = i < value ? 1f : emptyPipAlpha;
            _pips[i].color = color;
            _pips[i].rectTransform.localScale = Vector3.one * (i < value ? 1f : 0.78f);
        }

        if (_glow != null)
        {
            _glow.enabled = value >= FarmBoxMergeRules.MaxCardCounter;
        }
    }

    private static Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = parent != null ? parent.gameObject.layer : go.layer;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        image.maskable = true;
        return image;
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void Stretch(RectTransform rect, float padX, float padY)
    {
        SetAnchors(rect, new Vector2(padX, padY), new Vector2(1f - padX, 1f - padY));
    }
}
