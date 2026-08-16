using UnityEngine;

public static class FarmBoxMergeObjectUtility
{
    public static void Destroy(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            target.SetActive(false);
            Object.Destroy(target);
            return;
        }

        Object.DestroyImmediate(target);
    }

    public static T FindSceneComponent<T>() where T : Object
    {
        return Object.FindFirstObjectByType<T>();
    }
}
