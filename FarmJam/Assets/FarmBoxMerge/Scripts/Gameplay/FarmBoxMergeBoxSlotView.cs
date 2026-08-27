using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class FarmBoxMergeBoxSlotView : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [SerializeField, Range(1, FarmBoxMergeRules.MaxCardCounter)] private int acceptedCardValue = 1;
    [SerializeField] private Transform previewRoot;
    [SerializeField, Range(0.05f, 0.8f)] private float idleAlpha = 0.28f;
    [SerializeField, Range(0.1f, 1f)] private float compatibleAlpha = 0.68f;
    [SerializeField, Range(0.01f, 0.3f)] private float incompatibleAlpha = 0.07f;

    private readonly List<Renderer> _previewRenderers = new List<Renderer>();
    private readonly List<Box> _previewBoxes = new List<Box>(FarmBoxMergeRules.MaxCardCounter);
    private MaterialPropertyBlock _propertyBlock;

    private MergeBoxPatternType _patternType;
    private Vector2Int[] _patternCells;
    private int _draggedCardValue;
    private Color _previewColor = Color.white;
    private bool _isDraggingCard;
    private bool _isBuildingPreview;
    private bool _initialized;
    private bool _hasRequirement;
    private bool _wasOccupied;

    public int AcceptedCardValue => acceptedCardValue;
    public bool IsInitialized => _initialized;
    public bool HasRequirement => _hasRequirement;
    public bool IsOccupied => FindActiveBoxGroup() != null;
    public BoxPatternDefinition Pattern => new BoxPatternDefinition(_patternType, _patternCells);

    public event Action<FarmBoxMergeBoxSlotView> BecameAvailable;

    public bool CanAccept(int cardValue)
    {
        return _hasRequirement
            && !IsOccupied
            && cardValue == acceptedCardValue;
    }

    public void SetRequirement(
        FarmBoxMergeBoxRequirement requirement,
        BoxPatternDefinition pattern,
        Vector3[] localPositions,
        float heightOffset,
        IBoxFactory boxFactory,
        Material previewMaterial,
        Color previewColor)
    {
        acceptedCardValue = FarmBoxMergeRules.ClampCardCounter(requirement.BoxSize);
        _patternType = pattern.PatternType;
        _patternCells = pattern.Cells;
        _previewColor = previewColor;
        _propertyBlock ??= new MaterialPropertyBlock();
        BuildPreview(localPositions, heightOffset, boxFactory, previewMaterial);
        _initialized = true;
        _hasRequirement = true;
        _wasOccupied = IsOccupied;
        RefreshPreview();
    }

    public void ClearRequirement()
    {
        _hasRequirement = false;
        _isDraggingCard = false;
        if (previewRoot != null)
        {
            previewRoot.gameObject.SetActive(false);
        }
    }

    public void SetDragHighlight(int cardValue, bool isDragging)
    {
        _draggedCardValue = cardValue;
        _isDraggingCard = isDragging;
        RefreshPreview();
    }

    public void RefreshPreview()
    {
        if (previewRoot == null || !_hasRequirement)
        {
            if (previewRoot != null)
            {
                previewRoot.gameObject.SetActive(false);
            }

            return;
        }

        bool occupied = IsOccupied;
        previewRoot.gameObject.SetActive(!occupied);
        if (occupied)
        {
            return;
        }

        bool compatible = _isDraggingCard
            && _draggedCardValue == acceptedCardValue;
        float targetAlpha = !_isDraggingCard
            ? idleAlpha
            : compatible ? compatibleAlpha : incompatibleAlpha;
        Color targetColor = compatible
            ? Color.Lerp(_previewColor, Color.white, 0.16f)
            : _previewColor;
        targetColor.a = targetAlpha;

        for (int i = 0; i < _previewRenderers.Count; i++)
        {
            Renderer previewRenderer = _previewRenderers[i];
            if (previewRenderer == null)
            {
                continue;
            }

            previewRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, targetColor);
            _propertyBlock.SetColor(ColorId, targetColor);
            previewRenderer.SetPropertyBlock(_propertyBlock);
        }

        previewRoot.localScale = compatible ? Vector3.one * 1.06f : Vector3.one;
    }

    private void OnTransformChildrenChanged()
    {
        if (_isBuildingPreview)
        {
            return;
        }

        bool occupied = IsOccupied;
        bool becameAvailable = _wasOccupied && !occupied;
        _wasOccupied = occupied;

        if (becameAvailable && _hasRequirement)
        {
            ClearRequirement();
            BecameAvailable?.Invoke(this);
            return;
        }

        RefreshPreview();
    }

    private void BuildPreview(
        Vector3[] localPositions,
        float heightOffset,
        IBoxFactory boxFactory,
        Material previewMaterial)
    {
        if (localPositions == null)
        {
            return;
        }

        _isBuildingPreview = true;
        if (previewRoot == null)
        {
            GameObject previewObject = new GameObject("BoxSlotPreview");
            previewRoot = previewObject.transform;
            previewRoot.SetParent(transform, false);
        }

        previewRoot.name = $"BoxSlotPreview_{acceptedCardValue}";
        previewRoot.localPosition = Vector3.up * heightOffset;
        previewRoot.localRotation = Quaternion.identity;
        previewRoot.localScale = Vector3.one;

        for (int i = 0; i < localPositions.Length; i++)
        {
            Box previewBox = GetOrCreatePreviewBox(i, boxFactory, previewMaterial);
            if (previewBox == null)
            {
                continue;
            }

            previewBox.name = $"GhostBox_{i + 1:00}";
            previewBox.gameObject.SetActive(true);
            previewBox.transform.localPosition = localPositions[i];
            previewBox.transform.localRotation = Quaternion.identity;
        }

        for (int i = localPositions.Length; i < _previewBoxes.Count; i++)
        {
            if (_previewBoxes[i] != null)
            {
                _previewBoxes[i].gameObject.SetActive(false);
            }
        }

        _isBuildingPreview = false;
    }

    private Box GetOrCreatePreviewBox(
        int index,
        IBoxFactory boxFactory,
        Material previewMaterial)
    {
        if (index < _previewBoxes.Count && _previewBoxes[index] != null)
        {
            return _previewBoxes[index];
        }

        if (boxFactory == null)
        {
            return null;
        }

        Box previewBox = boxFactory.Create(previewRoot);
        if (previewBox == null)
        {
            return null;
        }

        previewBox.enabled = false;
        SetLayerRecursively(previewBox.gameObject, LayerMask.NameToLayer("Ignore Raycast"));

        Collider[] colliders = previewBox.GetComponentsInChildren<Collider>(true);
        for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
        {
            colliders[colliderIndex].enabled = false;
        }

        Renderer[] renderers = previewBox.GetComponentsInChildren<Renderer>(true);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer previewRenderer = renderers[rendererIndex];
            previewRenderer.sharedMaterial = previewMaterial;
            previewRenderer.shadowCastingMode = ShadowCastingMode.Off;
            previewRenderer.receiveShadows = false;
            _previewRenderers.Add(previewRenderer);
        }

        while (_previewBoxes.Count <= index)
        {
            _previewBoxes.Add(null);
        }

        _previewBoxes[index] = previewBox;
        return previewBox;
    }

    private MergeBoxParent FindActiveBoxGroup()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != previewRoot && child.TryGetComponent(out MergeBoxParent group))
            {
                return group;
            }
        }

        return null;
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null || layer < 0)
        {
            return;
        }

        target.layer = layer;
        for (int i = 0; i < target.transform.childCount; i++)
        {
            SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
        }
    }
}
