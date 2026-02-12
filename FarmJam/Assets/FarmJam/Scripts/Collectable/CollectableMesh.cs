using DG.Tweening;
using UnityEngine;

public class CollectableMesh : MonoBehaviour
{
       public MeshRenderer Mesh;
      // public Camera mainCamera; 
       public Vector3 fixedRotation = new Vector3(0, 0, 0); // Sabit bakış açısı
    
        // void Start()
        // {
        //     if (mainCamera == null) mainCamera = Camera.main;
        // }
    
        // void LateUpdate()
        // {
        //     if (mainCamera == null) return;
        //     
        //     // Kameraya bakma işlemi (world space)
        //     transform.LookAt(transform.position + mainCamera.transform.forward);
        //     
        //     // Sabit bir rotasyon açısı ayarla
        //     transform.rotation = Quaternion.Euler(fixedRotation);
        // }


        public void Rotate()
        {
            transform.DORotate(fixedRotation, .05f).SetEase(Ease.Linear);
        }
}
