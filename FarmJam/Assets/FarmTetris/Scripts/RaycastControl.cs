namespace FarmTetris
{
    using UnityEngine;

    public class RaycastControl : MonoBehaviour
    {
        public bool isMain;
        
        public Vector3 boxSize = new Vector3(0.9f, 0.9f, 0.9f); // BoxCast boyutu
        public float maxDistance = 10.0f; // BoxCast'in aÅŸaÄŸÄ±ya doÄŸru maksimum mesafesi
        public LayerMask collisionLayers; // Ã‡arpÄ±ÅŸmayÄ± kontrol etmek istediÄŸimiz katmanlar

        private bool hasHit; 
        private bool hitResult; 
        private Color gizmoColor = Color.red;

        public GridTile GridTile;

        void Update()
        {
            Vector3 origin = transform.position;
            
            RaycastHit hit;
            hasHit = Physics.BoxCast(origin, boxSize / 2, Vector3.down, out hit, Quaternion.identity, maxDistance, collisionLayers);
            
            if (hasHit)
            {
                // if (hit.collider.CompareTag("EmptyBox"))
                // {
                //     hitResult = false; // EmptyBox tagine Ã§arparsa false dÃ¶ndÃ¼r
                //     gizmoColor = Color.red; // Gizmos rengi kÄ±rmÄ±zÄ± olsun
                // }
                if (hit.collider.CompareTag("GridTile"))
                {
                    hitResult = true;
                    var _targetGridTile = hit.collider.gameObject.GetComponent<GridTile>(); // GridTile tagine Ã§arparsa true dÃ¶ndÃ¼r
                    if (_targetGridTile != GridTile)
                    {
                        if(GridTile !=null) GridTile.SetDefaultMat();
                        GridTile = _targetGridTile;
                    }
                    
                    if(GridTile.UnitBox == null)
                        gizmoColor = Color.green; // Gizmos rengi yeÅŸil olsun
                    else 
                        gizmoColor = Color.red; // Gizmos rengi yeÅŸil olsun
                }
                else
                {
                    if(GridTile != null) GridTile.SetDefaultMat();
                    GridTile = null;
                }
            }
            else
            {
                if(GridTile != null) GridTile.SetDefaultMat();
                GridTile = null;
                hitResult = false;
                gizmoColor = Color.red; // Ã‡arpÄ±ÅŸma yoksa rengi kÄ±rmÄ±zÄ± tut
            }
        }

        private void OnDrawGizmos()
        {
            RaycastHit hit;
            bool _hasHit = Physics.BoxCast(transform.position, boxSize / 2, Vector3.down, out hit, Quaternion.identity, maxDistance, collisionLayers);
            if (_hasHit)
            {
                Gizmos.color = gizmoColor; // Ã‡arpÄ±ÅŸmaya gÃ¶re belirlenen renk (kÄ±rmÄ±zÄ±/yeÅŸil)
                Gizmos.DrawRay(transform.position, Vector3.down * hit.distance);
                Gizmos.DrawWireCube(transform.position + Vector3.down * hit.distance, boxSize); // Ã‡arpÄ±ÅŸma noktasÄ±nda bir kutu Ã§iz
            }
            else
            {
                
                Gizmos.color = Color.yellow; // BoxCast tarama alanÄ± iÃ§in sarÄ± renk kullan
                Gizmos.DrawRay(transform.position, Vector3.down * maxDistance);
            }
        }

        public bool GetHasHit() => hasHit;
    }
}

