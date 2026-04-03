using UnityEngine;

namespace FarmBlast
{
    public class UnityBoxPoint : MonoBehaviour
    {
        [SerializeField] private UnitBox _unitBox;
        public Collectable Collectable;

        public void Init(UnitBox unitBox)
        {
            _unitBox = unitBox;
        }

        public void SetCollectable(Collectable collectable)
        {
            Collectable = collectable;
        }
    }
}
