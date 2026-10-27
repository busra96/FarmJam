using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws the 12 empty card sockets of the board tray behind the real card grid.
/// It mirrors the card GridLayoutGroup every frame the source layout changes, so
/// adaptive layout and safe-area changes keep both grids aligned. Cosmetic only.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class FarmBoxMergeCardSockets : MonoBehaviour
{
    [SerializeField] private GridLayoutGroup sourceGrid;
    [SerializeField] private Sprite socketSprite;
    [SerializeField] private Color socketColor = new Color(0.86f, 0.80f, 0.64f, 0.45f);
    [SerializeField, Min(1)] private int socketCount = FarmBoxMergeRules.MaxCardsOnBoard;
    [SerializeField, Range(0.5f, 1f)] private float socketInset = 0.94f;

    private GridLayoutGroup _grid;
    private RectTransform _rect;
    private Vector2 _lastCell;
    private Vector2 _lastSpacing;
    private RectOffset _lastPadding;
    private Rect _lastRect;

    public void Configure(GridLayoutGroup source, Sprite sprite)
    {
        sourceGrid = source;
        socketSprite = sprite;
    }

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _grid = GetComponent<GridLayoutGroup>();
        if (_grid == null)
        {
            _grid = gameObject.AddComponent<GridLayoutGroup>();
        }

        EnsureSockets();
        Sync(force: true);
    }

    private void LateUpdate()
    {
        Sync(force: false);
    }

    private void EnsureSockets()
    {
        for (int i = transform.childCount; i < socketCount; i++)
        {
            GameObject go = new GameObject("Socket" + (i + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            Image image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.sprite = socketSprite;
            image.type = socketSprite != null && socketSprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.color = socketColor;
            go.transform.localScale = Vector3.one * socketInset;
        }
    }

    private void Sync(bool force)
    {
        if (sourceGrid == null)
        {
            return;
        }

        RectTransform source = (RectTransform)sourceGrid.transform;
        if (source.parent == transform.parent && transform.GetSiblingIndex() > source.GetSiblingIndex())
        {
            transform.SetSiblingIndex(source.GetSiblingIndex());
        }

        if (!force && _lastCell == sourceGrid.cellSize && _lastSpacing == sourceGrid.spacing
            && _lastRect == source.rect && SamePadding(_lastPadding, sourceGrid.padding))
        {
            return;
        }

        _lastCell = sourceGrid.cellSize;
        _lastSpacing = sourceGrid.spacing;
        _lastRect = source.rect;
        _lastPadding = new RectOffset(sourceGrid.padding.left, sourceGrid.padding.right,
            sourceGrid.padding.top, sourceGrid.padding.bottom);

        _rect.anchorMin = source.anchorMin;
        _rect.anchorMax = source.anchorMax;
        _rect.pivot = source.pivot;
        _rect.anchoredPosition = source.anchoredPosition;
        _rect.sizeDelta = source.sizeDelta;

        _grid.padding = new RectOffset(sourceGrid.padding.left, sourceGrid.padding.right,
            sourceGrid.padding.top, sourceGrid.padding.bottom);
        _grid.cellSize = sourceGrid.cellSize;
        _grid.spacing = sourceGrid.spacing;
        _grid.startCorner = sourceGrid.startCorner;
        _grid.startAxis = sourceGrid.startAxis;
        _grid.childAlignment = sourceGrid.childAlignment;
        _grid.constraint = sourceGrid.constraint;
        _grid.constraintCount = sourceGrid.constraintCount;
    }

    private static bool SamePadding(RectOffset a, RectOffset b)
    {
        return a != null && b != null && a.left == b.left && a.right == b.right && a.top == b.top && a.bottom == b.bottom;
    }
}
