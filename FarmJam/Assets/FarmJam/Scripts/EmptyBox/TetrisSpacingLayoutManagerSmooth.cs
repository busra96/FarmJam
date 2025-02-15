using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TetrisSpacingLayoutManagerSmooth : MonoBehaviour
{
    public List<SpawnPoint> spawnPoints; // Spawn noktaları
    public List<TetrisSpacing> tetrisPieces = new List<TetrisSpacing>(); // Tetris parçaları
    public Vector3 centerPosition;
    public float extraSpacing = 0.5f;
    public float smoothSpeed = 5f; // Daha yumuşak hareket için

    private bool isArranging = false;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isArranging)
        {
            StartCoroutine(ArrangeSpawnPoints());
        }
    }

    [ContextMenu("ArrangeSpawnPoints")]
    public IEnumerator ArrangeSpawnPoints()
    {
        isArranging = true;

        List<TetrisSpacing> activePieces = new List<TetrisSpacing>();

        foreach (var spawnPoint in spawnPoints)
        {
            if (spawnPoint.EmptyBox != null)
            {
                activePieces.Add(spawnPoint.EmptyBox.TetrisSpacing);
            }
        }

        int count = activePieces.Count;
        if (count == 0)
        {
            isArranging = false;
            yield break;
        }

        if (count == 1)
        {
            yield return MoveToPosition(spawnPoints[0].transform, centerPosition.x);
        }
        else if (count == 2)
        {
            yield return AdjustTwoPieces(activePieces[0], activePieces[1]);
        }
        else if (count == 3)
        {
            yield return AdjustThreePieces(activePieces[0], activePieces[1], activePieces[2]);
        }

        isArranging = false;
    }

    private IEnumerator AdjustTwoPieces(TetrisSpacing p0, TetrisSpacing p1)
    {
        float spacing = CalculateSpacing(p0, p1);
        yield return MoveToPosition(spawnPoints[0].transform, centerPosition.x - spacing);
        yield return MoveToPosition(spawnPoints[1].transform, centerPosition.x + spacing);
    }

    private IEnumerator AdjustThreePieces(TetrisSpacing p0, TetrisSpacing p1, TetrisSpacing p2)
    {
        float spacingP0P1 = CalculateSpacing(p0, p1);
        float spacingP1P2 = CalculateSpacing(p1, p2);

        yield return MoveToPosition(spawnPoints[0].transform, centerPosition.x - spacingP0P1);
        yield return MoveToPosition(spawnPoints[1].transform, centerPosition.x);
        yield return MoveToPosition(spawnPoints[2].transform, centerPosition.x + spacingP1P2);
    }

    private float CalculateSpacing(TetrisSpacing left, TetrisSpacing right)
    {
        float leftSize = left.GetSizeAtRotation(Mathf.RoundToInt(left.transform.eulerAngles.y)).y;
        float rightSize = right.GetSizeAtRotation(Mathf.RoundToInt(right.transform.eulerAngles.y)).x;
        return (leftSize + rightSize + extraSpacing) * 0.5f;
    }

    private IEnumerator MoveToPosition(Transform obj, float targetX)
    {
        Vector3 startPosition = obj.position;
        Vector3 targetPosition = new Vector3(targetX, obj.position.y, obj.position.z);
        float elapsedTime = 0f;

        while (elapsedTime < 1f)
        {
            obj.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime);
            elapsedTime += Time.deltaTime * smoothSpeed;
            yield return null;
        }

        obj.position = targetPosition;
    }
}
