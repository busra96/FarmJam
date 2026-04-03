using DG.Tweening;
using UnityEngine;

namespace FarmBlast
{
    public class CollectableMesh : MonoBehaviour
    {
        public MeshRenderer Mesh;
        public Vector3 fixedRotation = new Vector3(0, 0, 0); // Sabit bakış açısı
    
        public void Rotate()
        {
            transform.DORotate(fixedRotation, .05f).SetEase(Ease.Linear);
        }
    }
}