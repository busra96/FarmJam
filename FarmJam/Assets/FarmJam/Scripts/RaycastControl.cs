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

    void Update()
    {
        // BoxCast için başlangıç pozisyonu
        Vector3 origin = transform.position;
        
        // BoxCast'in aşağı yönlü taraması
        RaycastHit hit;
        hasHit = Physics.BoxCast(origin, boxSize / 2, Vector3.down, out hit, Quaternion.identity, maxDistance, collisionLayers);
        
        // Eğer bir çarpışma olduysa
        if (hasHit)
        {
            // Çarpışan objenin tag'ini kontrol et
            if (hit.collider.CompareTag("EmptyBox"))
            {
                hitResult = false; // EmptyBox tagine çarparsa false döndür
                gizmoColor = Color.red; // Gizmos rengi kırmızı olsun
            }
            else if (hit.collider.CompareTag("GridTile"))
            {
                hitResult = true; // GridTile tagine çarparsa true döndür
                gizmoColor = Color.green; // Gizmos rengi yeşil olsun
            }
        }
        else
        {
            // Çarpışma yoksa sonuç varsayılan olarak false
            hitResult = false;
            gizmoColor = Color.red; // Çarpışma yoksa rengi kırmızı tut
        }
    }

    // Gizmos ile çarpışma kutusunu göster
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
            
            // BoxCast'in başlangıç noktasını ve yönünü görselleştirmek için
            Gizmos.color = Color.yellow; // BoxCast tarama alanı için sarı renk kullan
            Gizmos.DrawRay(transform.position, Vector3.down * maxDistance);
        }
    }
}