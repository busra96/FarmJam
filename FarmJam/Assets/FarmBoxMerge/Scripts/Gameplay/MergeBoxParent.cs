using System.Collections.Generic;
using UnityEngine;

public enum MergeBoxPatternType
{
    Single,
    Line,
    L,
    T,
    Z,
    Square
}

public class MergeBoxParent : MonoBehaviour
{
    private static readonly Vector2Int[] NeighborDirections =
    {
        Vector2Int.left,
        Vector2Int.right,
        Vector2Int.up,
        Vector2Int.down
    };

    [SerializeField] private int counterValue;
    [SerializeField] private ColorType colorType;
    [SerializeField] private MergeBoxPatternType patternType;
    [SerializeField] private List<Box> boxes = new List<Box>();
    [SerializeField] private float collapseDuration = 0.24f;
    [SerializeField] private float collapseStagger = 0.04f;
    [SerializeField] private float collapsePopScale = 1.12f;
    [SerializeField] private float collapseLift = 0.18f;

    private bool _isCollapsing;

    public int CounterValue => counterValue;
    public ColorType ColorType => colorType;
    public MergeBoxPatternType PatternType => patternType;
    public IReadOnlyList<Box> Boxes => boxes;
    public bool IsCollapsing => _isCollapsing;

    public void Initialize(int newCounterValue, ColorType newColorType, MergeBoxPatternType newPatternType, IList<Box> spawnedBoxes, IList<Vector2Int> boxCells)
    {
        counterValue = Mathf.Max(1, newCounterValue);
        colorType = newColorType;
        patternType = newPatternType;
        boxes.Clear();

        for (int i = 0; i < spawnedBoxes.Count; i++)
        {
            Box box = spawnedBoxes[i];
            if (box == null)
            {
                continue;
            }

            boxes.Add(box);
            Vector2Int gridCell = boxCells != null && i < boxCells.Count ? boxCells[i] : Vector2Int.zero;
            box.Initialize(this, boxes.Count - 1, newColorType, gridCell);
        }

        BuildNeighbors();
    }

    public void HandleBoxFilled(Box filledBox)
    {
        if (_isCollapsing || filledBox == null)
        {
            return;
        }

        List<Box> connectedBoxes = CollectConnectedBoxes(filledBox);
        if (connectedBoxes.Count == 0)
        {
            return;
        }

        if (connectedBoxes.Count == 1)
        {
            StartCoroutine(CollapseBoxesRoutine(connectedBoxes));
            return;
        }

        for (int i = 0; i < connectedBoxes.Count; i++)
        {
            if (!connectedBoxes[i].HasAssignedItem)
            {
                return;
            }
        }

        StartCoroutine(CollapseBoxesRoutine(connectedBoxes));
    }

    private void BuildNeighbors()
    {
        Dictionary<Vector2Int, Box> boxesByCell = new Dictionary<Vector2Int, Box>(boxes.Count);
        for (int i = 0; i < boxes.Count; i++)
        {
            if (boxes[i] != null)
            {
                boxesByCell[boxes[i].GridCell] = boxes[i];
            }
        }

        for (int i = 0; i < boxes.Count; i++)
        {
            Box sourceBox = boxes[i];
            if (sourceBox == null)
            {
                continue;
            }

            List<Box> directNeighbors = new List<Box>(NeighborDirections.Length);
            for (int directionIndex = 0; directionIndex < NeighborDirections.Length; directionIndex++)
            {
                Vector2Int neighborCell = sourceBox.GridCell + NeighborDirections[directionIndex];
                if (boxesByCell.TryGetValue(neighborCell, out Box neighbor))
                {
                    directNeighbors.Add(neighbor);
                }
            }

            sourceBox.SetNeighbors(directNeighbors);
        }
    }

    private List<Box> CollectConnectedBoxes(Box startBox)
    {
        List<Box> connectedBoxes = new List<Box>();
        if (startBox == null)
        {
            return connectedBoxes;
        }

        Queue<Box> pendingBoxes = new Queue<Box>();
        HashSet<Box> visitedBoxes = new HashSet<Box>();
        pendingBoxes.Enqueue(startBox);
        visitedBoxes.Add(startBox);

        while (pendingBoxes.Count > 0)
        {
            Box currentBox = pendingBoxes.Dequeue();
            connectedBoxes.Add(currentBox);

            IReadOnlyList<Box> neighbors = currentBox.NeighborBoxes;
            for (int i = 0; i < neighbors.Count; i++)
            {
                Box neighbor = neighbors[i];
                if (neighbor == null || visitedBoxes.Contains(neighbor))
                {
                    continue;
                }

                visitedBoxes.Add(neighbor);
                pendingBoxes.Enqueue(neighbor);
            }
        }

        return connectedBoxes;
    }

    private System.Collections.IEnumerator CollapseBoxesRoutine(IList<Box> boxesToCollapse)
    {
        _isCollapsing = true;

        List<Box> validBoxes = new List<Box>();
        List<Transform> itemTransforms = new List<Transform>();
        List<Vector3> boxStartScales = new List<Vector3>();
        List<Vector3> itemStartScales = new List<Vector3>();
        List<Vector3> boxStartPositions = new List<Vector3>();

        for (int i = 0; i < boxesToCollapse.Count; i++)
        {
            Box box = boxesToCollapse[i];
            if (box == null)
            {
                continue;
            }

            validBoxes.Add(box);
            boxStartScales.Add(box.transform.localScale);
            boxStartPositions.Add(box.transform.localPosition);

            Transform itemTransform = box.AssignedItem != null ? box.AssignedItem.transform : null;
            itemTransforms.Add(itemTransform);
            itemStartScales.Add(itemTransform != null ? itemTransform.localScale : Vector3.zero);
        }

        if (validBoxes.Count == 0)
        {
            _isCollapsing = false;
            yield break;
        }

        float duration = Mathf.Max(0.01f, collapseDuration);
        float stagger = Mathf.Max(0f, collapseStagger);
        float totalDuration = duration + (stagger * Mathf.Max(0, validBoxes.Count - 1));
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            for (int i = 0; i < validBoxes.Count; i++)
            {
                Box box = validBoxes[i];
                if (box == null)
                {
                    continue;
                }

                float localElapsed = Mathf.Clamp(elapsed - (i * stagger), 0f, duration);
                float progress = Mathf.Clamp01(localElapsed / duration);

                float scaleMultiplier;
                if (progress < 0.35f)
                {
                    float popProgress = progress / 0.35f;
                    float easedPop = FarmBoxMergeMath.EaseOutCubic(popProgress);
                    scaleMultiplier = Mathf.LerpUnclamped(1f, collapsePopScale, easedPop);
                }
                else
                {
                    float collapseProgress = (progress - 0.35f) / 0.65f;
                    float easedCollapse = FarmBoxMergeMath.SmoothStep(collapseProgress);
                    scaleMultiplier = Mathf.LerpUnclamped(collapsePopScale, 0f, easedCollapse);
                }

                box.transform.localScale = boxStartScales[i] * scaleMultiplier;
                box.transform.localPosition = boxStartPositions[i] + (Vector3.up * (collapseLift * progress));

                Transform itemTransform = itemTransforms[i];
                if (itemTransform != null)
                {
                    itemTransform.localScale = itemStartScales[i] * scaleMultiplier;
                }
            }

            yield return null;
        }

        for (int i = 0; i < validBoxes.Count; i++)
        {
            Box box = validBoxes[i];
            if (box == null)
            {
                continue;
            }

            MergeItem assignedItem = box.AssignedItem;
            box.ClearAssignedItem();
            boxes.Remove(box);

            if (assignedItem != null)
            {
                Destroy(assignedItem.gameObject);
            }

            Destroy(box.gameObject);
        }

        if (boxes.Count == 0)
        {
            Destroy(gameObject);
            yield break;
        }

        BuildNeighbors();
        _isCollapsing = false;
    }
}
