using System;
using System.Collections.Generic;
using Signals;
using UnityEngine;

public class UnitBox : MonoBehaviour
{
   public bool IsFull;
   public GameObject UnitBoxModel;
   private GridTile GridTile;
   private List<GridTile> GridTiles = new List<GridTile>();
   [SerializeField] private List<GridControlCollider> GridControlColliders;

   public List<UnityBoxPoint> Points;

   private void Start()
   {
      for (int i = 0; i < Points.Count; i++)
         Points[i].Init(this);
      
      UnitBoxSignals.OnThisUnitBoxIsFullCheck.AddListener(CheckIsFull);
   }

   private void OnDisable()
   {
      UnitBoxSignals.OnThisUnitBoxIsFullCheck.RemoveListener(CheckIsFull);
   }

   public void JumpToGridTile(GridTile tile)
   {
      GridTile = tile;
      transform.SetParent(GridTile.transform);
      transform.localPosition = Vector3.zero;

      for (int i = 0; i < GridControlColliders.Count; i++)
      {
         GridTiles.Add(GridControlColliders[i].GridTile);
         GridControlColliders[i].GridTile.UnitBox = this;
      }
      
      GridTileSignals.OnAddedUnitBox?.Dispatch(this);
   }

   public void CheckIsFull()
   {
      bool isFull = true;
      foreach (var point in Points)
      {
         if (point.Collectable == null || point.Collectable.isJumping)
         {
            isFull = false;
            break;
         }
      }

      if (isFull)
      {
         UnitBoxSignals.OnThisUnitBoxDestroyed?.Dispatch(this);
         Destroy( gameObject);
      }
   }
}
