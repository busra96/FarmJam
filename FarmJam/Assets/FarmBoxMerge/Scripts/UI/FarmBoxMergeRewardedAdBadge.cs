using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class FarmBoxMergeRewardedAdBadge
{
    private const string BadgeObjectName = "RewardedAdBadge";
    private const string LabelObjectName = "AdLabel";

    private static readonly Color BadgeColor = new Color(1f, 0.43f, 0.14f, 1f);
    private static readonly Color ShadowColor = new Color(0.28f, 0.13f, 0.04f, 0.38f);

    public static void CreateOrUpdate(Transform target)
    {
        if (target == null)
        {
            return;
        }

        TextMeshProUGUI sourceLabel = FindSourceLabel(target);
        RectTransform badgeRect = FindOrCreateRect(target, BadgeObjectName);
        ConfigureBadgeRect(badgeRect);

        Image badgeImage = GetOrAddComponent<Image>(badgeRect.gameObject);
        Image sourceImage = target.GetComponent<Image>();
        badgeImage.sprite = sourceImage != null ? sourceImage.sprite : null;
        badgeImage.type = sourceImage != null ? sourceImage.type : Image.Type.Simple;
        badgeImage.color = BadgeColor;
        badgeImage.raycastTarget = false;

        Shadow badgeShadow = GetOrAddComponent<Shadow>(badgeRect.gameObject);
        badgeShadow.effectColor = ShadowColor;
        badgeShadow.effectDistance = new Vector2(0f, -3f);
        badgeShadow.useGraphicAlpha = true;

        RectTransform labelRect = FindOrCreateRect(badgeRect, LabelObjectName);
        ConfigureLabelRect(labelRect);

        TextMeshProUGUI label = GetOrAddComponent<TextMeshProUGUI>(labelRect.gameObject);
        label.text = "AD";
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.fontStyle = FontStyles.Bold;
        label.fontSize = 18f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 7f;
        label.fontSizeMax = 18f;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        label.margin = new Vector4(2f, 1f, 2f, 1f);

        if (sourceLabel != null && sourceLabel.font != null)
        {
            label.font = sourceLabel.font;
        }

        Outline labelOutline = GetOrAddComponent<Outline>(labelRect.gameObject);
        labelOutline.effectColor = new Color(0.38f, 0.16f, 0.04f, 0.55f);
        labelOutline.effectDistance = new Vector2(1f, -1f);
        labelOutline.useGraphicAlpha = true;

        badgeRect.SetAsLastSibling();
        target.GetComponentInParent<FarmBoxMergeCanvasLocalization>()?.RegisterText(label, sourceLabel);
    }

    public static void ApplyLocalization(TMP_Text label, IFarmBoxMergeLocalizationService localization)
    {
        if (label.name == LabelObjectName && label.transform.parent.name == BadgeObjectName)
            label.text = localization.Get("ad");
    }

    public static void SetVisible(Transform target, bool visible)
    {
        if (target == null)
        {
            return;
        }

        Transform badge = target.Find(BadgeObjectName);
        if (badge == null && visible)
        {
            CreateOrUpdate(target);
            badge = target.Find(BadgeObjectName);
        }

        if (badge != null && badge.gameObject.activeSelf != visible)
        {
            badge.gameObject.SetActive(visible);
        }
    }

    public static TextMeshProUGUI FindPrimaryLabel(Transform target)
    {
        return FindSourceLabel(target);
    }

    private static TextMeshProUGUI FindSourceLabel(Transform target)
    {
        TextMeshProUGUI[] labels = target.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] != null && labels[i].transform.name != LabelObjectName)
            {
                return labels[i];
            }
        }

        return null;
    }

    private static RectTransform FindOrCreateRect(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing is RectTransform existingRect)
        {
            return existingRect;
        }

        GameObject created = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        RectTransform rect = created.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void ConfigureBadgeRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-7f, -7f);
        rect.sizeDelta = new Vector2(50f, 32f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.Euler(0f, 0f, -5f);
    }

    private static void ConfigureLabelRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        return target.TryGetComponent(out T component) ? component : target.AddComponent<T>();
    }
}
