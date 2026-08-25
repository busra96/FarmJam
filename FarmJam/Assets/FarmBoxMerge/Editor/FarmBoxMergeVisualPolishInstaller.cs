using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FarmBoxMergeVisualPolishInstaller
{
    private const string ScenePath = "Assets/FarmBoxMerge/FarmBoxMerge.unity";
    private const string CardPrefabPath = "Assets/FarmBoxMerge/Prefabs/Card.prefab";
    private const string BackdropPath = "Assets/FarmBoxMerge/Visuals/Backgrounds/FarmBackdrop.png";
    private const string CrateIconPath = "Assets/FarmBoxMerge/Visuals/UI/FarmCrateIcon.png";
    private const string RoundedSpritePath = "Assets/FarmBoxMerge/Visuals/UI/RoundedPanel.png";
    private const string PlatformWoodTexturePath = "Assets/FarmBoxMerge/Visuals/Environment/FarmPlatformWood.png";
    private const string PlatformWoodMaterialPath = "Assets/FarmBoxMerge/Visuals/Materials/FarmPlatformWood.mat";
    private const string PlatformRunnerMaterialPath = "Assets/FarmBoxMerge/Visuals/Materials/FarmPlatformRunner.mat";
    private const string PlatformAccentMaterialPath = "Assets/FarmBoxMerge/Visuals/Materials/FarmPlatformAccent.mat";

    private static readonly Color Cream = Hex("FFF7DF");
    private static readonly Color Ink = Hex("403629");
    private static readonly Color Green = Hex("65AD62");
    private static readonly Color Blue = Hex("4F9FCA");
    private static readonly Color Orange = Hex("E99B48");
    private static readonly Color Red = Hex("D85B54");

    [MenuItem("Tools/FarmBoxMerge/Apply Mobile Visual Polish")]
    public static void ApplyPolish()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("FarmBoxMerge visual polish can only be applied outside Play Mode.");
            return;
        }

        EnsureRoundedSprite();
        ConfigureSpriteImporter(BackdropPath, alpha: false, border: Vector4.zero, maxSize: 2048);
        ConfigureSpriteImporter(CrateIconPath, alpha: true, border: Vector4.zero, maxSize: 1024);
        ConfigureSpriteImporter(RoundedSpritePath, alpha: true, border: new Vector4(22f, 22f, 22f, 22f), maxSize: 128);
        ConfigurePlatformTextureImporter();

        Sprite backdropSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackdropPath);
        Sprite crateSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CrateIconPath);
        Sprite roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        Texture2D platformWoodTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PlatformWoodTexturePath);

        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        ApplyWorldPolish(scene, backdropSprite, platformWoodTexture);
        ApplyUiPolish(scene, roundedSprite);
        ApplyCardPrefabPolish(crateSprite, roundedSprite);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        FarmBoxMergeFeedbackInstaller.ApplyGameFeelPolish();
        Debug.Log("FBM_VISUAL_POLISH_COMPLETE");
    }

    [MenuItem("Tools/FarmBoxMerge/Apply Platform Polish")]
    public static void ApplyPlatformPolishOnly()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("FarmBoxMerge platform polish can only be applied outside Play Mode.");
            return;
        }

        ConfigurePlatformTextureImporter();
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Texture2D platformWoodTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PlatformWoodTexturePath);
        ApplyPlatformPolish(scene, platformWoodTexture);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("FBM_PLATFORM_POLISH_COMPLETE");
    }

    private static void ApplyWorldPolish(Scene scene, Sprite backdropSprite, Texture2D platformWoodTexture)
    {
        Camera camera = FindInScene<Camera>(scene, "Main Camera");
        if (camera != null)
        {
            Undo.RecordObject(camera, "Polish FarmBoxMerge camera");
            Undo.RecordObject(camera.transform, "Polish FarmBoxMerge camera");
            camera.fieldOfView = 50f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 400f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("B9DFE7");
            camera.transform.SetPositionAndRotation(new Vector3(0f, 35f, -18.8f), Quaternion.Euler(60f, 0f, 0f));
            ConfigureAdditionalCameraData(camera);
            CreateOrUpdateBackdrop(camera, backdropSprite);
        }

        DisableLegacyRenderer(scene, "Plane");
        DisableLegacyRenderer(scene, "Plane (1)");

        Light keyLight = FindInScene<Light>(scene, "Directional Light");
        if (keyLight != null)
        {
            Undo.RecordObject(keyLight, "Polish FarmBoxMerge lighting");
            Undo.RecordObject(keyLight.transform, "Polish FarmBoxMerge lighting");
            keyLight.color = Hex("FFF1CE");
            keyLight.intensity = 1.15f;
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.65f;
            keyLight.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Hex("BBD5C1");

        MergeItemSpawner itemSpawner = FindInScene<MergeItemSpawner>(scene, "MergeItemSpawner");
        if (itemSpawner != null)
        {
            Undo.RecordObject(itemSpawner.transform, "Center FarmBoxMerge item queue");
            itemSpawner.transform.position = new Vector3(0f, 4f, 6.9f);

            SerializedObject serializedSpawner = new SerializedObject(itemSpawner);
            serializedSpawner.FindProperty("maxVisibleItems").intValue = 6;
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

            Transform queueRoot = itemSpawner.transform.Find("ItemQueuePoints");
            if (queueRoot != null)
            {
                for (int i = 0; i < queueRoot.childCount; i++)
                {
                    Transform point = queueRoot.GetChild(i);
                    Undo.RecordObject(point, "Fit FarmBoxMerge item queue");
                    point.localPosition = new Vector3((i - 2.5f) * 1.8f, 0f, 0f);
                }
            }
        }

        ApplyPlatformPolish(scene, platformWoodTexture);
    }

    private static void ApplyPlatformPolish(Scene scene, Texture2D woodTexture)
    {
        GameObject platform = FindInSceneObject(scene, "Platform");
        if (platform == null)
        {
            Debug.LogWarning("FarmBoxMerge Platform object could not be found.");
            return;
        }

        Material woodMaterial = CreateOrUpdateMaterial(PlatformWoodMaterialPath, woodTexture, Color.white, 0.24f);
        Material runnerMaterial = CreateOrUpdateMaterial(PlatformRunnerMaterialPath, null, Hex("6F9A55"), 0.28f);
        Material accentMaterial = CreateOrUpdateMaterial(PlatformAccentMaterialPath, null, Hex("F6E1A8"), 0.22f);

        Undo.RecordObject(platform.transform, "Polish FarmBoxMerge platform");
        platform.transform.localRotation = Quaternion.identity;
        platform.transform.localScale = Vector3.one;

        Transform top = platform.transform.Find("FarmTableTop");
        if (top == null)
        {
            top = platform.transform.Find("Cube");
            if (top != null)
            {
                top.name = "FarmTableTop";
            }
        }

        top ??= CreatePlatformCube(platform.transform, "FarmTableTop");
        ConfigurePlatformPart(top, new Vector3(0f, 0.34f, 0f), new Vector3(13.2f, 0.42f, 2.8f), woodMaterial);

        Transform runner = GetOrCreatePlatformPart(platform.transform, "ProduceRunner");
        ConfigurePlatformPart(runner, new Vector3(0f, 0.59f, 0f), new Vector3(12.35f, 0.08f, 1.48f), runnerMaterial);

        Transform frontApron = GetOrCreatePlatformPart(platform.transform, "FrontApron");
        ConfigurePlatformPart(frontApron, new Vector3(0f, -0.15f, -1.42f), new Vector3(13.45f, 0.82f, 0.22f), woodMaterial);

        Transform frontAccent = GetOrCreatePlatformPart(platform.transform, "FrontAccent");
        ConfigurePlatformPart(frontAccent, new Vector3(0f, 0.19f, -1.56f), new Vector3(13.55f, 0.16f, 0.18f), accentMaterial);

        ConfigurePlatformPart(GetOrCreatePlatformPart(platform.transform, "FrontPostLeft"), new Vector3(-6.25f, -0.78f, -1.18f), new Vector3(0.42f, 1.55f, 0.42f), woodMaterial);
        ConfigurePlatformPart(GetOrCreatePlatformPart(platform.transform, "FrontPostRight"), new Vector3(6.25f, -0.78f, -1.18f), new Vector3(0.42f, 1.55f, 0.42f), woodMaterial);
        ConfigurePlatformPart(GetOrCreatePlatformPart(platform.transform, "BackPostLeft"), new Vector3(-6.25f, -0.78f, 1.18f), new Vector3(0.42f, 1.55f, 0.42f), woodMaterial);
        ConfigurePlatformPart(GetOrCreatePlatformPart(platform.transform, "BackPostRight"), new Vector3(6.25f, -0.78f, 1.18f), new Vector3(0.42f, 1.55f, 0.42f), woodMaterial);
    }

    private static Transform GetOrCreatePlatformPart(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        return existing != null ? existing : CreatePlatformCube(parent, name);
    }

    private static Transform CreatePlatformCube(Transform parent, string name)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.layer = parent.gameObject.layer;
        part.transform.SetParent(parent, false);
        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        Undo.RegisterCreatedObjectUndo(part, "Create FarmBoxMerge platform detail");
        return part.transform;
    }

    private static void ConfigurePlatformPart(Transform part, Vector3 localPosition, Vector3 localScale, Material material)
    {
        Undo.RecordObject(part, "Style FarmBoxMerge platform detail");
        part.localPosition = localPosition;
        part.localRotation = Quaternion.identity;
        part.localScale = localScale;

        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            renderer = part.gameObject.AddComponent<MeshRenderer>();
        }

        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        renderer.receiveShadows = true;
    }

    private static void ApplyUiPolish(Scene scene, Sprite roundedSprite)
    {
        Canvas canvas = FindInScene<Canvas>(scene, "Canvas");
        if (canvas == null)
        {
            return;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            Undo.RecordObject(scaler, "Make FarmBoxMerge UI responsive");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        TMP_FontAsset font = FindInScene<TextMeshProUGUI>(scene)?.font;
        RectTransform canvasRect = canvas.transform as RectTransform;

        TextMeshProUGUI title = CreateOrUpdateLabel(canvasRect, "GameTitle", "FARM BOX MERGE", font, 56f, Ink);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(800f, 76f), new Vector2(0.5f, 1f));

        TextMeshProUGUI level = CreateOrUpdateLabel(canvasRect, "LevelLabel", "LEVEL 1", font, 31f, Hex("5B6E45"));
        SetRect(level.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -111f), new Vector2(420f, 52f), new Vector2(0.5f, 1f));

        CreateOrUpdateQueuePill(canvasRect, font, roundedSprite);
        CreateOrUpdateRemainingItemsPanel(scene, canvasRect, font, roundedSprite);

        StyleTopButton(FindInScene<Button>(scene, "AddCardButton"), new Vector2(-360f, -185f), Blue, roundedSprite);
        StyleTopSurface(FindInSceneObject(scene, "TrashDropZone"), new Vector2(-120f, -185f), Red, roundedSprite);
        StyleTopButton(FindInScene<Button>(scene, "RefreshButton"), new Vector2(120f, -185f), Green, roundedSprite);
        StyleTopButton(FindInScene<Button>(scene, "RetryButton"), new Vector2(360f, -185f), Orange, roundedSprite);

        GameObject trayObject = FindInSceneObject(scene, "Panel");
        if (trayObject != null && trayObject.transform.parent == canvas.transform)
        {
            RectTransform trayRect = trayObject.GetComponent<RectTransform>();
            SetRect(trayRect, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(-36f, 700f), new Vector2(0.5f, 0f));
            StyleImage(trayObject.GetComponent<Image>(), Cream.WithAlpha(0.96f), roundedSprite);
            StyleShadow(trayObject, new Color(0.16f, 0.22f, 0.12f, 0.30f), new Vector2(0f, 12f));

            TextMeshProUGUI cardsTitle = CreateOrUpdateLabel(trayRect, "CardsTitle", "MERGE CARDS", font, 34f, Ink);
            SetRect(cardsTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(500f, 55f), new Vector2(0.5f, 1f));
        }

        CardMergeBoard board = FindInScene<CardMergeBoard>(scene);
        if (board != null)
        {
            GameObject boardObject = board.gameObject;
            RectTransform boardRect = boardObject.GetComponent<RectTransform>();
            if (boardRect != null)
            {
                boardRect.anchorMin = Vector2.zero;
                boardRect.anchorMax = Vector2.one;
                boardRect.offsetMin = new Vector2(36f, 28f);
                boardRect.offsetMax = new Vector2(-36f, -78f);
            }

            GridLayoutGroup grid = boardObject.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                Undo.RecordObject(grid, "Polish FarmBoxMerge card grid");
                grid.padding = new RectOffset(34, 34, 26, 24);
                grid.cellSize = new Vector2(211f, 169f);
                grid.spacing = new Vector2(20f, 18f);
                grid.childAlignment = TextAnchor.UpperCenter;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 4;
            }

            ConfigureBoardTheme(board);
            ConfigureCardSpawnerTheme(boardObject.GetComponent<CardSpawner>());
        }

        ConfigureGameControllerTheme(canvas.GetComponent<FarmBoxMergeGameController>());
        ConfigurePresentationController(canvas.gameObject, level);
        StyleOutcomePanel(scene, "WinPanel", "WinnerText", "HARVEST COMPLETE!", "NextLevelButton", Green, roundedSprite);
        StyleOutcomePanel(scene, "FailPanel", "LoserText", "ONE MORE TRY!", "RetryLevelButton", Orange, roundedSprite);
    }

    private static void ConfigureGameControllerTheme(FarmBoxMergeGameController controller)
    {
        if (controller == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("buttonColor").colorValue = Green;
        serialized.FindProperty("retryButtonColor").colorValue = Orange;
        serialized.FindProperty("addCardButtonColor").colorValue = Blue;
        serialized.FindProperty("refreshLabel").stringValue = "REFRESH";
        serialized.FindProperty("retryLabel").stringValue = "RETRY";
        serialized.FindProperty("addCardLabel").stringValue = "ADD CARD";
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureBoardTheme(CardMergeBoard board)
    {
        if (board == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(board);
        SerializedProperty palette = serialized.FindProperty("colorPalette");
        Color[] colors =
        {
            Hex("76B947"), Hex("F39A3F"), Hex("A875C4"), Hex("E45D5D"), Hex("F2C94C")
        };

        palette.arraySize = colors.Length;
        for (int i = 0; i < colors.Length; i++)
        {
            SerializedProperty entry = palette.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("colorType").enumValueIndex = i;
            entry.FindPropertyRelative("color").colorValue = colors[i];
        }

        serialized.FindProperty("trashAvailableColor").colorValue = Red.WithAlpha(0.96f);
        serialized.FindProperty("trashUnavailableColor").colorValue = Hex("897F72").WithAlpha(0.72f);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureCardSpawnerTheme(CardSpawner spawner)
    {
        if (spawner == null)
        {
            return;
        }

        Color[] colors =
        {
            Hex("76B947"), Hex("F39A3F"), Hex("A875C4"), Hex("E45D5D"), Hex("F2C94C")
        };

        SerializedObject serialized = new SerializedObject(spawner);
        SerializedProperty palette = serialized.FindProperty("availableColors");
        palette.arraySize = colors.Length;
        for (int i = 0; i < colors.Length; i++)
        {
            SerializedProperty entry = palette.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("colorType").enumValueIndex = i;
            entry.FindPropertyRelative("color").colorValue = colors[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigurePresentationController(GameObject canvas, TextMeshProUGUI levelLabel)
    {
        FarmBoxMergePresentationController presentation = canvas.GetComponent<FarmBoxMergePresentationController>();
        if (presentation == null)
        {
            presentation = Undo.AddComponent<FarmBoxMergePresentationController>(canvas);
        }

        SerializedObject serialized = new SerializedObject(presentation);
        serialized.FindProperty("gameController").objectReferenceValue = canvas.GetComponent<FarmBoxMergeGameController>();
        serialized.FindProperty("levelRuntime").objectReferenceValue = canvas.GetComponent<FarmBoxMergeLevelRuntime>();
        serialized.FindProperty("levelLabel").objectReferenceValue = levelLabel;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateOrUpdateQueuePill(RectTransform canvas, TMP_FontAsset font, Sprite roundedSprite)
    {
        Transform existing = canvas.Find("QueuePill");
        GameObject pill = existing != null ? existing.gameObject : new GameObject("QueuePill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = pill.GetComponent<RectTransform>();
        rect.SetParent(canvas, false);
        SetRect(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -320f), new Vector2(330f, 58f), new Vector2(0.5f, 1f));
        StyleImage(pill.GetComponent<Image>(), Cream.WithAlpha(0.90f), roundedSprite);
        pill.GetComponent<Image>().raycastTarget = false;

        TextMeshProUGUI label = CreateOrUpdateLabel(rect, "QueueText", "NEXT PRODUCE", font, 24f, Ink.WithAlpha(0.82f));
        SetStretch(label.rectTransform, 12f);
    }

    [MenuItem("Tools/FarmBoxMerge/Apply Remaining Items HUD")]
    public static void ApplyRemainingItemsHudOnly()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("FarmBoxMerge remaining-items HUD can only be applied outside Play Mode.");
            return;
        }

        ConfigureSpriteImporter(RoundedSpritePath, alpha: true, border: new Vector4(22f, 22f, 22f, 22f), maxSize: 128);
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        Canvas canvas = FindInScene<Canvas>(scene, "Canvas");
        if (canvas == null)
        {
            Debug.LogWarning("FarmBoxMerge Canvas could not be found.");
            return;
        }

        TMP_FontAsset font = FindInScene<TextMeshProUGUI>(scene)?.font;
        Sprite roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        CreateOrUpdateRemainingItemsPanel(scene, canvas.transform as RectTransform, font, roundedSprite);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("FBM_REMAINING_ITEMS_HUD_COMPLETE");
    }

    private static void CreateOrUpdateRemainingItemsPanel(Scene scene, RectTransform canvas, TMP_FontAsset font, Sprite roundedSprite)
    {
        if (canvas == null)
        {
            return;
        }

        Transform existing = canvas.Find("RemainingItemsPanel");
        GameObject panel = existing != null
            ? existing.gameObject
            : new GameObject("RemainingItemsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(canvas, false);
        SetRect(panelRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -394f), new Vector2(760f, 96f), new Vector2(0.5f, 1f));
        StyleImage(panel.GetComponent<Image>(), Cream.WithAlpha(0.94f), roundedSprite);
        panel.GetComponent<Image>().raycastTarget = false;
        StyleShadow(panel, new Color(0.16f, 0.22f, 0.12f, 0.24f), new Vector2(0f, -6f));

        HorizontalLayoutGroup panelLayout = panel.GetComponent<HorizontalLayoutGroup>();
        if (panelLayout == null)
        {
            panelLayout = Undo.AddComponent<HorizontalLayoutGroup>(panel);
        }

        panelLayout.padding = new RectOffset(18, 18, 12, 12);
        panelLayout.spacing = 12f;
        panelLayout.childAlignment = TextAnchor.MiddleCenter;
        panelLayout.childControlWidth = false;
        panelLayout.childControlHeight = false;
        panelLayout.childForceExpandWidth = false;
        panelLayout.childForceExpandHeight = false;

        ContentSizeFitter panelFitter = panel.GetComponent<ContentSizeFitter>();
        if (panelFitter == null)
        {
            panelFitter = Undo.AddComponent<ContentSizeFitter>(panel);
        }

        panelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        panelFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        TextMeshProUGUI title = CreateOrUpdateLabel(panelRect, "RemainingTitle", "ITEMS\nLEFT", font, 21f, Ink.WithAlpha(0.82f));
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-330f, 0f), new Vector2(130f, 70f), Vector2.one * 0.5f);
        LayoutElement titleLayout = title.GetComponent<LayoutElement>();
        if (titleLayout == null)
        {
            titleLayout = Undo.AddComponent<LayoutElement>(title.gameObject);
        }

        titleLayout.preferredWidth = 130f;
        titleLayout.preferredHeight = 70f;

        string[] names = { "GREEN", "ORANGE", "PURPLE", "RED", "YELLOW" };
        Color[] colors = { Hex("76B947"), Hex("F39A3F"), Hex("A875C4"), Hex("E45D5D"), Hex("F2C94C") };
        RectTransform[] entryRoots = new RectTransform[colors.Length];
        Image[] backgrounds = new Image[colors.Length];
        TextMeshProUGUI[] countLabels = new TextMeshProUGUI[colors.Length];

        for (int i = 0; i < colors.Length; i++)
        {
            string entryName = $"Remaining_{(ColorType)i}";
            Transform existingEntry = panelRect.Find(entryName);
            GameObject entry = existingEntry != null
                ? existingEntry.gameObject
                : new GameObject(entryName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            RectTransform entryRect = entry.GetComponent<RectTransform>();
            entryRect.SetParent(panelRect, false);
            SetRect(entryRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-220f + (i * 110f), 0f), new Vector2(96f, 68f), Vector2.one * 0.5f);
            LayoutElement entryLayout = entry.GetComponent<LayoutElement>();
            if (entryLayout == null)
            {
                entryLayout = Undo.AddComponent<LayoutElement>(entry);
            }

            entryLayout.preferredWidth = 96f;
            entryLayout.preferredHeight = 68f;

            Image entryImage = entry.GetComponent<Image>();
            StyleImage(entryImage, colors[i], roundedSprite);
            entryImage.raycastTarget = false;

            TextMeshProUGUI nameLabel = CreateOrUpdateLabel(entryRect, "Name", names[i], font, 12f, Color.white.WithAlpha(0.88f));
            nameLabel.fontStyle = FontStyles.Bold;
            nameLabel.alignment = TextAlignmentOptions.Center;
            nameLabel.enableAutoSizing = true;
            nameLabel.fontSizeMin = 8f;
            nameLabel.fontSizeMax = 12f;
            SetRect(nameLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -5f), new Vector2(88f, 20f), new Vector2(0.5f, 1f));

            TextMeshProUGUI countLabel = CreateOrUpdateLabel(entryRect, "Count", "0", font, 30f, Color.white);
            countLabel.fontStyle = FontStyles.Bold;
            countLabel.alignment = TextAlignmentOptions.Center;
            SetRect(countLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(88f, 42f), new Vector2(0.5f, 0f));

            entryRoots[i] = entryRect;
            backgrounds[i] = entryImage;
            countLabels[i] = countLabel;
        }

        FarmBoxMergeRemainingItemsView view = panel.GetComponent<FarmBoxMergeRemainingItemsView>();
        if (view == null)
        {
            view = Undo.AddComponent<FarmBoxMergeRemainingItemsView>(panel);
        }

        SerializedObject serializedView = new SerializedObject(view);
        serializedView.FindProperty("itemSpawner").objectReferenceValue = FindInScene<MergeItemSpawner>(scene);
        serializedView.FindProperty("layoutRoot").objectReferenceValue = panelRect;
        SerializedProperty entries = serializedView.FindProperty("entries");
        entries.arraySize = colors.Length;
        for (int i = 0; i < colors.Length; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("colorType").enumValueIndex = i;
            entry.FindPropertyRelative("root").objectReferenceValue = entryRoots[i];
            entry.FindPropertyRelative("background").objectReferenceValue = backgrounds[i];
            entry.FindPropertyRelative("countLabel").objectReferenceValue = countLabels[i];
        }

        serializedView.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void StyleTopButton(Button button, Vector2 position, Color color, Sprite sprite)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.transform as RectTransform;
        SetRect(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, new Vector2(220f, 94f), new Vector2(0.5f, 1f));
        StyleImage(button.targetGraphic as Image, color, sprite);
        StyleShadow(button.gameObject, new Color(0.18f, 0.18f, 0.12f, 0.32f), new Vector2(0f, -7f));
        ConfigureButtonTransition(button);

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = true;
            label.fontSizeMin = 19f;
            label.fontSizeMax = 31f;
            label.margin = new Vector4(10f, 5f, 10f, 5f);
        }
    }

    private static void StyleTopSurface(GameObject surface, Vector2 position, Color color, Sprite sprite)
    {
        if (surface == null)
        {
            return;
        }

        SetRect(surface.transform as RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, new Vector2(220f, 94f), new Vector2(0.5f, 1f));
        StyleImage(surface.GetComponent<Image>(), color, sprite);
        StyleShadow(surface, new Color(0.18f, 0.18f, 0.12f, 0.32f), new Vector2(0f, -7f));

        TextMeshProUGUI label = surface.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = true;
            label.fontSizeMin = 19f;
            label.fontSizeMax = 31f;
            label.margin = new Vector4(10f, 5f, 10f, 5f);
        }
    }

    private static void StyleOutcomePanel(Scene scene, string panelName, string textName, string title, string buttonName, Color accent, Sprite roundedSprite)
    {
        GameObject panel = FindInSceneObject(scene, panelName);
        if (panel == null)
        {
            return;
        }

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = new Color(0.12f, 0.18f, 0.12f, 0.90f);
            panelImage.sprite = null;
            panelImage.type = Image.Type.Simple;
        }

        Transform textTransform = FindDescendant(panel.transform, textName);
        TextMeshProUGUI text = textTransform != null ? textTransform.GetComponent<TextMeshProUGUI>() : null;
        if (text != null)
        {
            text.text = title;
            text.color = Cream;
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = 54f;
            text.fontSizeMax = 110f;
            SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 135f), new Vector2(880f, 240f), new Vector2(0.5f, 0.5f));
        }

        Transform buttonTransform = FindDescendant(panel.transform, buttonName);
        Button button = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
        if (button != null)
        {
            SetRect(button.transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -125f), new Vector2(520f, 130f), new Vector2(0.5f, 0.5f));
            StyleImage(button.targetGraphic as Image, accent, roundedSprite);
            StyleShadow(button.gameObject, new Color(0f, 0f, 0f, 0.35f), new Vector2(0f, -9f));
            ConfigureButtonTransition(button);
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.color = Color.white;
                label.fontStyle = FontStyles.Bold;
                label.fontSize = 45f;
            }
        }
    }

    private static void ApplyCardPrefabPolish(Sprite crateSprite, Sprite roundedSprite)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
        try
        {
            Transform panelTransform = FindDescendant(root.transform, "Panel");
            Image panel = panelTransform != null ? panelTransform.GetComponent<Image>() : null;
            StyleImage(panel, Hex("FFF9E9"), roundedSprite);
            if (panelTransform != null)
            {
                StyleShadow(panelTransform.gameObject, new Color(0.14f, 0.18f, 0.08f, 0.34f), new Vector2(0f, -8f));
            }

            Transform colorTransform = FindDescendant(root.transform, "Background Color");
            Image colorImage = colorTransform != null ? colorTransform.GetComponent<Image>() : null;
            if (colorImage != null)
            {
                colorImage.sprite = roundedSprite;
                colorImage.type = Image.Type.Sliced;
                RectTransform rect = colorImage.rectTransform;
                rect.anchorMin = new Vector2(0.045f, 0.055f);
                rect.anchorMax = new Vector2(0.955f, 0.945f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            Transform iconTransform = FindDescendant(root.transform, "Box Icon");
            Image icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            if (icon != null)
            {
                icon.sprite = crateSprite;
                icon.preserveAspect = true;
                icon.color = Color.white;
                RectTransform rect = icon.rectTransform;
                rect.anchorMin = new Vector2(0.22f, 0.34f);
                rect.anchorMax = new Vector2(0.78f, 0.94f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            Transform counterTransform = FindDescendant(root.transform, "Counter");
            TextMeshProUGUI counter = counterTransform != null ? counterTransform.GetComponent<TextMeshProUGUI>() : null;
            if (counter != null)
            {
                counter.color = Color.white;
                counter.fontStyle = FontStyles.Bold;
                counter.enableAutoSizing = true;
                counter.fontSizeMin = 42f;
                counter.fontSizeMax = 78f;
                RectTransform rect = counter.rectTransform;
                rect.anchorMin = new Vector2(0f, 0.02f);
                rect.anchorMax = new Vector2(1f, 0.43f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureAdditionalCameraData(Camera camera)
    {
        Component[] components = camera.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component == null || component.GetType().Name != "UniversalAdditionalCameraData")
            {
                continue;
            }

            SerializedObject serialized = new SerializedObject(component);
            SetBool(serialized, "m_RenderPostProcessing", true);
            SetInt(serialized, "m_Antialiasing", 2);
            SetInt(serialized, "m_AntialiasingQuality", 2);
            SetBool(serialized, "m_StopNaN", true);
            SetBool(serialized, "m_Dithering", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void CreateOrUpdateBackdrop(Camera camera, Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        Transform backdropTransform = camera.transform.Find("FarmBackdropVisual");
        GameObject backdrop = backdropTransform != null
            ? backdropTransform.gameObject
            : new GameObject("FarmBackdropVisual", typeof(SpriteRenderer));

        backdrop.transform.SetParent(camera.transform, false);
        backdrop.transform.localPosition = new Vector3(0f, 0f, 150f);
        backdrop.transform.localRotation = Quaternion.identity;

        float requiredHeight = 2f * 150f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float scale = requiredHeight / Mathf.Max(0.01f, sprite.bounds.size.y) * 1.04f;
        backdrop.transform.localScale = Vector3.one * scale;

        SpriteRenderer renderer = backdrop.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingOrder = -1000;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static TextMeshProUGUI CreateOrUpdateLabel(RectTransform parent, string name, string text, TMP_FontAsset font, float size, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject labelObject = existing != null
            ? existing.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));

        labelObject.transform.SetParent(parent, false);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        if (font != null)
        {
            label.font = font;
        }

        label.fontSize = size;
        label.fontStyle = FontStyles.Bold;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    private static void StyleImage(Image image, Color color, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

        image.color = color;
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
    }

    private static void StyleShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = target.AddComponent<Shadow>();
        }

        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static void ConfigureButtonTransition(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.55f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void SetStretch(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * padding;
        rect.offsetMax = Vector2.one * -padding;
    }

    private static void DisableLegacyRenderer(Scene scene, string objectName)
    {
        GameObject legacy = FindInSceneObject(scene, objectName);
        if (legacy == null)
        {
            return;
        }

        Renderer[] renderers = legacy.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Undo.RecordObject(renderer, "Hide legacy FarmBoxMerge background");
            renderer.enabled = false;
        }
    }

    private static void EnsureRoundedSprite()
    {
        if (File.Exists(RoundedSpritePath))
        {
            return;
        }

        const int size = 64;
        const float radius = 17f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float closestX = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float closestY = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(closestX, closestY));
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius + 0.5f - distance) * 255f);
                pixels[(y * size) + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(RoundedSpritePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(RoundedSpritePath, ImportAssetOptions.ForceSynchronousImport);
    }

    private static void ConfigureSpriteImporter(string path, bool alpha, Vector4 border, int maxSize)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Sprite importer not found: {path}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder = border;
        importer.alphaIsTransparency = alpha;
        importer.mipmapEnabled = false;
        importer.sRGBTexture = true;
        importer.maxTextureSize = maxSize;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
    }

    private static void ConfigurePlatformTextureImporter()
    {
        AssetDatabase.ImportAsset(PlatformWoodTexturePath, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(PlatformWoodTexturePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Platform texture importer not found: {PlatformWoodTexturePath}");
            return;
        }

        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.alphaIsTransparency = false;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.anisoLevel = 2;
        importer.maxTextureSize = 1024;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
    }

    private static Material CreateOrUpdateMaterial(string path, Texture texture, Color color, float smoothness)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                name = Path.GetFileNameWithoutExtension(path)
            };
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", new Vector2(1.5f, 1f));
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
            material.SetTextureScale("_MainTex", new Vector2(1.5f, 1f));
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static T FindInScene<T>(Scene scene, string objectName = null) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            foreach (T component in components)
            {
                if (string.IsNullOrEmpty(objectName) || component.gameObject.name == objectName)
                {
                    return component;
                }
            }
        }

        return null;
    }

    private static GameObject FindInSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindDescendant(root.transform, objectName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDescendant(root.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void SetBool(SerializedObject serialized, string propertyName, bool value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.boolValue = value;
        }
    }

    private static void SetInt(SerializedObject serialized, string propertyName, int value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString($"#{value}", out Color color);
        return color;
    }

    private static Color WithAlpha(this Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
