using UnityEngine;

public class UnitBox : MonoBehaviour
{
   private GridTile GridTile;
    
   public void JumpToGridTile(GridTile tile)
   {
      GridTile = tile;
      transform.SetParent(GridTile.transform);
      transform.localPosition = Vector3.zero;
   }
}
