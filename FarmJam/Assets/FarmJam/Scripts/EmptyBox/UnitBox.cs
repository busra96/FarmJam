using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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
   private bool onDestroyed = false;

   public UnitBoxColorTypeAndMat UnitBoxColorTypeAndMat;

   public void Init(ColorType colorType)
   {
      for (int i = 0; i < Points.Count; i++)
         Points[i].Init(this);

      UnitBoxColorTypeAndMat.ColorType = colorType;
      UnitBoxColorTypeAndMat.ActiveColor();
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
      if(onDestroyed)
         return;
      
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
         onDestroyed = true;
         DestroyAnim();
      }
   }

   private async UniTask DestroyAnim()
   {
      UnitBoxSignals.OnThisUnitBoxDestroyed?.Dispatch(this);
      UnitBoxModel.transform.DOScale(Vector3.zero, .25f).SetEase(Ease.OutBounce).OnComplete(() => Destroy(gameObject));
   }

   public UnityBoxPoint GetEmptyBoxPoint()
   {
      foreach (var unitBoxPoint in Points)
         if(unitBoxPoint.Collectable == null) return unitBoxPoint;

      return null;
   }
}
