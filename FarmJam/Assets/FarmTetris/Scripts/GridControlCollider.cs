namespace FarmTetris
{
    using UnityEngine;

    public class GridControlCollider : MonoBehaviour
    {
       public bool IsMain;
       public GridTile GridTile;
       private GridTile PreviousGridTile;
       
      private void UpdateClosestGridTile()
      {
         Collider[] colliders = Physics.OverlapSphere(transform.position, 0.2f); // 0.2f â†’ KÃ¼Ã§Ã¼k bir yarÄ±Ã§ap
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

         // EÄŸer yeni bir GridTile seÃ§ildiyse, Ã¶nceki GridTile'Ä±n rengini eski haline getir
         if (GridTile != closestTile)
         {
            if (PreviousGridTile != null)
            {
               PreviousGridTile.SetDefaultMat(); // Eski GridTile materyalini sÄ±fÄ±rla
            }

            PreviousGridTile = GridTile; // Eski GridTile'Ä± gÃ¼ncelle
            GridTile = closestTile; // Yeni GridTile'Ä± ata
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
          if(GridTile.UnitBox != null) return false;

          return true;
       }
    }
}

