using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EmptyBoxContainer", menuName = "Container/EmptyBoxContainer")]
public class EmptyBoxContainer : ScriptableObject
{
    public List<EmptyBoxContainerParameters> EmptyBoxContainerList;

    public EmptyBox ReturnEmptyBox(EmptyBoxType EmptyBoxType)
    {
        return EmptyBoxContainerList.Find(parameter => parameter.EmptyBoxType == EmptyBoxType)?.EmptyBox;
    }
}

[Serializable]
public class EmptyBoxContainerParameters
{
    public EmptyBoxType EmptyBoxType;
    public EmptyBox EmptyBox;
}