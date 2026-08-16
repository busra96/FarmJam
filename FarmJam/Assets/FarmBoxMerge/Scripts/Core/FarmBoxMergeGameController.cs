using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FarmBoxMergeGameController : MonoBehaviour
{
    [Header("Game References")]
    [SerializeField] private CardSpawner cardSpawner;
    [SerializeField] private CardMergeBoard cardMergeBoard;
    [SerializeField] private MergeItemSpawner itemSpawner;

    [Header("Reset UI")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button addCardButton;
    [SerializeField] private string refreshLabel = "REFRESH";
    [SerializeField] private string retryLabel = "RETRY";
    [SerializeField] private string addCardLabel = "ADD CARD";
    [SerializeField] private Color buttonColor = new Color(0.2f, 0.67f, 0.38f, 1f);
    [SerializeField] private Color retryButtonColor = new Color(0.95f, 0.55f, 0.18f, 1f);
    [SerializeField] private Color addCardButtonColor = new Color(0.2f, 0.55f, 0.9f, 1f);

    private Coroutine _resetRoutine;

    private void Awake()
    {
        ResolveReferences();
        ConfigureButton(refreshButton, RefreshGame, refreshLabel, buttonColor, "refresh");
        ConfigureButton(retryButton, RetryLevel, retryLabel, retryButtonColor, "retry");
        ConfigureButton(addCardButton, AddRandomCard, addCardLabel, addCardButtonColor, "add card");
    }

    private void OnDestroy()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveListener(RefreshGame);
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(RetryLevel);
        }

        if (addCardButton != null)
        {
            addCardButton.onClick.RemoveListener(AddRandomCard);
        }
    }

    [ContextMenu("Refresh Game")]
    public void RefreshGame()
    {
        StartReset(replaySameLevel: false);
    }

    [ContextMenu("Retry Level")]
    public void RetryLevel()
    {
        StartReset(replaySameLevel: true);
    }

    public void AddRandomCard()
    {
        if (!Application.isPlaying || _resetRoutine != null)
        {
            return;
        }

        Card spawnedCard = cardSpawner?.SpawnRandomCard();
        spawnedCard?.PlayMergePop();
    }

    private void StartReset(bool replaySameLevel)
    {
        if (!Application.isPlaying || _resetRoutine != null)
        {
            return;
        }

        _resetRoutine = StartCoroutine(ResetRoutine(replaySameLevel));
    }

    private IEnumerator ResetRoutine(bool replaySameLevel)
    {
        SetButtonsInteractable(false);

        cardMergeBoard?.ClearSpawnedBoxGroups();
        cardMergeBoard?.ClearCards();
        itemSpawner?.ClearSpawnedItems();

        // Destroy is deferred until the end of the frame. Waiting keeps old and new
        // runtime objects from sharing layout/registry state during a refresh.
        yield return null;

        if (replaySameLevel)
        {
            cardSpawner?.ReplayLastCards();
            itemSpawner?.ReplayInitialItems();
        }
        else
        {
            cardSpawner?.SpawnConfiguredCards();
            itemSpawner?.SpawnInitialItems();
        }

        SetButtonsInteractable(true);
        _resetRoutine = null;
    }

    private void ResolveReferences()
    {
        cardSpawner ??= FarmBoxMergeObjectUtility.FindSceneComponent<CardSpawner>();
        cardMergeBoard ??= FarmBoxMergeObjectUtility.FindSceneComponent<CardMergeBoard>();
        itemSpawner ??= FarmBoxMergeObjectUtility.FindSceneComponent<MergeItemSpawner>();

        if (refreshButton == null)
        {
            Transform buttonTransform = transform.Find("RefreshButton");
            if (buttonTransform != null)
            {
                refreshButton = buttonTransform.GetComponent<Button>();
            }
        }

        if (retryButton == null)
        {
            Transform buttonTransform = transform.Find("RetryButton");
            if (buttonTransform != null)
            {
                retryButton = buttonTransform.GetComponent<Button>();
            }
        }

        if (addCardButton == null)
        {
            Transform buttonTransform = transform.Find("AddCardButton");
            if (buttonTransform != null)
            {
                addCardButton = buttonTransform.GetComponent<Button>();
            }
        }
    }

    private void ConfigureButton(Button button, UnityEngine.Events.UnityAction action, string labelText, Color color, string buttonName)
    {
        if (button == null)
        {
            Debug.LogWarning($"FarmBoxMerge {buttonName} button reference is missing.", this);
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);

        if (button.targetGraphic is Image image)
        {
            image.color = color;
        }

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.text = labelText;
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (refreshButton != null)
        {
            refreshButton.interactable = interactable;
        }

        if (retryButton != null)
        {
            retryButton.interactable = interactable;
        }

        if (addCardButton != null)
        {
            addCardButton.interactable = interactable;
        }
    }
}
