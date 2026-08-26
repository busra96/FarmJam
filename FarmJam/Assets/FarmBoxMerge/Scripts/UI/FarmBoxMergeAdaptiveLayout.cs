using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class FarmBoxMergeAdaptiveLayout : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private CanvasScaler canvasScaler;
    [SerializeField] private Transform cameraBackdrop;

    [Header("Reference Framing")]
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);
    [SerializeField, Range(1f, 179f)] private float referenceVerticalFieldOfView = 50f;
    [SerializeField, Range(1f, 179f)] private float maximumVerticalFieldOfView = 88f;

    [Header("Safe Area")]
    [SerializeField] private bool respectSafeArea = true;
    [SerializeField] private RectTransform[] topSafeAreaElements;
    [SerializeField] private RectTransform[] bottomSafeAreaElements;
    [SerializeField] private RectTransform[] horizontalSafeAreaElements;

    private Vector2[] _topBasePositions;
    private Vector2[] _bottomBasePositions;
    private Vector2[] _horizontalBaseOffsetMins;
    private Vector2[] _horizontalBaseOffsetMaxs;
    private Vector3 _backdropBaseScale;
    private int _lastScreenWidth = -1;
    private int _lastScreenHeight = -1;
    private Rect _lastSafeArea;
    private bool _hasCachedLayout;

    private void Awake()
    {
        ResolveReferences();
        CacheBaseLayout();
        ApplyLayout(true);
    }

    private void OnEnable()
    {
        ApplyLayout(true);
    }

    private void Update()
    {
        ApplyLayout(false);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            ApplyLayout(true);
        }
    }

    [ContextMenu("Apply Adaptive Layout")]
    public void ApplyNow()
    {
        ResolveReferences();
        CacheBaseLayout();
        ApplyLayout(true);
    }

    public static float CalculateVerticalFieldOfView(
        float referenceFieldOfView,
        float referenceAspect,
        float screenAspect)
    {
        if (referenceAspect <= 0f || screenAspect <= 0f || screenAspect >= referenceAspect)
        {
            return referenceFieldOfView;
        }

        float referenceHalfAngle = referenceFieldOfView * 0.5f * Mathf.Deg2Rad;
        float fittedHalfAngle = Mathf.Atan(Mathf.Tan(referenceHalfAngle) * referenceAspect / screenAspect);
        return fittedHalfAngle * 2f * Mathf.Rad2Deg;
    }

    private void ResolveReferences()
    {
        gameplayCamera ??= Camera.main;
        canvasScaler ??= GetComponent<CanvasScaler>();
    }

    private void CacheBaseLayout()
    {
        if (_hasCachedLayout)
        {
            return;
        }

        _topBasePositions = CacheAnchoredPositions(topSafeAreaElements);
        _bottomBasePositions = CacheAnchoredPositions(bottomSafeAreaElements);
        CacheHorizontalOffsets();

        if (cameraBackdrop != null)
        {
            _backdropBaseScale = cameraBackdrop.localScale;
        }

        _hasCachedLayout = true;
    }

    private void ApplyLayout(bool force)
    {
        if (!_hasCachedLayout)
        {
            ResolveReferences();
            CacheBaseLayout();
        }

        int screenWidth = Screen.width;
        int screenHeight = Screen.height;
        Rect safeArea = Screen.safeArea;
        if (safeArea.width <= 0f || safeArea.height <= 0f)
        {
            safeArea = new Rect(0f, 0f, screenWidth, screenHeight);
        }

        if (!force &&
            screenWidth == _lastScreenWidth &&
            screenHeight == _lastScreenHeight &&
            safeArea == _lastSafeArea)
        {
            return;
        }

        if (screenWidth <= 0 || screenHeight <= 0 || referenceResolution.x <= 0f || referenceResolution.y <= 0f)
        {
            return;
        }

        _lastScreenWidth = screenWidth;
        _lastScreenHeight = screenHeight;
        _lastSafeArea = safeArea;

        float screenAspect = screenWidth / (float)screenHeight;
        float referenceAspect = referenceResolution.x / referenceResolution.y;
        bool screenIsNarrower = screenAspect <= referenceAspect;

        ConfigureCanvasScaler(screenIsNarrower);
        ConfigureCamera(screenAspect, referenceAspect);
        ConfigureSafeArea(screenWidth, screenHeight, safeArea, screenIsNarrower);
    }

    private void ConfigureCanvasScaler(bool screenIsNarrower)
    {
        if (canvasScaler == null)
        {
            return;
        }

        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        // Narrow phones retain the complete reference width. Tablets and wide
        // foldables retain the complete reference height. This is the UI version
        // of a "contain" fit and prevents either axis from being cropped.
        canvasScaler.matchWidthOrHeight = screenIsNarrower ? 0f : 1f;
    }

    private void ConfigureCamera(float screenAspect, float referenceAspect)
    {
        if (gameplayCamera == null || gameplayCamera.orthographic)
        {
            return;
        }

        float fittedFieldOfView = CalculateVerticalFieldOfView(
            referenceVerticalFieldOfView,
            referenceAspect,
            screenAspect);

        gameplayCamera.fieldOfView = Mathf.Min(fittedFieldOfView, maximumVerticalFieldOfView);
        ResizeBackdrop(screenAspect, referenceAspect, gameplayCamera.fieldOfView);
    }

    private void ResizeBackdrop(float screenAspect, float referenceAspect, float fieldOfView)
    {
        if (cameraBackdrop == null)
        {
            return;
        }

        float referenceTangent = Mathf.Tan(referenceVerticalFieldOfView * 0.5f * Mathf.Deg2Rad);
        float currentTangent = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
        float heightMultiplier = currentTangent / Mathf.Max(0.0001f, referenceTangent);
        float widthMultiplier = heightMultiplier * screenAspect / Mathf.Max(0.0001f, referenceAspect);
        float multiplier = Mathf.Max(1f, heightMultiplier, widthMultiplier);

        cameraBackdrop.localScale = new Vector3(
            _backdropBaseScale.x * multiplier,
            _backdropBaseScale.y * multiplier,
            _backdropBaseScale.z);
    }

    private void ConfigureSafeArea(
        int screenWidth,
        int screenHeight,
        Rect safeArea,
        bool screenIsNarrower)
    {
        float scaleFactor = screenIsNarrower
            ? screenWidth / referenceResolution.x
            : screenHeight / referenceResolution.y;

        scaleFactor = Mathf.Max(0.0001f, scaleFactor);
        float leftInset = respectSafeArea ? safeArea.xMin / scaleFactor : 0f;
        float rightInset = respectSafeArea ? (screenWidth - safeArea.xMax) / scaleFactor : 0f;
        float bottomInset = respectSafeArea ? safeArea.yMin / scaleFactor : 0f;
        float topInset = respectSafeArea ? (screenHeight - safeArea.yMax) / scaleFactor : 0f;

        // On tablets and unfolded devices the Canvas can be much wider than the
        // portrait design. Keep interactive UI inside a centered 1080-unit lane
        // instead of stretching card and button areas across the full display.
        float canvasWidth = screenWidth / scaleFactor;
        float contentPadding = Mathf.Max(0f, (canvasWidth - referenceResolution.x) * 0.5f);
        leftInset += contentPadding;
        rightInset += contentPadding;

        ApplyVerticalInset(topSafeAreaElements, _topBasePositions, -topInset);
        ApplyVerticalInset(bottomSafeAreaElements, _bottomBasePositions, bottomInset);
        ApplyHorizontalInsets(leftInset, rightInset);
    }

    private static Vector2[] CacheAnchoredPositions(RectTransform[] elements)
    {
        if (elements == null)
        {
            return System.Array.Empty<Vector2>();
        }

        Vector2[] positions = new Vector2[elements.Length];
        for (int index = 0; index < elements.Length; index++)
        {
            if (elements[index] != null)
            {
                positions[index] = elements[index].anchoredPosition;
            }
        }

        return positions;
    }

    private void CacheHorizontalOffsets()
    {
        if (horizontalSafeAreaElements == null)
        {
            _horizontalBaseOffsetMins = System.Array.Empty<Vector2>();
            _horizontalBaseOffsetMaxs = System.Array.Empty<Vector2>();
            return;
        }

        int count = horizontalSafeAreaElements.Length;
        _horizontalBaseOffsetMins = new Vector2[count];
        _horizontalBaseOffsetMaxs = new Vector2[count];
        for (int index = 0; index < count; index++)
        {
            RectTransform element = horizontalSafeAreaElements[index];
            if (element == null)
            {
                continue;
            }

            _horizontalBaseOffsetMins[index] = element.offsetMin;
            _horizontalBaseOffsetMaxs[index] = element.offsetMax;
        }
    }

    private static void ApplyVerticalInset(
        RectTransform[] elements,
        Vector2[] basePositions,
        float verticalInset)
    {
        if (elements == null || basePositions == null)
        {
            return;
        }

        int count = Mathf.Min(elements.Length, basePositions.Length);
        for (int index = 0; index < count; index++)
        {
            RectTransform element = elements[index];
            if (element == null)
            {
                continue;
            }

            Vector2 position = basePositions[index];
            position.y += verticalInset;
            element.anchoredPosition = position;
        }
    }

    private void ApplyHorizontalInsets(float leftInset, float rightInset)
    {
        if (horizontalSafeAreaElements == null ||
            _horizontalBaseOffsetMins == null ||
            _horizontalBaseOffsetMaxs == null)
        {
            return;
        }

        int count = Mathf.Min(
            horizontalSafeAreaElements.Length,
            Mathf.Min(_horizontalBaseOffsetMins.Length, _horizontalBaseOffsetMaxs.Length));

        for (int index = 0; index < count; index++)
        {
            RectTransform element = horizontalSafeAreaElements[index];
            if (element == null)
            {
                continue;
            }

            Vector2 offsetMin = _horizontalBaseOffsetMins[index];
            Vector2 offsetMax = _horizontalBaseOffsetMaxs[index];
            offsetMin.x += leftInset;
            offsetMax.x -= rightInset;
            element.offsetMin = offsetMin;
            element.offsetMax = offsetMax;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        referenceResolution.x = Mathf.Max(1f, referenceResolution.x);
        referenceResolution.y = Mathf.Max(1f, referenceResolution.y);
        maximumVerticalFieldOfView = Mathf.Max(referenceVerticalFieldOfView, maximumVerticalFieldOfView);
    }
#endif
}
