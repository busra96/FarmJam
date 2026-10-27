using TMPro;
using UnityEngine;

/// <summary>
/// Cosmetic helper for a world box slot: a floating round badge that repeats the
/// card value the silhouette accepts, plus an optional soft pad under the
/// silhouette. It only reads <see cref="FarmBoxMergeBoxSlotView"/> state and never
/// creates MergeBoxParent children, so slot occupancy rules are unchanged.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(FarmBoxMergeBoxSlotView))]
public sealed class FarmBoxMergeSlotBadge : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Sprite padSprite;
    [SerializeField] private Sprite discSprite;
    [SerializeField] private Material numberMaterial;
    [SerializeField] private bool showPad = true;
    [SerializeField] private Color padColor = new Color(1f, 0.95f, 0.70f, 0.55f);
    [SerializeField] private Color discColor = new Color(0.27f, 0.52f, 0.20f, 0.96f);
    [SerializeField] private Color discRimColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private Color numberColor = Color.white;
    [SerializeField] private float badgeHeight = 2.6f;
    [SerializeField] private float badgeForward = 0.4f;
    [SerializeField] private float discDiameter = 1.55f;
    [SerializeField] private float numberSize = 11f;
    [SerializeField] private float padPadding = 1.3f;

    private static Material _overlayMaterial;

    private FarmBoxMergeBoxSlotView _slot;
    private Transform _badgeRoot;
    private TextMeshPro _label;
    private SpriteRenderer _pad;
    private Camera _camera;
    private int _shownValue = -1;
    private float _boundsTimer;
    private Vector3 _center;
    private Vector2 _extent = Vector2.one;
    private float _phase;
    private float _popTime = -10f;

    public void Configure(TMP_FontAsset badgeFont, Sprite pad, Sprite disc, Material number)
    {
        font = badgeFont;
        padSprite = pad;
        discSprite = disc;
        numberMaterial = number;
    }

    private void Awake()
    {
        _slot = GetComponent<FarmBoxMergeBoxSlotView>();
        _phase = transform.GetSiblingIndex() * 0.9f;
        Build();
    }

    private void Build()
    {
        int layer = LayerMask.NameToLayer("Ignore Raycast");
        if (layer < 0) layer = 0;

        GameObject root = new GameObject("FBM_SlotBadge");
        root.layer = layer;
        _badgeRoot = root.transform;
        _badgeRoot.SetParent(transform, false);

        if (discSprite != null)
        {
            CreateDisc("Rim", discSprite, discRimColor, 1.14f, 0, layer);
            CreateDisc("Disc", discSprite, discColor, 1f, 1, layer);
        }

        GameObject labelObject = new GameObject("Value", typeof(RectTransform));
        labelObject.layer = layer;
        labelObject.transform.SetParent(_badgeRoot, false);
        labelObject.transform.localPosition = new Vector3(0f, -0.03f, -0.02f);
        _label = labelObject.AddComponent<TextMeshPro>();
        if (font != null)
        {
            _label.font = font;
            _label.fontSharedMaterial = numberMaterial != null ? numberMaterial : GetOverlayMaterial(font);
        }

        _label.fontSize = numberSize;
        _label.alignment = TextAlignmentOptions.Center;
        _label.fontStyle = FontStyles.Bold;
        _label.color = numberColor;
        _label.rectTransform.sizeDelta = new Vector2(2f, 2f);
        _label.raycastTarget = false;
        _label.sortingOrder = 3;

        if (showPad && padSprite != null)
        {
            GameObject padObject = new GameObject("FBM_SlotPad");
            padObject.layer = layer;
            padObject.transform.SetParent(transform, false);
            padObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _pad = padObject.AddComponent<SpriteRenderer>();
            _pad.sprite = padSprite;
            _pad.color = padColor;
            _pad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _pad.receiveShadows = false;
            _pad.sortingOrder = -10;
        }
    }

    private void CreateDisc(string name, Sprite sprite, Color color, float scale, int order, int layer)
    {
        GameObject disc = new GameObject(name);
        disc.layer = layer;
        disc.transform.SetParent(_badgeRoot, false);
        float size = sprite.bounds.size.x > 0f ? discDiameter / sprite.bounds.size.x : 1f;
        disc.transform.localScale = Vector3.one * size * scale;
        SpriteRenderer renderer = disc.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static Material GetOverlayMaterial(TMP_FontAsset font)
    {
        if (_overlayMaterial != null)
        {
            return _overlayMaterial;
        }

        Shader overlay = Shader.Find("TextMeshPro/Distance Field Overlay");
        if (overlay == null || font.material == null)
        {
            return font.material;
        }

        _overlayMaterial = new Material(font.material)
        {
            name = font.material.name + " (FBM Overlay)",
            shader = overlay,
            hideFlags = HideFlags.DontSave
        };
        _overlayMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.18f);
        _overlayMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.12f, 0.25f, 0.08f, 1f));
        _overlayMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
        return _overlayMaterial;
    }

    private void LateUpdate()
    {
        if (_slot == null)
        {
            return;
        }

        bool visible = _slot.HasRequirement && !_slot.IsOccupied;
        if (_badgeRoot.gameObject.activeSelf != visible)
        {
            _badgeRoot.gameObject.SetActive(visible);
        }

        if (_pad != null && _pad.gameObject.activeSelf != visible)
        {
            _pad.gameObject.SetActive(visible);
        }

        if (!visible)
        {
            _shownValue = -1;
            return;
        }

        if (_shownValue != _slot.AcceptedCardValue)
        {
            _shownValue = _slot.AcceptedCardValue;
            _label.text = _shownValue.ToString();
            _boundsTimer = 0f;
            _popTime = Time.unscaledTime;
        }

        _boundsTimer -= Time.unscaledDeltaTime;
        if (_boundsTimer <= 0f)
        {
            _boundsTimer = 0.25f;
            MeasureSilhouette();
        }

        float t = Time.unscaledTime * 2.2f + _phase;
        if (_pad != null)
        {
            _pad.transform.localPosition = new Vector3(_center.x, 0.08f, _center.z);
            float breathe = 1f + 0.05f * Mathf.Sin(t);
            _pad.transform.localScale = new Vector3(_extent.x * breathe, _extent.y * breathe, 1f);
            Color c = padColor;
            c.a *= 0.75f + 0.25f * Mathf.Sin(t);
            _pad.color = c;
        }

        _camera = _camera != null ? _camera : Camera.main;
        Vector3 local = new Vector3(_center.x, badgeHeight + 0.12f * Mathf.Sin(t * 1.3f), _center.z);
        _badgeRoot.localPosition = local;
        if (_camera != null)
        {
            _badgeRoot.rotation = _camera.transform.rotation;
            _badgeRoot.position -= _camera.transform.forward * badgeForward;
        }

        float pop = Mathf.Clamp01((Time.unscaledTime - _popTime) / 0.35f);
        float popScale = pop < 1f ? 1f + 0.35f * Mathf.Sin(pop * Mathf.PI) : 1f;
        _badgeRoot.localScale = Vector3.one * popScale;
    }

    private void MeasureSilhouette()
    {
        Bounds bounds = default;
        bool hasBounds = false;
        Renderer[] renderers = GetComponentsInChildren<Renderer>(false);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || r == _pad || r.transform.IsChildOf(_badgeRoot))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = r.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        if (!hasBounds)
        {
            _center = Vector3.zero;
            _extent = Vector2.one * 2f;
            return;
        }

        Vector3 localCenter = transform.InverseTransformPoint(bounds.center);
        Vector3 localSize = transform.InverseTransformVector(bounds.size);
        _center = new Vector3(localCenter.x, 0f, localCenter.z);
        float spriteWorld = padSprite != null ? padSprite.bounds.size.x : 1f;
        _extent = new Vector2(
            (Mathf.Abs(localSize.x) * padPadding) / Mathf.Max(0.01f, spriteWorld),
            (Mathf.Abs(localSize.z) * padPadding) / Mathf.Max(0.01f, spriteWorld));
    }
}
