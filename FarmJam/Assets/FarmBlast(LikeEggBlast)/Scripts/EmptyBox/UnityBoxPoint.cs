using UnityEngine;

namespace FarmBlast
{
    public class UnityBoxPoint : MonoBehaviour
    {
        [SerializeField] private UnitBox _unitBox;
        public Collectable Collectable;

        public void SetCollectable(Collectable collectable)
        {
            Collectable = collectable;
        }
    }
}
