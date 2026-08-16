using System;
using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private Card cardPrefab;
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

    private void Reset()
    {
        AutoAssignReferences();

        if (availableColors.Count == 0)
        {
            availableColors.AddRange(CardMergeBoard.CreateDefaultPalette());
        }
    }

    private void Awake()
    {
        AutoAssignReferences();
        EnsureBoardReference();
        SyncPaletteWithBoard();
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnConfiguredCards();
        }
    }

    [ContextMenu("Spawn Configured Cards")]
    public void SpawnConfiguredCards()
    {
        if (cardPrefab == null)
        {
            Debug.LogWarning("CardSpawner icin Card prefab referansi eksik.", this);
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

        if (cardPrefab == null || targetParent == null)
        {
            return null;
        }

        Card spawnedCard = Instantiate(cardPrefab, targetParent);
        spawnedCard.Initialize(resolvedBoard, counter, colorType);
        return spawnedCard;
    }

    public Card SpawnRandomCard()
    {
        int minCounter = Mathf.Min(counterRange.x, counterRange.y);
        int maxCounter = Mathf.Max(counterRange.x, counterRange.y);
        int counter = UnityEngine.Random.Range(minCounter, maxCounter + 1);
        return SpawnCard(counter, GetRandomColorType());
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
            SpawnCard(cardData.counter, cardData.colorType);
        }
    }

    private void SpawnRandomCards()
    {
        _lastSpawnedCards.Clear();
        int count = Mathf.Max(0, randomSpawnCount);
        for (int i = 0; i < count; i++)
        {
            int minCounter = Mathf.Min(counterRange.x, counterRange.y);
            int maxCounter = Mathf.Max(counterRange.x, counterRange.y);
            int counter = UnityEngine.Random.Range(minCounter, maxCounter + 1);
            ColorType colorType = GetRandomColorType();

            if (SpawnCard(counter, colorType) != null)
            {
                RememberCard(counter, colorType);
            }
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

            if (SpawnCard(cardData.counter, cardData.colorType) != null)
            {
                RememberCard(cardData.counter, cardData.colorType);
            }
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

}
