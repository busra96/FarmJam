using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Idempotent visual refresh on top of "Apply Mobile Visual Polish".
/// Only cosmetic objects/components are added; gameplay data, colours used for
/// card identity, prefab references and level assets are left untouched.
/// </summary>
public static class FarmBoxMergeVisualRefreshInstaller
{
    private const string ScenePath = "Assets/FarmBoxMerge/Scenes/FarmBoxMerge.unity";
    private const string CardPrefabPath = "Assets/FarmBoxMerge/Prefabs/Card.prefab";
    private const string ArtFolder = "Assets/FarmBoxMerge/Visuals/UI/Refresh";
    private const string RoundedSpritePath = "Assets/FarmBoxMerge/Visuals/UI/RoundedPanel.png";
    private const string BadgeFontPath = "Assets/FarmBoxMerge/Art/Fonts/Rounded/FarmBoxMerge Rounded Bold SDF.asset";
    private const string FallbackFontPath = "Assets/FarmBoxMerge/Art/Fonts/Multilingual/FarmBoxMerge Multilingual SDF.asset";

    private const string PipPath = ArtFolder + "/FBM_Pip.png";
    private const string GlowPath = ArtFolder + "/FBM_SoftGlow.png";
    private const string GlowFramePath = ArtFolder + "/FBM_GlowFrame.png";
    private const string GlossPath = ArtFolder + "/FBM_CardGloss.png";
    private const string SunburstPath = ArtFolder + "/FBM_Sunburst.png";
    private const string StarPath = ArtFolder + "/FBM_Star.png";

    [MenuItem("Tools/FarmBoxMerge/Apply Visual Refresh")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("FarmBoxMerge visual refresh can only be applied outside Play Mode.");
            return;
        }

        Scene active = SceneManager.GetActiveScene();
        if (active.isDirty)
        {
            Debug.LogWarning("Save the open scene before applying the visual refresh.");
            return;
        }

        string previousScene = active.path;
        ConfigureSprite(PipPath, 256, Vector4.zero);
        ConfigureSprite(GlowPath, 256, Vector4.zero);
        ConfigureSprite(GlowFramePath, 512, new Vector4(70f, 70f, 70f, 70f));
        ConfigureSprite(GlossPath, 256, new Vector4(40f, 40f, 40f, 40f));
        ConfigureSprite(SunburstPath, 512, Vector4.zero);
        ConfigureSprite(StarPath, 256, Vector4.zero);

        Sprite pip = AssetDatabase.LoadAssetAtPath<Sprite>(PipPath);
        Sprite glow = AssetDatabase.LoadAssetAtPath<Sprite>(GlowPath);
        Sprite glowFrame = AssetDatabase.LoadAssetAtPath<Sprite>(GlowFramePath);
        Sprite gloss = AssetDatabase.LoadAssetAtPath<Sprite>(GlossPath);
        Sprite sunburst = AssetDatabase.LoadAssetAtPath<Sprite>(SunburstPath);
        Sprite star = AssetDatabase.LoadAssetAtPath<Sprite>(StarPath);
        Sprite rounded = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        TMP_FontAsset badgeFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BadgeFontPath);
        if (badgeFont == null) badgeFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);

        if (pip == null || glow == null || gloss == null || sunburst == null || star == null || rounded == null)
        {
            Debug.LogError("FBM_VISUAL_REFRESH_FAILED missing sprites under " + ArtFolder);
            return;
        }

        ApplyCardPrefab(gloss, pip, glowFrame != null ? glowFrame : glow);

        Scene scene = active.path == ScenePath ? active : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int changes = 0;
        changes += ApplyCardSockets(rounded);
        changes += ApplyLevelBadge(scene, rounded);
        changes += ApplySlotBadges(badgeFont, glow, pip, EnsureBadgeMaterial(badgeFont));
        changes += ApplyWinPanel(scene, sunburst, star);
        changes += ApplyFailPanel(scene, glow);
        changes += ApplySettingsColors(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("FBM_VISUAL_REFRESH_COMPLETE changes=" + changes);

        if (!string.IsNullOrEmpty(previousScene) && previousScene != ScenePath)
        {
            EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }
    }

    private static void ApplyCardPrefab(Sprite gloss, Sprite pip, Sprite glow)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
        try
        {
            if (!root.TryGetComponent(out FarmBoxMergeCardDecor decor))
            {
                decor = root.AddComponent<FarmBoxMergeCardDecor>();
            }

            decor.Configure(gloss, pip, glow);
            PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static int ApplyCardSockets(Sprite rounded)
    {
        CardMergeBoard board = Object.FindFirstObjectByType<CardMergeBoard>(FindObjectsInactive.Include);
        GridLayoutGroup grid = board != null ? board.GetComponent<GridLayoutGroup>() : null;
        if (grid == null)
        {
            Debug.LogWarning("FBM visual refresh: card grid not found, sockets skipped.");
            return 0;
        }

        Transform parent = grid.transform.parent;
        Transform existing = parent.Find("FBM_CardSockets");
        GameObject sockets = existing != null
            ? existing.gameObject
            : new GameObject("FBM_CardSockets", typeof(RectTransform));
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(sockets, "Create card sockets");
            sockets.layer = grid.gameObject.layer;
            sockets.transform.SetParent(parent, false);
        }

        // Keep the sockets directly *behind* the card grid (re-runs must not move them in front).
        if (sockets.transform.GetSiblingIndex() > grid.transform.GetSiblingIndex())
        {
            sockets.transform.SetSiblingIndex(grid.transform.GetSiblingIndex());
        }
        if (!sockets.TryGetComponent(out FarmBoxMergeCardSockets mirror)) mirror = Undo.AddComponent<FarmBoxMergeCardSockets>(sockets);
        mirror.Configure(grid, rounded);
        EditorUtility.SetDirty(mirror);
        return 1;
    }

    private static int ApplyLevelBadge(Scene scene, Sprite rounded)
    {
        TextMeshProUGUI label = FindInScene<TextMeshProUGUI>(scene, "LevelLabel");
        if (label == null)
        {
            Debug.LogWarning("FBM visual refresh: LevelLabel not found.");
            return 0;
        }

        Undo.RecordObject(label, "Style level label");
        label.fontStyle = FontStyles.Bold;
        label.fontSize = Mathf.Max(label.fontSize, 52f);
        label.color = Color.white;
        label.characterSpacing = 4f;

        Transform parent = label.transform.parent;
        Transform existing = parent.Find("FBM_LevelBadge");
        GameObject badge = existing != null
            ? existing.gameObject
            : new GameObject("FBM_LevelBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow));
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(badge, "Create level badge");
            badge.layer = label.gameObject.layer;
            badge.transform.SetParent(parent, false);
        }

        if (badge.transform.GetSiblingIndex() > label.transform.GetSiblingIndex())
        {
            badge.transform.SetSiblingIndex(label.transform.GetSiblingIndex());
        }
        Image image = badge.GetComponent<Image>();
        image.sprite = rounded;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.30f, 0.56f, 0.22f, 0.92f);
        image.raycastTarget = false;
        Shadow shadow = badge.GetComponent<Shadow>();
        shadow.effectColor = new Color(0.10f, 0.16f, 0.06f, 0.40f);
        shadow.effectDistance = new Vector2(0f, -7f);
        if (!badge.TryGetComponent(out FarmBoxMergeLabelBadge follower)) follower = Undo.AddComponent<FarmBoxMergeLabelBadge>(badge);
        follower.Configure(label);
        EditorUtility.SetDirty(follower);
        return 1;
    }

    private static int ApplySlotBadges(TMP_FontAsset font, Sprite pad, Sprite disc, Material number)
    {
        GameObject root = GameObject.Find("SpawnedBoxGroups");
        if (root == null)
        {
            Debug.LogWarning("FBM visual refresh: SpawnedBoxGroups not found.");
            return 0;
        }

        int count = 0;
        for (int i = 0; i < root.transform.childCount; i++)
        {
            GameObject slot = root.transform.GetChild(i).gameObject;
            if (!slot.TryGetComponent(out FarmBoxMergeSlotBadge badge)) badge = Undo.AddComponent<FarmBoxMergeSlotBadge>(slot);
            badge.Configure(font, pad, disc, number);
            EditorUtility.SetDirty(badge);
            count++;
        }

        return count;
    }

    private static int ApplyWinPanel(Scene scene, Sprite sunburst, Sprite star)
    {
        GameObject panel = FindGameObject(scene, "WinPanel");
        TextMeshProUGUI title = FindInScene<TextMeshProUGUI>(scene, "WinnerText");
        if (panel == null || title == null)
        {
            Debug.LogWarning("FBM visual refresh: WinPanel/WinnerText not found.");
            return 0;
        }

        RectTransform titleRect = title.rectTransform;
        Image rays = EnsureImage(panel.transform, "FBM_Sunburst", sunburst, new Color(1f, 0.90f, 0.45f, 0.18f));
        RectTransform raysRect = rays.rectTransform;
        raysRect.anchorMin = raysRect.anchorMax = titleRect.anchorMin;
        raysRect.anchoredPosition = titleRect.anchoredPosition + new Vector2(0f, 40f);
        raysRect.sizeDelta = new Vector2(1250f, 1250f);
        raysRect.SetSiblingIndex(0);
        if (rays.GetComponent<FarmBoxMergeUiSpin>() == null)
        {
            Undo.AddComponent<FarmBoxMergeUiSpin>(rays.gameObject);
        }

        Transform starsRoot = panel.transform.Find("FBM_Stars");
        if (starsRoot == null)
        {
            GameObject starsObject = new GameObject("FBM_Stars", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(starsObject, "Create win stars");
            starsObject.layer = panel.layer;
            starsObject.transform.SetParent(panel.transform, false);
            starsRoot = starsObject.transform;
        }

        RectTransform starsRect = (RectTransform)starsRoot;
        starsRect.anchorMin = starsRect.anchorMax = titleRect.anchorMin;
        starsRect.anchoredPosition = titleRect.anchoredPosition + new Vector2(0f, 230f);
        starsRect.sizeDelta = new Vector2(600f, 200f);
        starsRect.SetSiblingIndex(1);

        Vector2[] positions = { new Vector2(-190f, -20f), new Vector2(0f, 20f), new Vector2(190f, -20f) };
        float[] sizes = { 130f, 170f, 130f };
        for (int i = 0; i < 3; i++)
        {
            Image starImage = EnsureImage(starsRoot, "Star" + (i + 1), star, new Color(1f, 0.80f, 0.22f, 1f));
            RectTransform rect = starImage.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = positions[i];
            rect.sizeDelta = Vector2.one * sizes[i];
            if (starImage.GetComponent<Shadow>() == null)
            {
                Shadow shadow = Undo.AddComponent<Shadow>(starImage.gameObject);
                shadow.effectColor = new Color(0.45f, 0.25f, 0f, 0.45f);
                shadow.effectDistance = new Vector2(0f, -6f);
            }

            if (!starImage.TryGetComponent(out FarmBoxMergeStarPop pop)) pop = Undo.AddComponent<FarmBoxMergeStarPop>(starImage.gameObject);
            SerializedObject serialized = new SerializedObject(pop);
            serialized.FindProperty("delay").floatValue = 0.18f + i * 0.16f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        return 1;
    }

    private static int ApplyFailPanel(Scene scene, Sprite glow)
    {
        GameObject panel = FindGameObject(scene, "FailPanel");
        TextMeshProUGUI title = FindInScene<TextMeshProUGUI>(scene, "FailText");
        if (panel == null || title == null)
        {
            return 0;
        }

        Image halo = EnsureImage(panel.transform, "FBM_FailHalo", glow, new Color(1f, 0.55f, 0.45f, 0.22f));
        RectTransform rect = halo.rectTransform;
        rect.anchorMin = rect.anchorMax = title.rectTransform.anchorMin;
        rect.anchoredPosition = title.rectTransform.anchoredPosition;
        rect.sizeDelta = new Vector2(900f, 520f);
        rect.SetSiblingIndex(0);
        return 1;
    }

    private static int ApplySettingsColors(Scene scene)
    {
        GameObject cancel = FindGameObject(scene, "Cancel-Button");
        if (cancel == null || !cancel.TryGetComponent(out Image image))
        {
            return 0;
        }

        Undo.RecordObject(image, "Soften settings close colour");
        image.color = new Color(0.91f, 0.42f, 0.36f, 1f);
        return 1;
    }

    private const string BadgeMaterialPath = "Assets/FarmBoxMerge/Visuals/Materials/FBM_SlotBadgeNumber.mat";

    private static Material EnsureBadgeMaterial(TMP_FontAsset font)
    {
        if (font == null || font.material == null)
        {
            return null;
        }

        Shader overlay = Shader.Find("TextMeshPro/Distance Field Overlay");
        if (overlay == null)
        {
            Debug.LogWarning("FBM visual refresh: TMP overlay shader not found; badge numbers use the font material.");
            return null;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(BadgeMaterialPath);
        if (material == null)
        {
            material = new Material(font.material);
            AssetDatabase.CreateAsset(material, BadgeMaterialPath);
        }
        else
        {
            material.CopyPropertiesFromMaterial(font.material);
        }

        material.shader = overlay;
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.18f);
        material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.12f, 0.25f, 0.08f, 1f));
        material.EnableKeyword(ShaderUtilities.Keyword_Outline);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Image EnsureImage(Transform parent, string name, Sprite sprite, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
        }

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private static T FindInScene<T>(Scene scene, string name) where T : Component
    {
        GameObject go = FindGameObject(scene, name);
        return go != null ? go.GetComponent<T>() : null;
    }

    private static GameObject FindGameObject(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindRecursive(root.transform, name);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindRecursive(Transform current, string name)
    {
        if (current.name == name)
        {
            return current;
        }

        for (int i = 0; i < current.childCount; i++)
        {
            Transform found = FindRecursive(current.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void ConfigureSprite(string path, int maxSize, Vector4 border)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("FBM visual refresh: texture not found " + path);
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder = border;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.sRGBTexture = true;
        importer.maxTextureSize = maxSize;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
    }
}
