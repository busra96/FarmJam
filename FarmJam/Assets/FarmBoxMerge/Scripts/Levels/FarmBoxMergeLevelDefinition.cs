using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class FarmBoxMergeItemRun
{
    public ColorType colorType = ColorType.Green;
    [Min(1)] public int count = 1;
}

[Serializable]
public class FarmBoxMergeCardSpawnGroup
{
    public ColorType colorType = ColorType.Green;
    [FormerlySerializedAs("counter")]
    [Min(1)] public int count = 1;
}

[CreateAssetMenu(fileName = "FarmBoxMergeLevel", menuName = "FarmBoxMerge/Level")]
public class FarmBoxMergeLevelDefinition : ScriptableObject
{
    [SerializeField, HideInInspector] private string levelId;
    [SerializeField] private string levelName = "New Level";
    [SerializeField, TextArea(2, 5)] private string designerNotes;
    [SerializeField] private List<FarmBoxMergeItemRun> itemSequence = new List<FarmBoxMergeItemRun>();
    [FormerlySerializedAs("startingCards")]
    [SerializeField] private List<FarmBoxMergeCardSpawnGroup> cardSpawnPlan = new List<FarmBoxMergeCardSpawnGroup>();
    [SerializeField, HideInInspector] private int cardPlanVersion;

    public string LevelId => levelId;
    public string LevelName => string.IsNullOrWhiteSpace(levelName) ? name : levelName;
    public string DesignerNotes => designerNotes;
    public IReadOnlyList<FarmBoxMergeItemRun> ItemSequence => itemSequence;
    public IReadOnlyList<FarmBoxMergeCardSpawnGroup> CardSpawnPlan => cardSpawnPlan;

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

    public int TotalCardSpawnCount
    {
        get
        {
            int total = 0;
            for (int i = 0; i < cardSpawnPlan.Count; i++)
            {
                if (cardSpawnPlan[i] != null)
                {
                    total += Mathf.Max(1, cardSpawnPlan[i].count);
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

        for (int i = 0; i < cardSpawnPlan.Count; i++)
        {
            if (cardSpawnPlan[i] != null)
            {
                cardSpawnPlan[i].count = cardPlanVersion <= 0
                    ? FarmBoxMergeRules.ClampCardCounter(cardSpawnPlan[i].count)
                    : Mathf.Max(1, cardSpawnPlan[i].count);
            }
        }
    }

    public void Initialize(string displayName)
    {
        levelName = string.IsNullOrWhiteSpace(displayName) ? "New Level" : displayName.Trim();
        cardPlanVersion = 1;
        EnsureLevelId();
    }

    public bool UpgradeLegacyCardPlan()
    {
        bool changed = cardPlanVersion < 1;

        if (cardPlanVersion < 1)
        {
            for (int i = 0; i < cardSpawnPlan.Count; i++)
            {
                FarmBoxMergeCardSpawnGroup group = cardSpawnPlan[i];
                if (group == null)
                {
                    continue;
                }

                int legacyCounter = FarmBoxMergeRules.ClampCardCounter(group.count);
                group.count = 1 << (legacyCounter - FarmBoxMergeRules.MinCardCounter);
            }
        }

        // Deck order is randomized at runtime, so repeated rows of the same
        // color carry no extra meaning. Store one compact total per color.
        Dictionary<ColorType, int> totals = new Dictionary<ColorType, int>();
        List<ColorType> colorOrder = new List<ColorType>();
        for (int i = 0; i < cardSpawnPlan.Count; i++)
        {
            FarmBoxMergeCardSpawnGroup group = cardSpawnPlan[i];
            if (group == null)
            {
                changed = true;
                continue;
            }

            if (!totals.ContainsKey(group.colorType))
            {
                totals.Add(group.colorType, 0);
                colorOrder.Add(group.colorType);
            }
            else
            {
                changed = true;
            }

            totals[group.colorType] += Mathf.Max(1, group.count);
        }

        if (changed)
        {
            cardSpawnPlan.Clear();
            for (int i = 0; i < colorOrder.Count; i++)
            {
                ColorType colorType = colorOrder[i];
                cardSpawnPlan.Add(new FarmBoxMergeCardSpawnGroup
                {
                    colorType = colorType,
                    count = totals[colorType]
                });
            }
        }

        cardPlanVersion = 1;
        return changed;
    }

    private void EnsureLevelId()
    {
        if (string.IsNullOrWhiteSpace(levelId))
        {
            levelId = Guid.NewGuid().ToString("N");
        }
    }
}
