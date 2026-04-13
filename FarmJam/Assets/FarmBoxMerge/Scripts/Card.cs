using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Card : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("View")]
    public Image BackgroundColorImg;
    public TextMeshProUGUI CounterTxt;

    [Header("Data")]
    [SerializeField] private ColorType colorType = ColorType.Green;
    [SerializeField] private Color fallbackColor = Color.white;

    [Header("Interaction")]
    [SerializeField] private float selectedScale = 1.08f;
    [SerializeField] private float dragScale = 1.14f;
    [SerializeField] private float dragFollowSharpness = 24f;
    [SerializeField] private float scaleSharpness = 18f;
    [SerializeField] private float rotationSharpness = 18f;
    [SerializeField] private float returnDuration = 0.18f;
    [SerializeField] private float mergeDuration = 0.12f;
    [SerializeField] private float dragWobbleAngle = 4.5f;
    [SerializeField] private float dragWobbleSpeed = 10f;
    [SerializeField] private float dragTiltByMovement = 0.12f;
    [SerializeField] private float mergePopScale = 1.16f;
    [SerializeField] private float mergePopDuration = 0.18f;

    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private LayoutElement _layoutElement;
    private CardMergeBoard _board;

    private int _counterValue = 1;
    private bool _hasRuntimeData;
    private bool _isDragging;
    private bool _mergeCompleted;

    private Transform _originalParent;
    private int _originalSiblingIndex;
    private Vector2 _originalAnchorMin;
    private Vector2 _originalAnchorMax;
    private Vector2 _originalPivot;
    private Vector2 _originalSizeDelta;
    private Vector3 _baseScale = Vector3.one;
    private float _baseRotationZ;
    private Vector2 _dragPointerOffset;
    private float _targetScaleMultiplier = 1f;
    private float _scaleEffectMultiplier = 1f;
    private Vector2 _dragTargetPosition;
    private Vector2 _lastVisualDragPosition;
    private float _targetRotationOffset;
    private RectTransform _placeholderRect;
    private Coroutine _motionRoutine;
    private Coroutine _scaleEffectRoutine;

    public int CounterValue => _counterValue;
    public ColorType CardColorType => colorType;
    public bool HasRuntimeData => _hasRuntimeData;
    public bool IsDragging => _isDragging;
    public bool MergeCompleted => _mergeCompleted;
    public RectTransform RectTransform => _rectTransform != null ? _rectTransform : _rectTransform = (RectTransform)transform;

    private CanvasGroup CanvasGroup
    {
        get
        {
            if (_canvasGroup != null)
            {
                return _canvasGroup;
            }

            if (!TryGetComponent(out _canvasGroup))
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            return _canvasGroup;
        }
    }

    private LayoutElement LayoutElement
    {
        get
        {
            if (_layoutElement != null)
            {
                return _layoutElement;
            }

            if (!TryGetComponent(out _layoutElement))
            {
                _layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            return _layoutElement;
        }
    }

    private void Awake()
    {
        ResolveReferences();
        CaptureBaseScale();
        _targetScaleMultiplier = 1f;

        if (!_hasRuntimeData)
        {
            SyncDataFromView();
        }
    }

    private void Update()
    {
        SmoothScale();
        SmoothDragFollow();
        SmoothRotation();
    }

    private void OnDestroy()
    {
        StopScaleEffectRoutine();
        DestroyPlaceholder();
    }

    private void Reset()
    {
        ResolveReferences();
        CaptureBaseScale();
    }

    private void OnValidate()
    {
        ResolveReferences();
        CaptureBaseScale();
    }

    public void Initialize(CardMergeBoard board, int counterValue, ColorType cardColorType)
    {
        AssignBoard(board);
        _counterValue = Mathf.Max(1, counterValue);
        colorType = cardColorType;
        _hasRuntimeData = true;
        RefreshVisuals();
    }

    public void AssignBoard(CardMergeBoard board)
    {
        _board = board;
    }

    public void SyncDataFromView()
    {
        ResolveReferences();

        if (CounterTxt != null && int.TryParse(CounterTxt.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedCounter))
        {
            _counterValue = Mathf.Max(1, parsedCounter);
        }

        if (BackgroundColorImg != null)
        {
            fallbackColor = BackgroundColorImg.color;
        }

        if (_board != null && _board.TryResolveColorType(fallbackColor, out ColorType resolvedColorType))
        {
            colorType = resolvedColorType;
        }

        _hasRuntimeData = true;
        RefreshVisuals();
    }

    public void SetCounter(int newCounterValue)
    {
        _counterValue = Mathf.Max(1, newCounterValue);
        _hasRuntimeData = true;
        RefreshVisuals();
    }

    public void SetColorType(ColorType cardColorType)
    {
        colorType = cardColorType;
        _hasRuntimeData = true;
        RefreshVisuals();
    }

    public void RefreshVisuals()
    {
        ResolveReferences();

        if (BackgroundColorImg != null)
        {
            BackgroundColorImg.color = ResolveVisualColor();
        }

        if (CounterTxt != null)
        {
            CounterTxt.text = _counterValue.ToString(CultureInfo.InvariantCulture);
        }
    }

    public bool CanMergeWith(Card otherCard)
    {
        if (otherCard == null || otherCard == this)
        {
            return false;
        }

        return otherCard.CounterValue == CounterValue && otherCard.CardColorType == CardColorType;
    }

    public void PrepareForDrag(RectTransform dragLayer, PointerEventData eventData)
    {
        ResolveReferences();
        EnsureBoardReference();
        StopMotionRoutine();

        _mergeCompleted = false;
        _isDragging = true;
        _targetRotationOffset = 0f;

        CacheLayoutState();
        CreatePlaceholder();

        Vector2 currentSize = RectTransform.rect.size;
        if (currentSize.x <= 0f || currentSize.y <= 0f)
        {
            currentSize = _originalSizeDelta;
        }

        Camera eventCamera = eventData.pressEventCamera;
        Vector2 pointerLocalPosition = ScreenPointToLocalPoint(dragLayer, eventData.position, eventCamera);
        Vector2 cardLocalPosition = ScreenPointToLocalPoint(dragLayer, RectTransformUtility.WorldToScreenPoint(eventCamera, RectTransform.position), eventCamera);
        _dragPointerOffset = cardLocalPosition - pointerLocalPosition;

        LayoutElement.ignoreLayout = true;
        CanvasGroup.blocksRaycasts = false;

        RectTransform.SetParent(dragLayer, false);
        RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        RectTransform.pivot = new Vector2(0.5f, 0.5f);
        RectTransform.sizeDelta = currentSize;
        RectTransform.anchoredPosition = cardLocalPosition;
        _dragTargetPosition = cardLocalPosition;
        _lastVisualDragPosition = cardLocalPosition;
        RectTransform.SetAsLastSibling();

        _targetScaleMultiplier = dragScale;
    }

    public void UpdateDragPosition(Vector2 localPointerPosition)
    {
        _dragTargetPosition = localPointerPosition + _dragPointerOffset;
    }

    public void ReturnToOriginalSlot()
    {
        _isDragging = false;
        _targetScaleMultiplier = 1f;
        _targetRotationOffset = 0f;
        StopMotionRoutine();

        if (_placeholderRect == null || _originalParent == null)
        {
            RestoreToOriginalSlotImmediate();
            return;
        }

        _motionRoutine = StartCoroutine(AnimateBackToPlaceholder());
    }

    public void MergeInto(Card targetCard)
    {
        if (targetCard == null)
        {
            ReturnToOriginalSlot();
            return;
        }

        _mergeCompleted = true;
        _isDragging = false;
        _targetScaleMultiplier = 1f;
        _targetRotationOffset = 0f;
        StopMotionRoutine();
        _motionRoutine = StartCoroutine(AnimateIntoTarget(targetCard));
    }

    public void PlayMergePop()
    {
        StopScaleEffectRoutine();
        _scaleEffectRoutine = StartCoroutine(AnimateMergePop());
    }

    public void MarkMergeCompleted()
    {
        _mergeCompleted = true;
        _isDragging = false;
    }

    public void ConsumeForWorldSpawn()
    {
        _mergeCompleted = true;
        _isDragging = false;
        _targetScaleMultiplier = 1f;
        _targetRotationOffset = 0f;

        StopMotionRoutine();
        StopScaleEffectRoutine();

        LayoutElement.ignoreLayout = false;
        CanvasGroup.blocksRaycasts = false;
        DestroyPlaceholder();

        if (_originalParent is RectTransform parentRect)
        {
            LayoutRebuilder.MarkLayoutForRebuild(parentRect);
        }

        Destroy(gameObject);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_mergeCompleted)
        {
            return;
        }

        EnsureReadyForInteraction();
        _targetScaleMultiplier = selectedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_isDragging || _mergeCompleted)
        {
            return;
        }

        _targetScaleMultiplier = 1f;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        EnsureReadyForInteraction();
        _board?.BeginDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _board?.UpdateDrag(this, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _board?.EndDrag(this, eventData);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null || eventData.pointerDrag == gameObject)
        {
            return;
        }

        Card draggedCard = eventData.pointerDrag.GetComponent<Card>();
        if (draggedCard == null)
        {
            return;
        }

        EnsureReadyForInteraction();
        _board?.TryMerge(draggedCard, this);
    }

    private void EnsureReadyForInteraction()
    {
        ResolveReferences();

        if (!_hasRuntimeData)
        {
            SyncDataFromView();
        }

        EnsureBoardReference();
    }

    private void EnsureBoardReference()
    {
        if (_board != null)
        {
            return;
        }

        _board = GetComponentInParent<CardMergeBoard>();
        if (_board != null)
        {
            return;
        }

        GridLayoutGroup gridLayoutGroup = GetComponentInParent<GridLayoutGroup>();
        if (gridLayoutGroup != null)
        {
            _board = gridLayoutGroup.GetComponent<CardMergeBoard>();
            if (_board == null)
            {
                _board = gridLayoutGroup.gameObject.AddComponent<CardMergeBoard>();
            }
        }
    }

    private void ResolveReferences()
    {
        _rectTransform ??= (RectTransform)transform;

        if (BackgroundColorImg == null)
        {
            BackgroundColorImg = FindImageByName("Background Color");
        }

        if (CounterTxt == null)
        {
            CounterTxt = FindTextByName("Counter");
        }
    }

    private void CaptureBaseScale()
    {
        if (!_isDragging)
        {
            _baseScale = RectTransform.localScale;
            _baseRotationZ = NormalizeAngle(RectTransform.localEulerAngles.z);
        }
    }

    private void CacheLayoutState()
    {
        _originalParent = RectTransform.parent;
        _originalSiblingIndex = RectTransform.GetSiblingIndex();
        _originalAnchorMin = RectTransform.anchorMin;
        _originalAnchorMax = RectTransform.anchorMax;
        _originalPivot = RectTransform.pivot;
        _originalSizeDelta = RectTransform.sizeDelta;
    }

    private void SmoothScale()
    {
        float interpolation = 1f - Mathf.Exp(-scaleSharpness * Time.unscaledDeltaTime);
        RectTransform.localScale = Vector3.Lerp(RectTransform.localScale, _baseScale * (_targetScaleMultiplier * _scaleEffectMultiplier), interpolation);
    }

    private void SmoothDragFollow()
    {
        if (!_isDragging)
        {
            _targetRotationOffset = 0f;
            return;
        }

        float interpolation = 1f - Mathf.Exp(-dragFollowSharpness * Time.unscaledDeltaTime);
        Vector2 nextPosition = Vector2.Lerp(RectTransform.anchoredPosition, _dragTargetPosition, interpolation);
        Vector2 frameDelta = nextPosition - _lastVisualDragPosition;
        _lastVisualDragPosition = nextPosition;
        RectTransform.anchoredPosition = nextPosition;

        float movementTilt = Mathf.Clamp(-frameDelta.x * dragTiltByMovement, -dragWobbleAngle, dragWobbleAngle);
        float wobble = Mathf.Sin(Time.unscaledTime * dragWobbleSpeed) * dragWobbleAngle * 0.45f;
        _targetRotationOffset = movementTilt + wobble;
    }

    private void SmoothRotation()
    {
        float interpolation = 1f - Mathf.Exp(-rotationSharpness * Time.unscaledDeltaTime);
        float currentZ = NormalizeAngle(RectTransform.localEulerAngles.z);
        float targetZ = _baseRotationZ + _targetRotationOffset;
        float nextZ = Mathf.LerpAngle(currentZ, targetZ, interpolation);
        RectTransform.localRotation = Quaternion.Euler(0f, 0f, nextZ);
    }

    private void CreatePlaceholder()
    {
        if (_placeholderRect != null || _originalParent is not RectTransform originalParentRect)
        {
            return;
        }

        GameObject placeholderObject = new GameObject($"{name}_Placeholder", typeof(RectTransform), typeof(LayoutElement), typeof(CanvasGroup));
        _placeholderRect = placeholderObject.GetComponent<RectTransform>();
        LayoutElement placeholderLayout = placeholderObject.GetComponent<LayoutElement>();
        CanvasGroup placeholderCanvasGroup = placeholderObject.GetComponent<CanvasGroup>();

        _placeholderRect.SetParent(originalParentRect, false);
        _placeholderRect.SetSiblingIndex(_originalSiblingIndex);
        _placeholderRect.anchorMin = _originalAnchorMin;
        _placeholderRect.anchorMax = _originalAnchorMax;
        _placeholderRect.pivot = _originalPivot;
        _placeholderRect.sizeDelta = _originalSizeDelta;

        Vector2 currentSize = RectTransform.rect.size;
        placeholderLayout.minWidth = currentSize.x;
        placeholderLayout.minHeight = currentSize.y;
        placeholderLayout.preferredWidth = currentSize.x;
        placeholderLayout.preferredHeight = currentSize.y;

        placeholderCanvasGroup.alpha = 0f;
        placeholderCanvasGroup.blocksRaycasts = false;
        placeholderCanvasGroup.interactable = false;
    }

    private void DestroyPlaceholder()
    {
        if (_placeholderRect == null)
        {
            return;
        }

        GameObject placeholderObject = _placeholderRect.gameObject;
        _placeholderRect = null;

        if (placeholderObject == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(placeholderObject);
            return;
        }

        DestroyImmediate(placeholderObject);
    }

    private void StopMotionRoutine()
    {
        if (_motionRoutine == null)
        {
            return;
        }

        StopCoroutine(_motionRoutine);
        _motionRoutine = null;
    }

    private void StopScaleEffectRoutine()
    {
        if (_scaleEffectRoutine == null)
        {
            return;
        }

        StopCoroutine(_scaleEffectRoutine);
        _scaleEffectRoutine = null;
        _scaleEffectMultiplier = 1f;
    }

    private IEnumerator AnimateBackToPlaceholder()
    {
        CanvasGroup.blocksRaycasts = false;
        RectTransform dragParent = RectTransform.parent as RectTransform;

        if (dragParent == null || _placeholderRect == null)
        {
            RestoreToOriginalSlotImmediate();
            yield break;
        }

        Vector2 startPosition = RectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < returnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Vector2 targetPosition = GetAnchoredPositionForTarget(dragParent, _placeholderRect);
            float progress = Mathf.Clamp01(elapsed / returnDuration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            RectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, easedProgress);
            yield return null;
        }

        RestoreToOriginalSlotImmediate();
    }

    private IEnumerator AnimateIntoTarget(Card targetCard)
    {
        CanvasGroup.blocksRaycasts = false;
        RectTransform dragParent = RectTransform.parent as RectTransform;

        if (dragParent == null || targetCard == null)
        {
            RestoreToOriginalSlotImmediate();
            yield break;
        }

        Vector2 startPosition = RectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < mergeDuration && targetCard != null)
        {
            elapsed += Time.unscaledDeltaTime;
            Vector2 targetPosition = GetAnchoredPositionForTarget(dragParent, targetCard.RectTransform);
            float progress = Mathf.Clamp01(elapsed / mergeDuration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            RectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, easedProgress);
            yield return null;
        }

        if (targetCard == null)
        {
            RestoreToOriginalSlotImmediate();
            yield break;
        }

        targetCard.SetCounter(targetCard.CounterValue + 1);
        targetCard.PlayMergePop();
        DestroyPlaceholder();

        if (_originalParent is RectTransform parentRect)
        {
            LayoutRebuilder.MarkLayoutForRebuild(parentRect);
        }

        Destroy(gameObject);
    }

    private void RestoreToOriginalSlotImmediate()
    {
        _isDragging = false;
        _targetRotationOffset = 0f;
        _scaleEffectMultiplier = 1f;
        LayoutElement.ignoreLayout = false;

        if (_originalParent != null)
        {
            int siblingIndex = GetRestoreSiblingIndex();
            RectTransform.SetParent(_originalParent, false);
            RectTransform.anchorMin = _originalAnchorMin;
            RectTransform.anchorMax = _originalAnchorMax;
            RectTransform.pivot = _originalPivot;
            RectTransform.sizeDelta = _originalSizeDelta;
            RectTransform.SetSiblingIndex(siblingIndex);
        }

        RectTransform.anchoredPosition = Vector2.zero;
        RectTransform.localScale = _baseScale;
        RectTransform.localRotation = Quaternion.Euler(0f, 0f, _baseRotationZ);
        CanvasGroup.blocksRaycasts = true;
        DestroyPlaceholder();

        if (_originalParent is RectTransform parentRect)
        {
            LayoutRebuilder.MarkLayoutForRebuild(parentRect);
        }
    }

    private int GetRestoreSiblingIndex()
    {
        if (_placeholderRect != null && _placeholderRect.parent == _originalParent)
        {
            return _placeholderRect.GetSiblingIndex();
        }

        int maxSiblingIndex = _originalParent != null ? Mathf.Max(0, _originalParent.childCount - 1) : 0;
        return Mathf.Clamp(_originalSiblingIndex, 0, maxSiblingIndex);
    }

    private IEnumerator AnimateMergePop()
    {
        float halfDuration = Mathf.Max(0.01f, mergePopDuration * 0.5f);
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / halfDuration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            _scaleEffectMultiplier = Mathf.LerpUnclamped(1f, mergePopScale, easedProgress);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / halfDuration);
            float easedProgress = progress * progress * (3f - 2f * progress);
            _scaleEffectMultiplier = Mathf.LerpUnclamped(mergePopScale, 1f, easedProgress);
            yield return null;
        }

        _scaleEffectMultiplier = 1f;
        _scaleEffectRoutine = null;
    }

    private Vector2 GetAnchoredPositionForTarget(RectTransform currentParent, RectTransform targetRect)
    {
        Camera eventCamera = _board != null ? _board.EventCamera : null;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, targetRect.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(currentParent, screenPoint, eventCamera, out Vector2 localPoint);
        return localPoint;
    }

    private Image FindImageByName(string targetName)
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image.gameObject.name == targetName)
            {
                return image;
            }
        }

        return null;
    }

    private TextMeshProUGUI FindTextByName(string targetName)
    {
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            if (text.gameObject.name == targetName)
            {
                return text;
            }
        }

        return null;
    }

    private Vector2 ScreenPointToLocalPoint(RectTransform targetRect, Vector2 screenPoint, Camera eventCamera)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screenPoint, eventCamera, out Vector2 localPoint);
        return localPoint;
    }

    private Color ResolveVisualColor()
    {
        if (_board != null)
        {
            fallbackColor = _board.GetColorOrFallback(colorType, fallbackColor);
        }

        return fallbackColor;
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f)
        {
            angle -= 360f;
        }

        while (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }
}
