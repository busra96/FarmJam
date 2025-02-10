using UnityEngine;

public class UnityBoxPoint : MonoBehaviour
{
    private UnitBox _unitBox;
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