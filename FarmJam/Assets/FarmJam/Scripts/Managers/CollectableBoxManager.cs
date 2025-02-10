using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class CollectableBoxManager : MonoBehaviour
{
    [Inject] private UnitBoxManager unitBoxManager;
    public List<CollectableBox> CollectableBoxes;
}