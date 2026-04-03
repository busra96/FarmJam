using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Signals;
using UnityEngine;


namespace FarmBlast
{
   public class UnitBox : MonoBehaviour
   {
      private const float DESTROY_DURATION = 0.2f;
      private const float INITIAL_SCALE = 1f;

      public bool IsFull;
      private bool onDestroyed = false;
      
      private GridTile GridTile;
      private List<GridTile> GridTiles = new List<GridTile>();
      [SerializeField] private List<GridControlCollider> GridControlColliders;

      public GameObject UnitBoxModel;
      public List<UnityBoxPoint> Points;
      public UnitBoxColorTypeAndMat UnitBoxColorTypeAndMat;
      public UnitBoxAudio UnitBoxAudio;

      public IReadOnlyList<GridControlCollider> Colliders => GridControlColliders;
      public ColorType ColorType => UnitBoxColorTypeAndMat != null ? UnitBoxColorTypeAndMat.ColorType : default;

      public void Init(ColorType colorType)
      {
         UnitBoxColorTypeAndMat.ColorType = colorType;
         UnitBoxColorTypeAndMat.ActiveColor();
      }

      private void OnDisable()
      {
      }
      
      public void JumpToGridTile(GridTile tile)
      {
         GridTiles.Clear();
         GridTile = tile;
         transform.SetParent(GridTile.transform, false);
         transform.localPosition = Vector3.zero;
         transform.localRotation = Quaternion.identity;
         transform.localScale = Vector3.one * INITIAL_SCALE;

         foreach (var collider in GridControlColliders)
         {
            if (collider == null || collider.GridTile == null)
            {
               continue;
            }

            GridTiles.Add(collider.GridTile);
            collider.GridTile.UnitBox = this;
         }

        // GridTileSignals.OnAddedUnitBox?.Dispatch(this);
      }

      public GridControlCollider GetMainGridControlCollider()
      {
         return GridControlColliders.Find(collider => collider != null && collider.IsMain);
      }

      public void PopFromGrid()
      {
         if (onDestroyed)
         {
            return;
         }

         onDestroyed = true;
         ClearGridReferences();

         if (UnitBoxAudio != null)
         {
            UnitBoxAudio.PlayWinClip();
         }

         transform.DOScale(Vector3.zero, DESTROY_DURATION)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
               if (this != null)
               {
                  Destroy(gameObject);
               }
            });
      }

      private void ClearGridReferences()
      {
         for (int i = 0; i < GridTiles.Count; i++)
         {
            GridTile occupiedTile = GridTiles[i];
            if (occupiedTile == null)
            {
               continue;
            }

            if (occupiedTile.UnitBox == this)
            {
               occupiedTile.UnitBox = null;
            }

            occupiedTile.SetDefaultMat();
         }

         for (int i = 0; i < GridControlColliders.Count; i++)
         {
            if (GridControlColliders[i] != null)
            {
               GridControlColliders[i].GridTile = null;
            }
         }

         GridTiles.Clear();
         GridTile = null;
      }

      public UnityBoxPoint GetEmptyBoxPoint() => Points.Find(point => point.Collectable == null);
   }
}


