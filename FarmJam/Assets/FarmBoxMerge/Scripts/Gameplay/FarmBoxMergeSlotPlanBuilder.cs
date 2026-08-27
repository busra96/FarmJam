using System.Collections.Generic;
using UnityEngine;

public readonly struct FarmBoxMergeBoxRequirement
{
    public FarmBoxMergeBoxRequirement(ColorType colorType, int boxSize)
    {
        ColorType = colorType;
        BoxSize = FarmBoxMergeRules.ClampCardCounter(boxSize);
    }

    public ColorType ColorType { get; }
    public int BoxSize { get; }
}

public static class FarmBoxMergeSlotPlanBuilder
{
    private const int WorldSlotCount = 3;

    private sealed class RecipeCandidate
    {
        public readonly int[] CountsBySize = new int[FarmBoxMergeRules.MaxCardCounter + 1];
        public int GroupCount;
        public int CardCost;
    }

    public static bool TryBuildPlan(
        FarmBoxMergeLevelDefinition level,
        List<FarmBoxMergeBoxRequirement> output,
        out string error)
    {
        output?.Clear();
        if (level == null || output == null)
        {
            error = "Level veya çıktı listesi eksik.";
            return false;
        }

        Dictionary<ColorType, int> itemTotals = CountItems(level.ItemSequence);
        Dictionary<ColorType, int> cardTotals = CountCards(level.CardSpawnPlan);
        return TryBuildPlan(itemTotals, cardTotals, true, output, out error);
    }

    public static bool TryBuildRemainingPlan(
        IReadOnlyDictionary<ColorType, int> remainingItemTotals,
        IReadOnlyDictionary<ColorType, int> remainingCardTotals,
        List<FarmBoxMergeBoxRequirement> output,
        out string error)
    {
        return TryBuildPlan(remainingItemTotals, remainingCardTotals, false, output, out error);
    }

    public static bool TryValidateAuthoredPlan(
        FarmBoxMergeLevelDefinition level,
        out string error)
    {
        if (level == null || !level.HasAuthoredBoxSlotPlan)
        {
            error = "Level için sabit şeffaf kutu akışı tanımlanmamış.";
            return false;
        }

        Dictionary<ColorType, int> itemTotals = CountItems(level.ItemSequence);
        Dictionary<ColorType, int> cardTotals = CountCards(level.CardSpawnPlan);
        Dictionary<ColorType, int> plannedItems = new Dictionary<ColorType, int>();
        Dictionary<ColorType, int> plannedCards = new Dictionary<ColorType, int>();

        IReadOnlyList<FarmBoxMergeBoxSlotPlanEntry> plan = level.BoxSlotPlan;
        for (int i = 0; i < plan.Count; i++)
        {
            FarmBoxMergeBoxSlotPlanEntry entry = plan[i];
            if (entry == null)
            {
                error = $"Şeffaf kutu akışının {i + 1}. adımı boş.";
                return false;
            }

            int boxSize = FarmBoxMergeRules.ClampCardCounter(entry.boxSize);
            Add(plannedItems, entry.intendedColor, boxSize);
            Add(plannedCards, entry.intendedColor, GetRequiredLevelOneCardCount(boxSize));
        }

        if (!HaveSameTotals(itemTotals, plannedItems, out ColorType itemColor))
        {
            error = $"{itemColor}: hedef kutu kapasitesi ile collectable item toplamı eşleşmiyor.";
            return false;
        }

        if (!HaveSameTotals(cardTotals, plannedCards, out ColorType cardColor))
        {
            error = $"{cardColor}: hedef kutuları üretmek için gereken level-1 kart sayısı kart planıyla eşleşmiyor.";
            return false;
        }

        if (!CanProcessItemFlow(level.ItemSequence, plan, out error))
        {
            return false;
        }

        List<ColorType> cardDeck = new List<ColorType>();
        if (!FarmBoxMergeCardDeckBuilder.TryBuild(level, cardDeck, out error))
        {
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryBuildPlan(
        IReadOnlyDictionary<ColorType, int> itemTotals,
        IReadOnlyDictionary<ColorType, int> cardTotals,
        bool requireExactCardCount,
        List<FarmBoxMergeBoxRequirement> output,
        out string error)
    {
        output?.Clear();
        if (itemTotals == null || cardTotals == null || output == null)
        {
            error = "Item, kart veya çıktı verisi eksik.";
            return false;
        }

        List<ColorType> colors = new List<ColorType>(itemTotals.Keys);
        colors.Sort();

        for (int i = 0; i < colors.Count; i++)
        {
            ColorType colorType = colors[i];
            int itemCount = itemTotals[colorType];
            int cardCount = cardTotals.TryGetValue(colorType, out int authoredCards)
                ? authoredCards
                : 0;

            if (itemCount <= 0)
            {
                continue;
            }

            if (!TrySolveColor(itemCount, cardCount, requireExactCardCount, out RecipeCandidate recipe))
            {
                string cardRule = requireExactCardCount ? "tam olarak" : "en fazla";
                error = $"{colorType}: {itemCount} item ve {cardRule} {cardCount} level-1 kart ile 1-4 arası kutu planı kurulamıyor.";
                output.Clear();
                return false;
            }

            for (int boxSize = 1; boxSize <= FarmBoxMergeRules.MaxCardCounter; boxSize++)
            {
                for (int count = 0; count < recipe.CountsBySize[boxSize]; count++)
                {
                    output.Add(new FarmBoxMergeBoxRequirement(colorType, boxSize));
                }
            }
        }

        if (requireExactCardCount)
        {
            foreach (KeyValuePair<ColorType, int> cards in cardTotals)
            {
                if (cards.Value > 0 && !itemTotals.ContainsKey(cards.Key))
                {
                    error = $"{cards.Key}: Kart var fakat bu renkte collectable item yok.";
                    output.Clear();
                    return false;
                }
            }
        }

        error = string.Empty;
        return requireExactCardCount ? output.Count > 0 : true;
    }

    public static int GetRequiredLevelOneCardCount(int boxSize)
    {
        int clampedSize = FarmBoxMergeRules.ClampCardCounter(boxSize);
        return 1 << (clampedSize - FarmBoxMergeRules.MinCardCounter);
    }

    private static bool TrySolveColor(
        int itemCount,
        int cardCount,
        bool requireExactCardCount,
        out RecipeCandidate bestRecipe)
    {
        bestRecipe = null;
        for (int fours = 0; fours <= itemCount / 4; fours++)
        {
            for (int threes = 0; threes <= itemCount / 3; threes++)
            {
                for (int twos = 0; twos <= itemCount / 2; twos++)
                {
                    int ones = itemCount - (fours * 4) - (threes * 3) - (twos * 2);
                    if (ones < 0)
                    {
                        continue;
                    }

                    int requiredCards = ones
                        + (twos * GetRequiredLevelOneCardCount(2))
                        + (threes * GetRequiredLevelOneCardCount(3))
                        + (fours * GetRequiredLevelOneCardCount(4));
                    if ((requireExactCardCount && requiredCards != cardCount)
                        || (!requireExactCardCount && requiredCards > cardCount))
                    {
                        continue;
                    }

                    int groupCount = ones + twos + threes + fours;
                    if (!IsBetterRecipe(
                            requireExactCardCount,
                            requiredCards,
                            groupCount,
                            fours,
                            bestRecipe))
                    {
                        continue;
                    }

                    bestRecipe = new RecipeCandidate
                    {
                        GroupCount = groupCount,
                        CardCost = requiredCards
                    };
                    bestRecipe.CountsBySize[1] = ones;
                    bestRecipe.CountsBySize[2] = twos;
                    bestRecipe.CountsBySize[3] = threes;
                    bestRecipe.CountsBySize[4] = fours;
                }
            }
        }

        return bestRecipe != null;
    }

    private static bool IsBetterRecipe(
        bool requireExactCardCount,
        int cardCost,
        int groupCount,
        int fourCount,
        RecipeCandidate currentBest)
    {
        if (currentBest == null)
        {
            return true;
        }

        if (!requireExactCardCount && cardCost != currentBest.CardCost)
        {
            return cardCost > currentBest.CardCost;
        }

        if (groupCount != currentBest.GroupCount)
        {
            return groupCount < currentBest.GroupCount;
        }

        return fourCount > currentBest.CountsBySize[4];
    }

    private static Dictionary<ColorType, int> CountItems(
        IReadOnlyList<FarmBoxMergeItemRun> itemSequence)
    {
        Dictionary<ColorType, int> totals = new Dictionary<ColorType, int>();
        if (itemSequence == null)
        {
            return totals;
        }

        for (int i = 0; i < itemSequence.Count; i++)
        {
            FarmBoxMergeItemRun run = itemSequence[i];
            if (run != null)
            {
                Add(totals, run.colorType, Mathf.Max(1, run.count));
            }
        }

        return totals;
    }

    private static Dictionary<ColorType, int> CountCards(
        IReadOnlyList<FarmBoxMergeCardSpawnGroup> cardPlan)
    {
        Dictionary<ColorType, int> totals = new Dictionary<ColorType, int>();
        if (cardPlan == null)
        {
            return totals;
        }

        for (int i = 0; i < cardPlan.Count; i++)
        {
            FarmBoxMergeCardSpawnGroup group = cardPlan[i];
            if (group != null)
            {
                Add(totals, group.colorType, Mathf.Max(1, group.count));
            }
        }

        return totals;
    }

    private static void Add(
        Dictionary<ColorType, int> totals,
        ColorType colorType,
        int amount)
    {
        totals.TryGetValue(colorType, out int current);
        totals[colorType] = current + amount;
    }

    private static bool HaveSameTotals(
        IReadOnlyDictionary<ColorType, int> expected,
        IReadOnlyDictionary<ColorType, int> actual,
        out ColorType mismatchColor)
    {
        foreach (ColorType colorType in System.Enum.GetValues(typeof(ColorType)))
        {
            int expectedCount = expected.TryGetValue(colorType, out int expectedValue)
                ? expectedValue
                : 0;
            int actualCount = actual.TryGetValue(colorType, out int actualValue)
                ? actualValue
                : 0;
            if (expectedCount != actualCount)
            {
                mismatchColor = colorType;
                return false;
            }
        }

        mismatchColor = default;
        return true;
    }

    private static bool CanProcessItemFlow(
        IReadOnlyList<FarmBoxMergeItemRun> itemSequence,
        IReadOnlyList<FarmBoxMergeBoxSlotPlanEntry> plan,
        out string error)
    {
        List<ColorType> activeColors = new List<ColorType>(WorldSlotCount);
        List<int> activeRemaining = new List<int>(WorldSlotCount);
        int nextPlanIndex = 0;
        ActivatePlannedGroups(plan, activeColors, activeRemaining, ref nextPlanIndex);

        int itemIndex = 0;
        for (int runIndex = 0; runIndex < itemSequence.Count; runIndex++)
        {
            FarmBoxMergeItemRun run = itemSequence[runIndex];
            if (run == null)
            {
                continue;
            }

            int count = Mathf.Max(1, run.count);
            for (int repeat = 0; repeat < count; repeat++)
            {
                itemIndex++;
                int activeIndex = activeColors.IndexOf(run.colorType);
                if (activeIndex < 0)
                {
                    error = $"{itemIndex}. item ({run.colorType}) geldiğinde üç aktif hedef kutu arasında bu renk yok.";
                    return false;
                }

                activeRemaining[activeIndex]--;
                if (activeRemaining[activeIndex] > 0)
                {
                    continue;
                }

                activeColors.RemoveAt(activeIndex);
                activeRemaining.RemoveAt(activeIndex);
                ActivatePlannedGroups(plan, activeColors, activeRemaining, ref nextPlanIndex);
            }
        }

        if (nextPlanIndex != plan.Count || activeColors.Count != 0)
        {
            error = "Item akışı bittiğinde hedef kutu akışında tamamlanmamış adımlar kalıyor.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static void ActivatePlannedGroups(
        IReadOnlyList<FarmBoxMergeBoxSlotPlanEntry> plan,
        List<ColorType> activeColors,
        List<int> activeRemaining,
        ref int nextPlanIndex)
    {
        while (activeColors.Count < WorldSlotCount && nextPlanIndex < plan.Count)
        {
            FarmBoxMergeBoxSlotPlanEntry entry = plan[nextPlanIndex++];
            activeColors.Add(entry.intendedColor);
            activeRemaining.Add(FarmBoxMergeRules.ClampCardCounter(entry.boxSize));
        }
    }
}

/// <summary>
/// Converts authored level data into a deterministic level-one card deck.
/// Keeping this pure makes the same rule reusable by runtime, editor validation
/// and future automated level tests without depending on CardSpawner state.
/// </summary>
public static class FarmBoxMergeCardDeckBuilder
{
    private const int ColorCount = 5;

    public static bool TryBuild(
        FarmBoxMergeLevelDefinition level,
        List<ColorType> output,
        out string error)
    {
        output?.Clear();
        if (level == null || output == null || !level.HasAuthoredBoxSlotPlan)
        {
            error = "Sabit şeffaf kutu planı bulunamadı.";
            return false;
        }

        int[] expectedCards = new int[ColorCount];
        IReadOnlyList<FarmBoxMergeCardSpawnGroup> cardPlan = level.CardSpawnPlan;
        for (int i = 0; i < cardPlan.Count; i++)
        {
            FarmBoxMergeCardSpawnGroup group = cardPlan[i];
            if (group == null || !TryGetColorIndex(group.colorType, out int colorIndex))
            {
                continue;
            }

            expectedCards[colorIndex] += Mathf.Max(1, group.count);
        }

        int[] authoredCards = new int[ColorCount];
        IReadOnlyList<FarmBoxMergeBoxSlotPlanEntry> slotPlan = level.BoxSlotPlan;
        for (int i = 0; i < slotPlan.Count; i++)
        {
            FarmBoxMergeBoxSlotPlanEntry entry = slotPlan[i];
            if (entry == null || !TryGetColorIndex(entry.intendedColor, out int colorIndex))
            {
                output.Clear();
                error = $"Şeffaf kutu akışının {i + 1}. kart adımı geçersiz.";
                return false;
            }

            int cardCount = FarmBoxMergeSlotPlanBuilder.GetRequiredLevelOneCardCount(entry.boxSize);
            authoredCards[colorIndex] += cardCount;
            for (int cardIndex = 0; cardIndex < cardCount; cardIndex++)
            {
                output.Add(entry.intendedColor);
            }
        }

        for (int colorIndex = 0; colorIndex < ColorCount; colorIndex++)
        {
            if (expectedCards[colorIndex] == authoredCards[colorIndex])
            {
                continue;
            }

            output.Clear();
            error = $"{(ColorType)colorIndex}: kart planı ile sabit kutu çözüm maliyeti eşleşmiyor.";
            return false;
        }

        error = output.Count > 0 ? string.Empty : "Level kart destesi boş.";
        return output.Count > 0;
    }

    private static bool TryGetColorIndex(ColorType colorType, out int index)
    {
        index = (int)colorType;
        return index >= 0 && index < ColorCount;
    }
}
