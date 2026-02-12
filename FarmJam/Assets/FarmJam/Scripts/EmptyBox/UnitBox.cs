using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Signals;
using UnityEngine;

public class UnitBox : MonoBehaviour
{
   private const float DESTROY_DURATION = 0.2f;

   public bool IsFull;
   public GameObject UnitBoxModel;
   private GridTile GridTile;
   private List<GridTile> GridTiles = new List<GridTile>();
   [SerializeField] private List<GridControlCollider> GridControlColliders;

   public List<UnityBoxPoint> Points;
   private bool onDestroyed = false;

   public UnitBoxColorTypeAndMat UnitBoxColorTypeAndMat;

   public UnitBoxAudio UnitBoxAudio;

   public void Init(ColorType colorType)
   {
      foreach (var point in Points)
         point.Init(this);

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

      foreach (var collider in GridControlColliders)
      {
         GridTiles.Add(collider.GridTile);
         collider.GridTile.UnitBox = this;
      }

      GridTileSignals.OnAddedUnitBox?.Dispatch(this);
      LevelManagerSignals.OnLevelWinFailCheckTimerRestart?.Dispatch();
   }

   public void CheckIsFull()
   {
      if (onDestroyed)
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
      UnitBoxAudio.PlayWinClip();
      UnitBoxSignals.OnThisUnitBoxDestroyed?.Dispatch(this);
      UnitBoxModel.transform.DOScale(Vector3.zero, DESTROY_DURATION)
         .SetEase(Ease.Linear)
         .OnComplete(() => Destroy(gameObject));
   }

   public UnityBoxPoint GetEmptyBoxPoint()
   {
      foreach (var unitBoxPoint in Points)
      {
         if (unitBoxPoint.Collectable == null)
            return unitBoxPoint;
      }

      return null;
   }
}
