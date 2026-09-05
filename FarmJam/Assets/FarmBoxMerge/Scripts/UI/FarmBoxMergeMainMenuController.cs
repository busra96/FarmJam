using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class FarmBoxMergeMainMenuController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button playButton;
    [SerializeField] private RectTransform gameIcon;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float entranceDuration = 0.42f;

    private void Awake()
    {
        ResolveReferences();
        EnsureEventSystem();

        if (playButton == null)
        {
            Debug.LogError("FarmBoxMerge Main Menu PlayButton reference is missing.", this);
            return;
        }

        playButton.onClick.AddListener(HandlePlayClicked);
        StartCoroutine(AnimateEntrance());
    }

    private void OnDestroy()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(HandlePlayClicked);
        }
    }

    private void HandlePlayClicked()
    {
        playButton.interactable = false;
        FarmBoxMergeSceneFlow.LoadGameplay();
    }

    private void ResolveReferences()
    {
        canvasGroup ??= Object.FindFirstObjectByType<CanvasGroup>(FindObjectsInactive.Include);

        if (playButton == null)
        {
            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == "PlayButton")
                {
                    playButton = buttons[i];
                    break;
                }
            }
        }

        if (gameIcon == null && canvasGroup != null)
        {
            gameIcon = canvasGroup.transform.Find("GameIcon") as RectTransform;
        }
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        _ = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));
    }

    private IEnumerator AnimateEntrance()
    {
        RectTransform playButtonRect = playButton != null ? playButton.transform as RectTransform : null;
        if (canvasGroup == null || playButtonRect == null)
        {
            yield break;
        }

        float duration = Mathf.Max(0.01f, entranceDuration);
        canvasGroup.alpha = 0f;
        Vector3 iconScale = gameIcon != null ? gameIcon.localScale : Vector3.one;
        Vector3 buttonScale = playButtonRect.localScale;
        if (gameIcon != null)
        {
            gameIcon.localScale = iconScale * 0.82f;
        }

        playButtonRect.localScale = buttonScale * 0.86f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, progress);
            if (gameIcon != null)
            {
                gameIcon.localScale = Vector3.LerpUnclamped(iconScale * 0.82f, iconScale, eased);
            }

            playButtonRect.localScale = Vector3.LerpUnclamped(buttonScale * 0.86f, buttonScale, eased);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        if (gameIcon != null)
        {
            gameIcon.localScale = iconScale;
        }

        playButtonRect.localScale = buttonScale;
    }
}
