using Signals;
using UnityEngine;

public class GridControlCollider : MonoBehaviour
{
   public bool IsMain;
   public GridTile GridTile;
   private GridTile PreviousGridTile;
   
  private void UpdateClosestGridTile()
  {
     Collider[] colliders = Physics.OverlapSphere(transform.position, 0.2f); // 0.2f → Küçük bir yarıçap
     GridTile closestTile = null;
     float minDistance = Mathf.Infinity;

     foreach (var col in colliders)
     {
        if (col.CompareTag("GridTile"))
        {
           GridTile tile = col.GetComponent<GridTile>();
           float distance = Vector3.Distance(transform.position, tile.transform.position);
            
           if (distance < minDistance)
           {
              minDistance = distance;
              closestTile = tile;
           }
        }
     }

     // Eğer yeni bir GridTile seçildiyse, önceki GridTile'ın rengini eski haline getir
     if (GridTile != closestTile)
     {
        if (PreviousGridTile != null)
        {
           PreviousGridTile.SetDefaultMat(); // Eski GridTile materyalini sıfırla
        }

        PreviousGridTile = GridTile; // Eski GridTile'ı güncelle
        GridTile = closestTile; // Yeni GridTile'ı ata
     }

     if ( PreviousGridTile != null && GridTile != PreviousGridTile)
     {
        PreviousGridTile.SetDefaultMat();
        PreviousGridTile = null;
     }
  }
  
  private void Update()
  {
     UpdateClosestGridTile();
  }

   public bool ReturnOnGridTileIsAvailable()
   {
      if (GridTile == null) return false;
      if(GridTile.GridControlCollider != null) return false;

      return true;
   }
}