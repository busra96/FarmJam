using Signals;
using UnityEngine;

public class GridControlCollider : MonoBehaviour
{
   public bool IsMain;
   public GridTile GridTile;
   
   public void OnTriggerEnter(Collider other)
   {
      if (other.tag == "GridTile")
      {
         GridTile gridTile = other.GetComponent<GridTile>();

         if (GridTile != null && GridTile.GridControlCollider != null && GridTile.GridControlCollider == this)
         {
            GridTile.GridControlCollider = null;
            GridTile.SetDefaultMat();
         }
         
         GridTile = gridTile;
         GridTile.GridControlCollider = this;
      }
   }

   public void OnTriggerExit(Collider other)
   {
      if (other.tag == "GridTile")
      {
         GridTile gridTile = other.GetComponent<GridTile>();

         if (gridTile.GridControlCollider == this)
         {
            gridTile.GridControlCollider = null;
         }
      }
      GridTileSignals.OnGridTileMaterialColorCheck?.Dispatch();
      
      
   }

   public bool ReturnOnGridTileIsAvailable()
   {
      if (GridTile == null) return false;

      if (GridTile.GridControlCollider != this) return false;

      return true;
   }
}