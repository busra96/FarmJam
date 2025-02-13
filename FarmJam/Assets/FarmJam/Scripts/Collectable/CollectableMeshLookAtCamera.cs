using UnityEngine;

public class CollectableMeshLookAtCamera : MonoBehaviour
{
       public Camera mainCamera; // Kamerayı dışarıdan atayabilirsin ya da otomatik aldırabilirsin.
       public Vector3 fixedRotation = new Vector3(0, 0, 0); // Sabit bakış açısı
    
        void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
        }
    
        void LateUpdate()
        {
            if (mainCamera == null) return;
            
            // Kameraya bakma işlemi (world space)
            transform.LookAt(transform.position + mainCamera.transform.forward);
            
            // Sabit bir rotasyon açısı ayarla
            transform.rotation = Quaternion.Euler(fixedRotation);
        }
}
