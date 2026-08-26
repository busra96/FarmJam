using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class FarmBoxMergeLevelSequenceMixer
{
    private const string LevelFolder = "Assets/FarmBoxMerge/Levels";
    private const int DefaultSeed = 20260826;

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
        string[] levelGuids = AssetDatabase.FindAssets(
            "t:FarmBoxMergeLevelDefinition",
            new[] { LevelFolder });
        Array.Sort(levelGuids, CompareAssetPaths);

        int changedLevelCount = 0;
        int singleColorLevelCount = 0;
        for (int i = 0; i < levelGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(levelGuids[i]);
            FarmBoxMergeLevelDefinition level =
                AssetDatabase.LoadAssetAtPath<FarmBoxMergeLevelDefinition>(path);
            if (level == null)
            {
                continue;
            }

            if (CountDistinctColors(level.ItemSequence) < 2)
            {
                singleColorLevelCount++;
                continue;
            }

            Undo.RecordObject(level, "Mix FarmBoxMerge item flow");
            List<ColorType> mixedItems = BuildMixedSequence(
                level.ItemSequence,
                DefaultSeed + ((i + 1) * 7919));
            WriteSequence(level, mixedItems);
            EditorUtility.SetDirty(level);
            changedLevelCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"FBM_LEVEL_MIX_COMPLETE changed={changedLevelCount} " +
            $"singleColorUnchanged={singleColorLevelCount}");
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

    private static int CompareAssetPaths(string firstGuid, string secondGuid)
    {
        return string.CompareOrdinal(
            AssetDatabase.GUIDToAssetPath(firstGuid),
            AssetDatabase.GUIDToAssetPath(secondGuid));
    }
}
