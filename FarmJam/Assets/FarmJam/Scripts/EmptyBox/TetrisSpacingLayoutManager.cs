using System;
using System.Collections.Generic;
using DG.Tweening;
using Signals;
using UnityEngine;

public class TetrisSpacingLayoutManager : MonoBehaviour
{
   public List<SpawnPoint> spawnPoints; // Spawn noktalarının transform'ları
   public TetrisSpacing P0;
   public TetrisSpacing P1;
   public TetrisSpacing P2;
   
   public Vector3 centerPosition; // Orta nokta
   public float extraSpacing = 0.5f; // Parçalar arasında ekstra boşluk

   /*private void OnEnable()
   {
      EmptyBoxSignals.OnAddedTetrisSpacingLayoutList.AddListener(OnAddedTetrisSpacingLayoutList);
      EmptyBoxSignals.OnRemovedTetrisLayoutList.AddListener(OnRemovedTetrisLayoutList);
      EmptyBoxSignals.OnUpdateTetrisLayout.AddListener(ArrangeSpawnPoints);
   }
   private void OnDisable()
   {
      EmptyBoxSignals.OnAddedTetrisSpacingLayoutList.RemoveListener(OnAddedTetrisSpacingLayoutList);
      EmptyBoxSignals.OnRemovedTetrisLayoutList.RemoveListener(OnRemovedTetrisLayoutList);
      EmptyBoxSignals.OnUpdateTetrisLayout.RemoveListener(ArrangeSpawnPoints);
   }*/

   private void Update()
   {
       if (Input.GetKeyDown(KeyCode.R))
       {
           ArrangeSpawnPoints();
       }
   }

   private void OnAddedTetrisSpacingLayoutList(EmptyBox obj)
   {
       ArrangeSpawnPoints();
   }

   private void OnRemovedTetrisLayoutList(EmptyBox obj)
   {
       for (int i = 0; i < spawnPoints.Count; i++)
       {
           if (spawnPoints[i].EmptyBox.TetrisSpacing.gameObject == obj.gameObject)
           {
               spawnPoints[i].EmptyBox.TetrisSpacing = null;
               spawnPoints[i].HasEmptyBox = false;
           }
       }
   }

   [ContextMenu(" ArrangeSpawnPoints ")]
    public void ArrangeSpawnPoints()
    {
        if(spawnPoints[0].EmptyBox != null) P0 = spawnPoints[0].EmptyBox.TetrisSpacing;
        if(spawnPoints[1].EmptyBox != null) P1 = spawnPoints[1].EmptyBox.TetrisSpacing;
        if(spawnPoints[2].EmptyBox != null) P2 = spawnPoints[2].EmptyBox.TetrisSpacing;
        

        if (P0 != null && P1 == null && P2 == null)
        {
            Debug.Log(" SAdECE P0 NULL DEĞİL ");
            spawnPoints[0].transform.position = centerPosition;
            return;
        }
        
        if (P0 == null && P1 != null && P2 == null)
        {
            Debug.Log(" SAdECE P1 NULL DEĞİL ");
            spawnPoints[1].transform.position = centerPosition;
            return;
        }
        
        if (P0 == null && P1 == null && P2 != null)
        {
            Debug.Log(" SAdECE P2 NULL DEĞİL ");
            spawnPoints[2].transform.position = centerPosition;
            return;
        }

        
        if (P0 != null && P1 != null && P2 != null)
            CalculateP0P1P2();
        
        else if (P0 != null && P1 != null && P2 == null)
            CalculateP0P1();

        else if (P0 != null && P1 == null && P2 != null)
            CalculateP0P2();

        else if (P0 == null && P1 != null && P2 != null)
            CalculateP1P2();

    }

    public void CalculateP0P1()
    {
        Debug.Log(" CalculateP0P1 ");
        Vector2 P0Spacing = P0.GetSizeAtRotation(Mathf.RoundToInt(P0.transform.eulerAngles.y));
        float p0RightSpacing = P0Spacing.y;
        
        Vector2 P1Spacing = P1.GetSizeAtRotation(Mathf.RoundToInt(P1.transform.eulerAngles.y));
        float p1LeftSpacing = P1Spacing.x;

        float targetSpacing = (p0RightSpacing + p1LeftSpacing + extraSpacing) * .5f;
        float P0TargetXPosition = centerPosition.x - targetSpacing;
       // spawnPoints[0].transform.position = new Vector3(P0TargetXPosition, 0,-10);
        spawnPoints[0].transform.DOMoveX(P0TargetXPosition, .1f).SetEase(Ease.Linear);
        
        
        float P1TargetXPosition = centerPosition.x + targetSpacing;
       // spawnPoints[1].transform.position = new Vector3(P1TargetXPosition, 0,-10);
        spawnPoints[1].transform.DOMoveX(P1TargetXPosition, .1f).SetEase(Ease.Linear);

    }
    
    
    // X _> LEFT           Y _> RİGHT
    public void CalculateP1P2()
    {
        Debug.Log(" CalculateP1P2 ");
        Vector2 P1Spacing = P1.GetSizeAtRotation(Mathf.RoundToInt(P1.transform.eulerAngles.y));
        float p1RightSpacing = P1Spacing.y;
        
        Vector2 P2Spacing = P2.GetSizeAtRotation(Mathf.RoundToInt(P2.transform.eulerAngles.y));
        float p2LeftSpacing = P2Spacing.x;

        float targetSpacing = (p1RightSpacing + p2LeftSpacing + extraSpacing) * .5f;
        float P1TargetPositionX = centerPosition.x - targetSpacing;
       // spawnPoints[1].transform.position = new Vector3(P1TargetPositionX, 0,-10);
        spawnPoints[1].transform.DOMoveX(P1TargetPositionX, .1f).SetEase(Ease.Linear);

        float P2TargetXPosition = centerPosition.x + targetSpacing;
       // spawnPoints[2].transform.position = new Vector3(P2TargetXPosition, 0,-10);
        spawnPoints[2].transform.DOMoveX(P2TargetXPosition, .1f).SetEase(Ease.Linear);
        
    }

    public void CalculateP0P2()
    {
        Debug.Log(" CalculateP0P2 ");
        Vector2 P0Spacing = P0.GetSizeAtRotation(Mathf.RoundToInt(P0.transform.eulerAngles.y));
        float p0RightSpacing = P0Spacing.y;
        
        Vector2 P2Spacing = P2.GetSizeAtRotation(Mathf.RoundToInt(P2.transform.eulerAngles.y));
        float p2LeftSpacing = P2Spacing.x;

        float targetSpacing = (p0RightSpacing + p2LeftSpacing + extraSpacing) * .5f;
        float P0TargetXPosition = centerPosition.x - targetSpacing;
       // spawnPoints[0].transform.position = new Vector3(P0TargetXPosition, 0,-10);
       spawnPoints[0].transform.DOMoveX(P0TargetXPosition, .1f).SetEase(Ease.Linear);

        float P2TargetXPosition = centerPosition.x + targetSpacing;
        //spawnPoints[2].transform.position = new Vector3(P2TargetXPosition, 0,-10);
        spawnPoints[2].transform.DOMoveX(P2TargetXPosition, .1f).SetEase(Ease.Linear);
        
    }

    public void CalculateP0P1P2()
    {
        
        Debug.Log(" CalculateP0P1P2 ");
        Vector2 P0Spacing = P0.GetSizeAtRotation(Mathf.RoundToInt(P0.transform.eulerAngles.y));
        float p0RightSpacing = P0Spacing.y;
        
        Vector2 P1Spacing = P1.GetSizeAtRotation(Mathf.RoundToInt(P1.transform.eulerAngles.y));
        float p1LeftSpacing = P1Spacing.x;
        float p1RightSpacing = P1Spacing.y;
        
        Vector2 P2Spacing = P2.GetSizeAtRotation(Mathf.RoundToInt(P2.transform.eulerAngles.y));
        float p2LeftSpacing = P2Spacing.x;

        float targetSpacingP0P1 = (p0RightSpacing + p1LeftSpacing + extraSpacing);
        float P0TargetXPosition = centerPosition.x - targetSpacingP0P1;
        //spawnPoints[0].transform.position = new Vector3(P0TargetXPosition, 0,-10);
        spawnPoints[0].transform.DOMoveX(P0TargetXPosition, .1f).SetEase(Ease.Linear);

       // spawnPoints[1].transform.position = centerPosition;
       spawnPoints[1].transform.DOMoveX(0, .1f).SetEase(Ease.Linear);

        float targetSpacingP1P2 = (p1RightSpacing + p2LeftSpacing + extraSpacing);
        float P2TargetXPosition = centerPosition.x + targetSpacingP1P2;
        //spawnPoints[2].transform.position = new Vector3(P2TargetXPosition, 0,-10);
        spawnPoints[2].transform.DOMoveX(P2TargetXPosition, .1f).SetEase(Ease.Linear);
    }

}