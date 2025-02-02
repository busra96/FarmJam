using UnityEngine;

public class RaycastControl : MonoBehaviour
{
    public bool isMain;
    
    public Vector3 boxSize = new Vector3(0.9f, 0.9f, 0.9f); // BoxCast boyutu
    public float maxDistance = 10.0f; // BoxCast'in aşağıya doğru maksimum mesafesi
    public LayerMask collisionLayers; // Çarpışmayı kontrol etmek istediğimiz katmanlar

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
            //     hitResult = false; // EmptyBox tagine çarparsa false döndür
            //     gizmoColor = Color.red; // Gizmos rengi kırmızı olsun
            // }
            if (hit.collider.CompareTag("GridTile"))
            {
                hitResult = true;
                var _targetGridTile = hit.collider.gameObject.GetComponent<GridTile>(); // GridTile tagine çarparsa true döndür
                if (_targetGridTile != GridTile)
                {
                    if(GridTile !=null) GridTile.SetDefaultMat();
                    GridTile = _targetGridTile;
                }
                
                if(GridTile.EmptyBox == null)
                    gizmoColor = Color.green; // Gizmos rengi yeşil olsun
                else 
                    gizmoColor = Color.red; // Gizmos rengi yeşil olsun
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
            gizmoColor = Color.red; // Çarpışma yoksa rengi kırmızı tut
        }
    }

    private void OnDrawGizmos()
    {
        RaycastHit hit;
        bool _hasHit = Physics.BoxCast(transform.position, boxSize / 2, Vector3.down, out hit, Quaternion.identity, maxDistance, collisionLayers);
        if (_hasHit)
        {
            Gizmos.color = gizmoColor; // Çarpışmaya göre belirlenen renk (kırmızı/yeşil)
            Gizmos.DrawRay(transform.position, Vector3.down * hit.distance);
            Gizmos.DrawWireCube(transform.position + Vector3.down * hit.distance, boxSize); // Çarpışma noktasında bir kutu çiz
        }
        else
        {
            
            Gizmos.color = Color.yellow; // BoxCast tarama alanı için sarı renk kullan
            Gizmos.DrawRay(transform.position, Vector3.down * maxDistance);
        }
    }

    public bool GetHasHit() => hasHit;
}