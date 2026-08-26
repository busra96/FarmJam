using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class CardSpawner : MonoBehaviour
{
    public enum SpawnMode
    {
        RandomPalette,
        ManualList
    }

    [Serializable]
    public class CardSpawnEntry
    {
        [Min(1)] public int counter = 1;
        public ColorType colorType = ColorType.Green;
    }

    [Header("References")]
    [SerializeField] private RectTransform spawnParent;
    [SerializeField] private CardMergeBoard board;

    [Header("Spawn Settings")]
    [SerializeField] private bool spawnOnStart;
    [SerializeField] private bool clearBeforeSpawn = true;
    [SerializeField] private SpawnMode spawnMode = SpawnMode.RandomPalette;
    [SerializeField] private int randomSpawnCount = 8;
    [SerializeField] private Vector2Int counterRange = new Vector2Int(1, 2);
    [SerializeField] private List<CardMergeBoard.ColorPaletteEntry> availableColors = new List<CardMergeBoard.ColorPaletteEntry>();
    [SerializeField] private List<CardSpawnEntry> manualCards = new List<CardSpawnEntry>();

    private readonly List<CardSpawnEntry> _lastSpawnedCards = new List<CardSpawnEntry>();
    private ICardFactory _cardFactory;
    private IFarmBoxMergeBoxRegistry _boxRegistry;
    private IFarmBoxMergeFeedbackService _feedback;
    private bool _initialized;

    [Inject]
    public void Construct(
        ICardFactory cardFactory,
        IFarmBoxMergeBoxRegistry boxRegistry,
        IFarmBoxMergeFeedbackService feedback,
        CardMergeBoard injectedBoard)
    {
        _cardFactory = cardFactory;
        _boxRegistry = boxRegistry;
        _feedback = feedback;
        board = injectedBoard;
    }

    private void Reset()
    {
        AutoAssignReferences();

        if (availableColors.Count == 0)
        {
            availableColors.AddRange(CardMergeBoard.CreateDefaultPalette());
        }
    }

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        AutoAssignReferences();
        EnsureBoardReference();
        SyncPaletteWithBoard();

        if (spawnOnStart)
        {
            SpawnConfiguredCards();
        }
    }

    public void SetSpawnOnStart(bool shouldSpawnOnStart)
    {
        spawnOnStart = shouldSpawnOnStart;
    }

    public void SpawnLevelCards(IReadOnlyList<FarmBoxMergeStartingCard> startingCards)
    {
        _lastSpawnedCards.Clear();
        if (startingCards == null)
        {
            return;
        }

        int cardCount = Mathf.Min(startingCards.Count, FarmBoxMergeRules.MaxCardsOnBoard);
        for (int i = 0; i < cardCount; i++)
        {
            FarmBoxMergeStartingCard cardData = startingCards[i];
            if (cardData == null)
            {
                continue;
            }

            int counter = FarmBoxMergeRules.ClampCardCounter(cardData.counter);
            if (SpawnCard(counter, cardData.colorType) == null)
            {
                break;
            }

            RememberCard(counter, cardData.colorType);
        }
    }

    [ContextMenu("Spawn Configured Cards")]
    public void SpawnConfiguredCards()
    {
        if (_cardFactory == null)
        {
            Debug.LogWarning("Card factory is not available. Check FarmBoxMergeLifetimeScope.", this);
            return;
        }

        RectTransform targetParent = ResolveSpawnParent();
        if (targetParent == null)
        {
            Debug.LogWarning("CardSpawner icin spawn parent bulunamadi.", this);
            return;
        }

        EnsureBoardReference();
        SyncPaletteWithBoard();

        if (clearBeforeSpawn)
        {
            ClearCards();
        }

        switch (spawnMode)
        {
            case SpawnMode.ManualList:
                SpawnManualCards();
                break;
            default:
                SpawnRandomCards();
                break;
        }
    }

    [ContextMenu("Clear Spawned Cards")]
    public void ClearCards()
    {
        if (board != null)
        {
            board.ClearCards();
            return;
        }

        RectTransform targetParent = ResolveSpawnParent();
        if (targetParent == null)
        {
            return;
        }

        for (int i = targetParent.childCount - 1; i >= 0; i--)
        {
            Transform child = targetParent.GetChild(i);
            if (child.TryGetComponent(out Card _))
            {
                FarmBoxMergeObjectUtility.Destroy(child.gameObject);
            }
        }
    }

    public Card SpawnCard(int counter, ColorType colorType)
    {
        RectTransform targetParent = ResolveSpawnParent();
        CardMergeBoard resolvedBoard = EnsureBoardReference();

        if (_cardFactory == null || targetParent == null || !CanSpawnCard())
        {
            return null;
        }

        Card spawnedCard = _cardFactory.Create(targetParent);
        if (spawnedCard == null)
        {
            return null;
        }

        spawnedCard.Initialize(resolvedBoard, FarmBoxMergeRules.ClampCardCounter(counter), colorType);
        resolvedBoard?.RegisterCard(spawnedCard);
        spawnedCard.PlayMergePop();
        _feedback?.PlayCardSpawn(spawnedCard.RectTransform, _feedback.ColorFor(colorType));
        return spawnedCard;
    }

    public bool CanSpawnCard()
    {
        CardMergeBoard resolvedBoard = EnsureBoardReference();
        if (resolvedBoard != null)
        {
            return resolvedBoard.HasCardCapacity;
        }

        RectTransform targetParent = ResolveSpawnParent();
        return targetParent != null
            && targetParent.GetComponentsInChildren<Card>(true).Length < FarmBoxMergeRules.MaxCardsOnBoard;
    }

    public Card SpawnRandomCard()
    {
        int minCounter = FarmBoxMergeRules.ClampCardCounter(Mathf.Min(counterRange.x, counterRange.y));
        int maxCounter = FarmBoxMergeRules.ClampCardCounter(Mathf.Max(counterRange.x, counterRange.y));
        int counter = UnityEngine.Random.Range(minCounter, maxCounter + 1);
        return SpawnCard(counter, GetRandomColorType());
    }

    public Card SpawnRecommendedCard(IReadOnlyList<MergeItem> queuedItems)
    {
        if (!CanSpawnCard())
        {
            return null;
        }

        return SpawnCard(FarmBoxMergeRules.MinCardCounter, GetRecommendedColorType(queuedItems));
    }

    public void ReplayLastCards()
    {
        if (_lastSpawnedCards.Count == 0)
        {
            SpawnConfiguredCards();
            return;
        }

        if (clearBeforeSpawn)
        {
            ClearCards();
        }

        for (int i = 0; i < _lastSpawnedCards.Count; i++)
        {
            CardSpawnEntry cardData = _lastSpawnedCards[i];
            if (SpawnCard(cardData.counter, cardData.colorType) == null)
            {
                break;
            }
        }
    }

    private void SpawnRandomCards()
    {
        _lastSpawnedCards.Clear();
        int count = Mathf.Max(0, randomSpawnCount);
        for (int i = 0; i < count; i++)
        {
            int minCounter = FarmBoxMergeRules.ClampCardCounter(Mathf.Min(counterRange.x, counterRange.y));
            int maxCounter = FarmBoxMergeRules.ClampCardCounter(Mathf.Max(counterRange.x, counterRange.y));
            int counter = UnityEngine.Random.Range(minCounter, maxCounter + 1);
            ColorType colorType = GetRandomColorType();

            if (SpawnCard(counter, colorType) == null)
            {
                break;
            }

            RememberCard(counter, colorType);
        }
    }

    private void SpawnManualCards()
    {
        _lastSpawnedCards.Clear();
        foreach (CardSpawnEntry cardData in manualCards)
        {
            if (cardData == null)
            {
                continue;
            }

            if (SpawnCard(cardData.counter, cardData.colorType) == null)
            {
                break;
            }

            RememberCard(FarmBoxMergeRules.ClampCardCounter(cardData.counter), cardData.colorType);
        }
    }

    private void RememberCard(int counter, ColorType colorType)
    {
        _lastSpawnedCards.Add(new CardSpawnEntry
        {
            counter = counter,
            colorType = colorType
        });
    }

    private void AutoAssignReferences()
    {
        if (spawnParent == null && transform is RectTransform rectTransform)
        {
            spawnParent = rectTransform;
        }

        if (board == null && spawnParent != null)
        {
            board = spawnParent.GetComponent<CardMergeBoard>();
        }

    }

    private RectTransform ResolveSpawnParent()
    {
        if (spawnParent != null)
        {
            return spawnParent;
        }

        if (board != null)
        {
            spawnParent = board.CardContainer;
            return spawnParent;
        }

        if (transform is RectTransform rectTransform)
        {
            spawnParent = rectTransform;
        }

        return spawnParent;
    }

    private CardMergeBoard EnsureBoardReference()
    {
        if (board != null)
        {
            return board;
        }

        RectTransform targetParent = ResolveSpawnParent();
        GameObject targetObject = targetParent != null ? targetParent.gameObject : gameObject;

        board = targetObject.GetComponent<CardMergeBoard>();
        if (board == null)
        {
            board = GetComponentInParent<CardMergeBoard>();
        }

        return board;
    }

    private void SyncPaletteWithBoard()
    {
        CardMergeBoard resolvedBoard = EnsureBoardReference();
        if (resolvedBoard == null)
        {
            return;
        }

        if (availableColors.Count == 0)
        {
            if (resolvedBoard.HasColorPalette)
            {
                foreach (CardMergeBoard.ColorPaletteEntry entry in resolvedBoard.ColorPalette)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    availableColors.Add(new CardMergeBoard.ColorPaletteEntry
                    {
                        colorType = entry.colorType,
                        color = entry.color
                    });
                }
            }
            else
            {
                availableColors.AddRange(CardMergeBoard.CreateDefaultPalette());
            }
        }

        resolvedBoard.SetColorPalette(availableColors);
    }

    private ColorType GetRandomColorType()
    {
        if (availableColors.Count == 0)
        {
            SyncPaletteWithBoard();
        }

        if (availableColors.Count == 0)
        {
            return FarmBoxMergeRandom.ColorType();
        }

        int randomIndex = UnityEngine.Random.Range(0, availableColors.Count);
        return availableColors[randomIndex].colorType;
    }

    private ColorType GetRecommendedColorType(IReadOnlyList<MergeItem> queuedItems)
    {
        CardMergeBoard resolvedBoard = EnsureBoardReference();
        Dictionary<ColorType, int> remainingCapacityByColor = new Dictionary<ColorType, int>();
        List<ColorType> queueOrder = new List<ColorType>();

        if (queuedItems != null)
        {
            for (int i = 0; i < queuedItems.Count; i++)
            {
                MergeItem item = queuedItems[i];
                if (item == null)
                {
                    continue;
                }

                ColorType colorType = item.ColorType;
                if (!remainingCapacityByColor.TryGetValue(colorType, out int remainingCapacity))
                {
                    int cardCapacity = resolvedBoard != null ? resolvedBoard.GetCardCapacity(colorType) : 0;
                    remainingCapacity = cardCapacity + (_boxRegistry?.CountAvailable(colorType) ?? 0);
                }

                if (!queueOrder.Contains(colorType))
                {
                    queueOrder.Add(colorType);
                }

                if (remainingCapacity <= 0)
                {
                    return colorType;
                }

                remainingCapacityByColor[colorType] = remainingCapacity - 1;
            }
        }

        for (int i = 0; i < queueOrder.Count; i++)
        {
            if (resolvedBoard != null && resolvedBoard.HasLevelOneCard(queueOrder[i]))
            {
                return queueOrder[i];
            }
        }

        if (resolvedBoard != null)
        {
            SyncPaletteWithBoard();
            for (int i = 0; i < availableColors.Count; i++)
            {
                ColorType colorType = availableColors[i].colorType;
                if (resolvedBoard.HasLevelOneCard(colorType))
                {
                    return colorType;
                }
            }
        }

        return queueOrder.Count > 0 ? queueOrder[0] : GetRandomColorType();
    }

}
