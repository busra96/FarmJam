using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MergeItem : MonoBehaviour
{
    [SerializeField] private ColorType colorType = ColorType.Green;
    [SerializeField] private List<ItemColorMeshEntry> colorMeshEntries = new List<ItemColorMeshEntry>();

    private MergeItemSpawner _owner;

    public ColorType ColorType => colorType;
    public IReadOnlyList<ItemColorMeshEntry> ColorMeshEntries => colorMeshEntries;

    private void Awake()
    {
        RefreshVisuals();
    }

    private void OnDestroy()
    {
        if (_owner != null)
        {
            _owner.UnregisterActiveItem(this);
        }
    }

    private void OnValidate()
    {
        RefreshVisuals();
    }

    public void Initialize(ColorType assignedColorType)
    {
        SetColorType(assignedColorType);
    }

    public void AssignSpawner(MergeItemSpawner owner)
    {
        _owner = owner;
    }

    public void SetColorType(ColorType assignedColorType)
    {
        colorType = assignedColorType;
        RefreshVisuals();
    }

    public void RefreshVisuals()
    {
        if (colorMeshEntries == null)
        {
            return;
        }

        for (int i = 0; i < colorMeshEntries.Count; i++)
        {
            ItemColorMeshEntry colorMeshEntry = colorMeshEntries[i];
            if (colorMeshEntry == null || colorMeshEntry.meshObject == null)
            {
                continue;
            }

            colorMeshEntry.meshObject.SetActive(colorMeshEntry.colorType == colorType);
        }
    }

    public float GetVisualBottomLocalY()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(false);
        float bottom = float.PositiveInfinity;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !TryGetRendererLocalBounds(renderer, out Bounds rendererBounds))
            {
                continue;
            }

            Matrix4x4 rendererToItem = transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            for (int cornerIndex = 0; cornerIndex < 8; cornerIndex++)
            {
                Vector3 corner = new Vector3(
                    (cornerIndex & 1) == 0 ? rendererBounds.min.x : rendererBounds.max.x,
                    (cornerIndex & 2) == 0 ? rendererBounds.min.y : rendererBounds.max.y,
                    (cornerIndex & 4) == 0 ? rendererBounds.min.z : rendererBounds.max.z);
                bottom = Mathf.Min(bottom, rendererToItem.MultiplyPoint3x4(corner).y);
            }
        }

        return float.IsPositiveInfinity(bottom) ? 0f : bottom;
    }

    private static bool TryGetRendererLocalBounds(Renderer renderer, out Bounds bounds)
    {
        if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
        {
            bounds = skinnedMeshRenderer.localBounds;
            return true;
        }

        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            bounds = meshFilter.sharedMesh.bounds;
            return true;
        }

        bounds = default;
        return false;
    }
}

[Serializable]
public class ItemColorMeshEntry
{
    public ColorType colorType;
    public GameObject meshObject;
}
