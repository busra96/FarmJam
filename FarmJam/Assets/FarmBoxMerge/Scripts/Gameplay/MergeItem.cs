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
}

[Serializable]
public class ItemColorMeshEntry
{
    public ColorType colorType;
    public GameObject meshObject;
}
