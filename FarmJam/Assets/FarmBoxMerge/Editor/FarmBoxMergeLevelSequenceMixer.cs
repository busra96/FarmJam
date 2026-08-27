using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class FarmBoxMergeLevelSequenceMixer
{
    private const string LevelFolder = "Assets/FarmBoxMerge/Levels";
    private const int DefaultSeed = 20260826;
    private const int WorldSlotCount = 3;

    private sealed class ScheduledBoxGroup
    {
        public ColorType ColorType;
        public int BoxSize;
        public int RemainingItems;
    }

    [MenuItem("Tools/FarmBoxMerge/Upgrade Level-One Card Spawn Plans")]
    public static void UpgradeLevelOneCardSpawnPlans()
    {
        string[] levelGuids = AssetDatabase.FindAssets(
            "t:FarmBoxMergeLevelDefinition",
            new[] { LevelFolder });
        int upgradedCount = 0;

        for (int i = 0; i < levelGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(levelGuids[i]);
            FarmBoxMergeLevelDefinition level =
                AssetDatabase.LoadAssetAtPath<FarmBoxMergeLevelDefinition>(path);
            if (level == null || !level.UpgradeLegacyCardPlan())
            {
                continue;
            }

            EditorUtility.SetDirty(level);
            upgradedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"FBM_CARD_PLAN_UPGRADE_COMPLETE upgraded={upgradedCount}");
    }

    [MenuItem("Tools/FarmBoxMerge/Mix All Level Item Flows")]
    public static void MixAllLevelItemFlows()
    {
        RebuildAllLevelDesigns();
    }

    [MenuItem("Tools/FarmBoxMerge/Rebuild All Deterministic Slot Flows")]
    public static void RebuildAllDeterministicSlotFlows()
    {
        RebuildAllLevelDesigns();
    }

    private static void RebuildAllLevelDesigns()
    {
        string[] levelGuids = AssetDatabase.FindAssets(
            "t:FarmBoxMergeLevelDefinition",
            new[] { LevelFolder });
        Array.Sort(levelGuids, CompareAssetPaths);

        int changedLevelCount = 0;
        int invalidLevelCount = 0;
        for (int i = 0; i < levelGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(levelGuids[i]);
            FarmBoxMergeLevelDefinition level =
                AssetDatabase.LoadAssetAtPath<FarmBoxMergeLevelDefinition>(path);
            if (level == null)
            {
                continue;
            }

            Undo.RecordObject(level, "Rebuild FarmBoxMerge level flow");
            bool isValid = false;
            string validationError = string.Empty;
            for (int attempt = 0; attempt < 128 && !isValid; attempt++)
            {
                int seed = DefaultSeed + ((i + 1) * 7919) + (attempt * 104729);
                List<ColorType> mixedItems = BuildPlayableLevelDesign(
                    level,
                    seed,
                    out List<FarmBoxMergeBoxSlotPlanEntry> slotPlan);
                WriteSequence(level, mixedItems);
                WriteSlotPlan(level, slotPlan);
                isValid = FarmBoxMergeSlotPlanBuilder.TryValidateAuthoredPlan(
                    level,
                    out validationError);
            }

            if (!isValid)
            {
                Debug.LogError($"FBM_LEVEL_DESIGN_INVALID level={level.LevelName} error={validationError}", level);
                invalidLevelCount++;
                continue;
            }

            EditorUtility.SetDirty(level);
            changedLevelCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"FBM_LEVEL_DESIGN_REBUILD_COMPLETE changed={changedLevelCount} invalid={invalidLevelCount}");
    }

    internal static List<ColorType> BuildMixedSequence(
        IReadOnlyList<FarmBoxMergeItemRun> sourceRuns,
        int seed)
    {
        Dictionary<ColorType, int> remaining = CountItemsByColor(sourceRuns);
        List<ColorType> colors = new List<ColorType>(remaining.Keys);
        colors.Sort();

        int totalCount = 0;
        foreach (int count in remaining.Values)
        {
            totalCount += count;
        }

        System.Random random = new System.Random(seed);
        List<ColorType> result = new List<ColorType>(totalCount);
        ColorType previousColor = default;
        bool hasPrevious = false;

        while (result.Count < totalCount)
        {
            List<ColorType> candidates = FindCandidates(
                colors,
                remaining,
                previousColor,
                hasPrevious,
                totalCount - result.Count);

            if (candidates.Count == 0)
            {
                candidates = FindAvailableColors(colors, remaining);
            }

            ColorType selectedColor = ChooseWeighted(candidates, remaining, random);
            result.Add(selectedColor);
            remaining[selectedColor]--;
            previousColor = selectedColor;
            hasPrevious = true;
        }

        return result;
    }

    internal static List<ColorType> BuildPlayableMixedSequence(
        FarmBoxMergeLevelDefinition level,
        int seed)
    {
        return BuildPlayableLevelDesign(level, seed, out _);
    }

    internal static List<ColorType> BuildPlayableLevelDesign(
        FarmBoxMergeLevelDefinition level,
        int seed,
        out List<FarmBoxMergeBoxSlotPlanEntry> slotPlan)
    {
        slotPlan = new List<FarmBoxMergeBoxSlotPlanEntry>();
        List<FarmBoxMergeBoxRequirement> requirements = new List<FarmBoxMergeBoxRequirement>();
        if (level == null
            || !FarmBoxMergeSlotPlanBuilder.TryBuildPlan(level, requirements, out _)
            || requirements.Count == 0)
        {
            return BuildMixedSequence(level?.ItemSequence, seed);
        }

        System.Random random = new System.Random(seed);
        List<ScheduledBoxGroup> waitingGroups = new List<ScheduledBoxGroup>(requirements.Count);
        for (int i = 0; i < requirements.Count; i++)
        {
            waitingGroups.Add(new ScheduledBoxGroup
            {
                ColorType = requirements[i].ColorType,
                BoxSize = requirements[i].BoxSize,
                RemainingItems = requirements[i].BoxSize
            });
        }

        Shuffle(waitingGroups, random);
        List<ScheduledBoxGroup> activeGroups = new List<ScheduledBoxGroup>(WorldSlotCount);
        List<ColorType> result = new List<ColorType>(level.TotalItemCount);
        ColorType previousColor = default;
        bool hasPrevious = false;

        FillActiveGroups(activeGroups, waitingGroups, random, slotPlan, seed);
        while (activeGroups.Count > 0)
        {
            List<int> candidates = new List<int>();
            for (int i = 0; i < activeGroups.Count; i++)
            {
                if (!hasPrevious || activeGroups[i].ColorType != previousColor)
                {
                    candidates.Add(i);
                }
            }

            if (candidates.Count == 0)
            {
                for (int i = 0; i < activeGroups.Count; i++)
                {
                    candidates.Add(i);
                }
            }

            int selectedIndex = candidates[random.Next(candidates.Count)];
            ScheduledBoxGroup selectedGroup = activeGroups[selectedIndex];
            result.Add(selectedGroup.ColorType);
            previousColor = selectedGroup.ColorType;
            hasPrevious = true;
            selectedGroup.RemainingItems--;

            if (selectedGroup.RemainingItems <= 0)
            {
                activeGroups.RemoveAt(selectedIndex);
                FillActiveGroups(activeGroups, waitingGroups, random, slotPlan, seed);
            }
        }

        return result;
    }

    private static void FillActiveGroups(
        List<ScheduledBoxGroup> activeGroups,
        List<ScheduledBoxGroup> waitingGroups,
        System.Random random,
        List<FarmBoxMergeBoxSlotPlanEntry> slotPlan,
        int seed)
    {
        while (activeGroups.Count < WorldSlotCount && waitingGroups.Count > 0)
        {
            List<int> candidates = new List<int>();
            for (int i = 0; i < waitingGroups.Count; i++)
            {
                bool colorAlreadyActive = false;
                for (int activeIndex = 0; activeIndex < activeGroups.Count; activeIndex++)
                {
                    if (activeGroups[activeIndex].ColorType == waitingGroups[i].ColorType)
                    {
                        colorAlreadyActive = true;
                        break;
                    }
                }

                if (!colorAlreadyActive)
                {
                    candidates.Add(i);
                }
            }

            if (candidates.Count == 0)
            {
                for (int i = 0; i < waitingGroups.Count; i++)
                {
                    candidates.Add(i);
                }
            }

            int selectedIndex = candidates[random.Next(candidates.Count)];
            ScheduledBoxGroup selectedGroup = waitingGroups[selectedIndex];
            activeGroups.Add(selectedGroup);
            waitingGroups.RemoveAt(selectedIndex);
            slotPlan.Add(new FarmBoxMergeBoxSlotPlanEntry
            {
                intendedColor = selectedGroup.ColorType,
                boxSize = selectedGroup.BoxSize,
                fourBoxPatternVariant = selectedGroup.BoxSize == FarmBoxMergeRules.MaxCardCounter
                    ? (seed + slotPlan.Count) & 3
                    : 0
            });
        }
    }

    private static void Shuffle<T>(List<T> values, System.Random random)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            (values[i], values[swapIndex]) = (values[swapIndex], values[i]);
        }
    }

    private static List<ColorType> FindCandidates(
        List<ColorType> colors,
        Dictionary<ColorType, int> remaining,
        ColorType previousColor,
        bool hasPrevious,
        int slotsBeforeSelection)
    {
        List<ColorType> validCandidates = new List<ColorType>();
        List<ColorType> nonRepeatingCandidates = new List<ColorType>();

        for (int i = 0; i < colors.Count; i++)
        {
            ColorType color = colors[i];
            if (remaining[color] <= 0 || (hasPrevious && color == previousColor))
            {
                continue;
            }

            nonRepeatingCandidates.Add(color);
            remaining[color]--;
            if (CanFinishWithoutRepeating(colors, remaining, color, slotsBeforeSelection - 1))
            {
                validCandidates.Add(color);
            }
            remaining[color]++;
        }

        return validCandidates.Count > 0 ? validCandidates : nonRepeatingCandidates;
    }

    private static bool CanFinishWithoutRepeating(
        List<ColorType> colors,
        Dictionary<ColorType, int> remaining,
        ColorType previousColor,
        int remainingSlots)
    {
        for (int i = 0; i < colors.Count; i++)
        {
            ColorType color = colors[i];
            int colorCount = remaining[color];
            int otherCount = remainingSlots - colorCount;
            int allowedExtra = color == previousColor ? 0 : 1;
            if (colorCount > otherCount + allowedExtra)
            {
                return false;
            }
        }

        return true;
    }

    private static ColorType ChooseWeighted(
        List<ColorType> candidates,
        Dictionary<ColorType, int> remaining,
        System.Random random)
    {
        int totalWeight = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            totalWeight += remaining[candidates[i]];
        }

        int roll = random.Next(totalWeight);
        for (int i = 0; i < candidates.Count; i++)
        {
            ColorType color = candidates[i];
            roll -= remaining[color];
            if (roll < 0)
            {
                return color;
            }
        }

        return candidates[candidates.Count - 1];
    }

    private static List<ColorType> FindAvailableColors(
        List<ColorType> colors,
        Dictionary<ColorType, int> remaining)
    {
        List<ColorType> available = new List<ColorType>();
        for (int i = 0; i < colors.Count; i++)
        {
            if (remaining[colors[i]] > 0)
            {
                available.Add(colors[i]);
            }
        }

        return available;
    }

    private static Dictionary<ColorType, int> CountItemsByColor(
        IReadOnlyList<FarmBoxMergeItemRun> sourceRuns)
    {
        Dictionary<ColorType, int> counts = new Dictionary<ColorType, int>();
        if (sourceRuns == null)
        {
            return counts;
        }

        for (int i = 0; i < sourceRuns.Count; i++)
        {
            FarmBoxMergeItemRun run = sourceRuns[i];
            if (run == null)
            {
                continue;
            }

            counts.TryGetValue(run.colorType, out int currentCount);
            counts[run.colorType] = currentCount + Mathf.Max(1, run.count);
        }

        return counts;
    }

    private static int CountDistinctColors(IReadOnlyList<FarmBoxMergeItemRun> sourceRuns)
    {
        return CountItemsByColor(sourceRuns).Count;
    }

    private static void WriteSequence(
        FarmBoxMergeLevelDefinition level,
        List<ColorType> mixedItems)
    {
        SerializedObject serializedLevel = new SerializedObject(level);
        SerializedProperty sequence = serializedLevel.FindProperty("itemSequence");
        sequence.ClearArray();

        int runIndex = -1;
        ColorType previousColor = default;
        for (int i = 0; i < mixedItems.Count; i++)
        {
            ColorType color = mixedItems[i];
            if (runIndex >= 0 && color == previousColor)
            {
                SerializedProperty currentRun = sequence.GetArrayElementAtIndex(runIndex);
                currentRun.FindPropertyRelative("count").intValue++;
                continue;
            }

            runIndex++;
            sequence.InsertArrayElementAtIndex(runIndex);
            SerializedProperty newRun = sequence.GetArrayElementAtIndex(runIndex);
            newRun.FindPropertyRelative("colorType").enumValueIndex = (int)color;
            newRun.FindPropertyRelative("count").intValue = 1;
            previousColor = color;
        }

        serializedLevel.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WriteSlotPlan(
        FarmBoxMergeLevelDefinition level,
        IReadOnlyList<FarmBoxMergeBoxSlotPlanEntry> slotPlan)
    {
        SerializedObject serializedLevel = new SerializedObject(level);
        SerializedProperty serializedPlan = serializedLevel.FindProperty("boxSlotPlan");
        serializedPlan.ClearArray();

        for (int i = 0; i < slotPlan.Count; i++)
        {
            FarmBoxMergeBoxSlotPlanEntry entry = slotPlan[i];
            serializedPlan.InsertArrayElementAtIndex(i);
            SerializedProperty serializedEntry = serializedPlan.GetArrayElementAtIndex(i);
            serializedEntry.FindPropertyRelative("intendedColor").enumValueIndex = (int)entry.intendedColor;
            serializedEntry.FindPropertyRelative("boxSize").intValue = entry.boxSize;
            serializedEntry.FindPropertyRelative("fourBoxPatternVariant").intValue = entry.fourBoxPatternVariant;
        }

        serializedLevel.ApplyModifiedPropertiesWithoutUndo();
    }

    private static int CompareAssetPaths(string firstGuid, string secondGuid)
    {
        return string.CompareOrdinal(
            AssetDatabase.GUIDToAssetPath(firstGuid),
            AssetDatabase.GUIDToAssetPath(secondGuid));
    }
}
