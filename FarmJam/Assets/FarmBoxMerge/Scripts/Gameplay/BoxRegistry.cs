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
        CleanupDestroyedBoxes();

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

    public static int CountAvailable(ColorType colorType)
    {
        CleanupDestroyedBoxes();

        int count = 0;
        foreach (Box box in ActiveBoxes)
        {
            if (box.CanAcceptItem(colorType))
            {
                count++;
            }
        }

        return count;
    }

    private static void CleanupDestroyedBoxes()
    {
        ActiveBoxes.RemoveWhere(box => box == null);
    }
}
