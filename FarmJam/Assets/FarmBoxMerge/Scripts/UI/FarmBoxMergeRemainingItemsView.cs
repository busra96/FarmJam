using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class FarmBoxMergeRemainingItemsView : MonoBehaviour
{
    [Serializable]
    private sealed class ColorEntry
    {
        public ColorType colorType;
        public RectTransform root;
        public Image background;
        public TextMeshProUGUI countLabel;
        [NonSerialized] public CanvasGroup canvasGroup;
        [NonSerialized] public int lastCount = -1;
    }

    private static readonly ColorType[] AllColorTypes =
    {
        ColorType.Green,
        ColorType.Orange,
        ColorType.Purple,
        ColorType.Red,
        ColorType.Yellow
    };

    [SerializeField] private MergeItemSpawner itemSpawner;
    [SerializeField] private RectTransform layoutRoot;
    [SerializeField] private List<ColorEntry> entries = new List<ColorEntry>();
    [SerializeField, Range(0.1f, 1f)] private float emptyAlpha = 0.38f;
    [SerializeField, Min(0.01f)] private float countPopDuration = 0.18f;
    [SerializeField, Range(1f, 1.5f)] private float countPopScale = 1.18f;

    private readonly Dictionary<RectTransform, Coroutine> _pulseRoutines = new Dictionary<RectTransform, Coroutine>();

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (itemSpawner != null)
        {
            itemSpawner.RemainingColorCountsChanged += Refresh;
        }

        RefreshImmediate();
    }

    private void OnDisable()
    {
        if (itemSpawner != null)
        {
            itemSpawner.RemainingColorCountsChanged -= Refresh;
        }

        foreach (KeyValuePair<RectTransform, Coroutine> routine in _pulseRoutines)
        {
            if (routine.Value != null)
            {
                StopCoroutine(routine.Value);
            }

            if (routine.Key != null)
            {
                routine.Key.localScale = Vector3.one;
            }
        }

        _pulseRoutines.Clear();
    }

    public void Refresh()
    {
        RefreshEntries(animateDecrease: true);
    }

    private void RefreshImmediate()
    {
        RefreshEntries(animateDecrease: false);
    }

    private void RefreshEntries(bool animateDecrease)
    {
        if (itemSpawner == null)
        {
            return;
        }

        bool visibilityChanged = false;
        for (int i = 0; i < entries.Count; i++)
        {
            ColorEntry entry = entries[i];
            if (entry == null || entry.root == null)
            {
                continue;
            }

            bool isUsedInLevel = itemSpawner.IsColorUsedInCurrentSequence(entry.colorType);
            if (entry.root.gameObject.activeSelf != isUsedInLevel)
            {
                entry.root.gameObject.SetActive(isUsedInLevel);
                visibilityChanged = true;
            }

            if (!isUsedInLevel)
            {
                StopCountPulse(entry.root);
                entry.lastCount = -1;
                continue;
            }

            int count = itemSpawner.GetRemainingUnplacedCount(entry.colorType);
            if (entry.countLabel != null)
            {
                entry.countLabel.SetText("{0}", count);
            }

            if (entry.root != null)
            {
                if (entry.canvasGroup == null
                    && !entry.root.TryGetComponent(out entry.canvasGroup))
                {
                    entry.canvasGroup = entry.root.gameObject.AddComponent<CanvasGroup>();
                }

                entry.canvasGroup.alpha = count > 0 ? 1f : emptyAlpha;
                if (animateDecrease && entry.lastCount >= 0 && count < entry.lastCount)
                {
                    PlayCountPulse(entry.root);
                }
            }

            entry.lastCount = count;
        }

        if (visibilityChanged && layoutRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
        }
    }

    private void ResolveReferences()
    {
        itemSpawner ??= FarmBoxMergeObjectUtility.FindSceneComponent<MergeItemSpawner>();
        layoutRoot ??= transform as RectTransform;
        if (entries.Count > 0)
        {
            return;
        }

        for (int i = 0; i < AllColorTypes.Length; i++)
        {
            ColorType colorType = AllColorTypes[i];
            Transform entryTransform = transform.Find($"Remaining_{colorType}");
            if (entryTransform == null)
            {
                continue;
            }

            entries.Add(new ColorEntry
            {
                colorType = colorType,
                root = entryTransform as RectTransform,
                background = entryTransform.GetComponent<Image>(),
                countLabel = entryTransform.Find("Count")?.GetComponent<TextMeshProUGUI>()
            });
        }
    }

    private void PlayCountPulse(RectTransform target)
    {
        if (_pulseRoutines.TryGetValue(target, out Coroutine runningRoutine) && runningRoutine != null)
        {
            StopCoroutine(runningRoutine);
        }

        _pulseRoutines[target] = StartCoroutine(CountPulseRoutine(target));
    }

    private void StopCountPulse(RectTransform target)
    {
        if (!_pulseRoutines.TryGetValue(target, out Coroutine runningRoutine))
        {
            return;
        }

        if (runningRoutine != null)
        {
            StopCoroutine(runningRoutine);
        }

        target.localScale = Vector3.one;
        _pulseRoutines.Remove(target);
    }

    private IEnumerator CountPulseRoutine(RectTransform target)
    {
        target.localScale = Vector3.one * countPopScale;
        float elapsed = 0f;
        while (elapsed < countPopDuration && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = FarmBoxMergeMath.EaseOutCubic(Mathf.Clamp01(elapsed / countPopDuration));
            target.localScale = Vector3.LerpUnclamped(Vector3.one * countPopScale, Vector3.one, progress);
            yield return null;
        }

        if (target != null)
        {
            target.localScale = Vector3.one;
            _pulseRoutines.Remove(target);
        }
    }
}
