using UnityEngine;

public class UnityBoxPoint : MonoBehaviour
{
    public Collectable Collectable;

    public void SetCollectable(Collectable collectable)
    {
        Collectable = collectable;
        Collectable.JumpToTarget(transform);
    }
}