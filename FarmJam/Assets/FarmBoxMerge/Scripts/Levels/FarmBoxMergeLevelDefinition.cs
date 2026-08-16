using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FarmBoxMergeItemRun
{
    public ColorType colorType = ColorType.Green;
    [Min(1)] public int count = 1;
}

[Serializable]
public class FarmBoxMergeStartingCard
{
    public ColorType colorType = ColorType.Green;
    [Range(FarmBoxMergeRules.MinCardCounter, FarmBoxMergeRules.MaxCardCounter)]
    public int counter = FarmBoxMergeRules.MinCardCounter;
}

[CreateAssetMenu(fileName = "FarmBoxMergeLevel", menuName = "FarmBoxMerge/Level")]
public class FarmBoxMergeLevelDefinition : ScriptableObject
{
    [SerializeField, HideInInspector] private string levelId;
    [SerializeField] private string levelName = "New Level";
    [SerializeField, TextArea(2, 5)] private string designerNotes;
    [SerializeField] private List<FarmBoxMergeItemRun> itemSequence = new List<FarmBoxMergeItemRun>();
    [SerializeField] private List<FarmBoxMergeStartingCard> startingCards = new List<FarmBoxMergeStartingCard>();

    public string LevelId => levelId;
    public string LevelName => string.IsNullOrWhiteSpace(levelName) ? name : levelName;
    public string DesignerNotes => designerNotes;
    public IReadOnlyList<FarmBoxMergeItemRun> ItemSequence => itemSequence;
    public IReadOnlyList<FarmBoxMergeStartingCard> StartingCards => startingCards;

    public int TotalItemCount
    {
        get
        {
            int total = 0;
            for (int i = 0; i < itemSequence.Count; i++)
            {
                if (itemSequence[i] != null)
                {
                    total += Mathf.Max(1, itemSequence[i].count);
                }
            }

            return total;
        }
    }

    public int StartingCardCapacity
    {
        get
        {
            int total = 0;
            for (int i = 0; i < startingCards.Count; i++)
            {
                if (startingCards[i] != null)
                {
                    total += FarmBoxMergeRules.ClampCardCounter(startingCards[i].counter);
                }
            }

            return total;
        }
    }

    private void OnValidate()
    {
        EnsureLevelId();
        levelName = string.IsNullOrWhiteSpace(levelName) ? name : levelName.Trim();

        for (int i = 0; i < itemSequence.Count; i++)
        {
            if (itemSequence[i] != null)
            {
                itemSequence[i].count = Mathf.Max(1, itemSequence[i].count);
            }
        }

        for (int i = 0; i < startingCards.Count; i++)
        {
            if (startingCards[i] != null)
            {
                startingCards[i].counter = FarmBoxMergeRules.ClampCardCounter(startingCards[i].counter);
            }
        }
    }

    public void Initialize(string displayName)
    {
        levelName = string.IsNullOrWhiteSpace(displayName) ? "New Level" : displayName.Trim();
        EnsureLevelId();
    }

    private void EnsureLevelId()
    {
        if (string.IsNullOrWhiteSpace(levelId))
        {
            levelId = Guid.NewGuid().ToString("N");
        }
    }
}
