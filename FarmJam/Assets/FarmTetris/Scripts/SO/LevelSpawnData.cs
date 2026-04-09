namespace FarmTetris
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "LevelSpawnData", menuName = "FarmTetrisLevelSpawnData")]
    public class FarmTetrisLevelSpawnData : ScriptableObject
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
}

