using System.Collections.Generic;
using UnityEngine;

public class UnitBox : MonoBehaviour
{
   public GameObject UnitBoxModel;
   private GridTile GridTile;
   private List<GridTile> GridTiles = new List<GridTile>();
   [SerializeField] private List<GridControlCollider> GridControlColliders;
    
   public void JumpToGridTile(GridTile tile)
   {
      GridTile = tile;
      transform.SetParent(GridTile.transform);
      transform.localPosition = Vector3.zero;
      UnitBoxModel.transform.localScale = Vector3.one * .98f;

      for (int i = 0; i < GridControlColliders.Count; i++)
      {
         GridTiles.Add(GridControlColliders[i].GridTile);
         GridControlColliders[i].GridTile.UnitBox = this;
      }
   }
}
