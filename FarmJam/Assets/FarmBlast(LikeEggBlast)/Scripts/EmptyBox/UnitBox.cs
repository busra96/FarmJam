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
      private const float INITIAL_SCALE = 0.7f;

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
         transform.SetParent(GridTile.transform);
         transform.localPosition = Vector3.zero;

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

      public UnityBoxPoint GetEmptyBoxPoint() => Points.Find(point => point.Collectable == null);
   }
}


