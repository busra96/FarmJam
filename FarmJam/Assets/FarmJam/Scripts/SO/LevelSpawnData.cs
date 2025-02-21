using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelSpawnData", menuName = "LevelSpawnData")]
public class LevelSpawnData : ScriptableObject
{
    [Serializable]
    public struct  EmptyBoxParameter
    {
        public ColorType ColorType;
        public EmptyBox EmptyBox;
    }
    
    public List<EmptyBoxParameter> EmptyBoxTypes;
    public CollectableBoxParent CollectableBoxParent;
}