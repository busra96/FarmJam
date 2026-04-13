using System;
using System.Collections.Generic;
using UnityEngine;

public class Box : MonoBehaviour
{
    [SerializeField] private Transform collectableRoot;
    [SerializeField] private MergeBoxParent parentBoxGroup;
    [SerializeField] private int boxIndex;
    [SerializeField] private ColorType colorType;

    public Transform CollectableRoot => collectableRoot != null ? collectableRoot : EnsureCollectableRoot();
    public MergeBoxParent ParentBoxGroup => parentBoxGroup;
    public int BoxIndex => boxIndex;
    public ColorType ColorType => colorType;

    public List<MeshRenderer> MeshRenderers = new List<MeshRenderer>();
    public List<BoxColorEntry> BoxColorEntryList = new List<BoxColorEntry>();

    private void Reset()
    {
        EnsureCollectableRoot();
    }

    public void Initialize(MergeBoxParent parent, int index, ColorType assignedColorType)
    {
        parentBoxGroup = parent;
        boxIndex = index;
        colorType = assignedColorType;
        EnsureCollectableRoot();
        ApplyColorMaterial();
    }

    private Transform EnsureCollectableRoot()
    {
        if (collectableRoot != null)
        {
            return collectableRoot;
        }

        Transform existingRoot = transform.Find("Collectables");
        if (existingRoot != null)
        {
            collectableRoot = existingRoot;
            return collectableRoot;
        }

        GameObject collectablesObject = new GameObject("Collectables");
        collectableRoot = collectablesObject.transform;
        collectableRoot.SetParent(transform, false);
        collectableRoot.localPosition = Vector3.zero;
        collectableRoot.localRotation = Quaternion.identity;
        collectableRoot.localScale = Vector3.one;
        return collectableRoot;
    }

    private void ApplyColorMaterial()
    {
        Material targetMaterial = ResolveMaterial(colorType);
        if (targetMaterial == null || MeshRenderers == null || MeshRenderers.Count == 0)
        {
            return;
        }

        for (int i = 0; i < MeshRenderers.Count; i++)
        {
            MeshRenderer meshRenderer = MeshRenderers[i];
            if (meshRenderer == null)
            {
                continue;
            }

            Material[] sharedMaterials = meshRenderer.sharedMaterials;
            if (sharedMaterials == null || sharedMaterials.Length == 0)
            {
                meshRenderer.sharedMaterial = targetMaterial;
                continue;
            }

            for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
            {
                sharedMaterials[materialIndex] = targetMaterial;
            }

            meshRenderer.sharedMaterials = sharedMaterials;
        }
    }

    private Material ResolveMaterial(ColorType targetColorType)
    {
        if (BoxColorEntryList == null)
        {
            return null;
        }

        for (int i = 0; i < BoxColorEntryList.Count; i++)
        {
            BoxColorEntry colorEntry = BoxColorEntryList[i];
            if (colorEntry == null || colorEntry.material == null || colorEntry.ColorType != targetColorType)
            {
                continue;
            }

            return colorEntry.material;
        }

        return null;
    }
}

[Serializable]
public class BoxColorEntry
{
    public ColorType ColorType;
    public Material material;
}
