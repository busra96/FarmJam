using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public bool HasEmptyBox;
    public EmptyBox EmptyBox;

    public void SetEmptyBox(EmptyBox emptyBox)
    {
        HasEmptyBox = true;
        EmptyBox = emptyBox;
    }

    public void RemoveEmptyBox()
    {
        HasEmptyBox = false;
        EmptyBox = null;
    }
}