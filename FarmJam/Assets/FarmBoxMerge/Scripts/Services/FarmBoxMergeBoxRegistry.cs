using System.Collections.Generic;

public interface IFarmBoxMergeBoxRegistry
{
    void Register(Box box);
    void Unregister(Box box);
    bool TryFindAvailable(ColorType colorType, out Box matchingBox);
    int CountAvailable(ColorType colorType);
}

public sealed class FarmBoxMergeBoxRegistry : IFarmBoxMergeBoxRegistry
{
    private readonly HashSet<Box> _activeBoxes = new HashSet<Box>();

    public void Register(Box box)
    {
        if (box != null) _activeBoxes.Add(box);
    }

    public void Unregister(Box box)
    {
        if (box != null) _activeBoxes.Remove(box);
    }

    public bool TryFindAvailable(ColorType colorType, out Box matchingBox)
    {
        Cleanup();
        foreach (Box box in _activeBoxes)
        {
            if (box.CanAcceptItem(colorType))
            {
                matchingBox = box;
                return true;
            }
        }

        matchingBox = null;
        return false;
    }

    public int CountAvailable(ColorType colorType)
    {
        Cleanup();
        int count = 0;
        foreach (Box box in _activeBoxes)
        {
            if (box.CanAcceptItem(colorType)) count++;
        }

        return count;
    }

    private void Cleanup()
    {
        _activeBoxes.RemoveWhere(box => box == null);
    }
}
