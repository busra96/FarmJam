namespace FarmTetris
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "EmptyBoxContainer", menuName = "Container/FarmTetris/EmptyBoxContainer")]
    public class EmptyBoxContainer : ScriptableObject
    {
        public List<EmptyBoxContainerParameters> EmptyBoxContainerList;

        public EmptyBox ReturnEmptyBox(EmptyBoxType EmptyBoxType)
        {
            if (EmptyBoxContainerList == null || EmptyBoxContainerList.Count == 0)
            {
                return null;
            }

            EmptyBox matchedEmptyBox = EmptyBoxContainerList.Find(parameter => parameter.EmptyBoxType == EmptyBoxType)?.EmptyBox;
            if (matchedEmptyBox != null)
            {
                return matchedEmptyBox;
            }

            return EmptyBoxContainerList[0].EmptyBox;
        }
    }

    [Serializable]
    public class EmptyBoxContainerParameters
    {
        public EmptyBoxType EmptyBoxType;
        public EmptyBox EmptyBox;
    }
}

