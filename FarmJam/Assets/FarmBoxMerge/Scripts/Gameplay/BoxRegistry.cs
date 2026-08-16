using System.Collections.Generic;

public static class BoxRegistry
{
    private static readonly HashSet<Box> ActiveBoxes = new HashSet<Box>();

    public static void Register(Box box)
    {
        if (box != null)
        {
            ActiveBoxes.Add(box);
        }
    }

    public static void Unregister(Box box)
    {
        if (box != null)
        {
            ActiveBoxes.Remove(box);
        }
    }

    public static bool TryFindAvailable(ColorType colorType, out Box matchingBox)
    {
        ActiveBoxes.RemoveWhere(box => box == null);

        foreach (Box box in ActiveBoxes)
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
}
