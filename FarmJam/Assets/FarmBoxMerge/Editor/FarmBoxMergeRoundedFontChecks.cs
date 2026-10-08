using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class FarmBoxMergeRoundedFontChecks
{
    [MenuItem("Tools/FarmBoxMerge/Localization/Preview Rounded Font Comparison")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Stop Play Mode and wait for compilation before previewing fonts.");

        var catalog = AssetDatabase.LoadAssetAtPath<FarmBoxMergeLocalizationCatalog>(FarmBoxMergeLocalizationInstaller.CatalogPath);
        var originalAssignments = catalog.fonts.Select(entry => entry.font).ToArray();
        FarmBoxMergeSharedFontBuilder.BuildRounded();
        string guid = AssetDatabase.AssetPathToGUID(FarmBoxMergeSharedFontBuilder.RoundedFontPath);
        FarmBoxMergeSharedFontBuilder.BuildRounded();
        if (guid != AssetDatabase.AssetPathToGUID(FarmBoxMergeSharedFontBuilder.RoundedFontPath) ||
            !originalAssignments.SequenceEqual(catalog.fonts.Select(entry => entry.font)))
            throw new InvalidOperationException("Rebuilding the alternative changed asset identity or active fonts.");

        var rounded = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FarmBoxMergeSharedFontBuilder.RoundedFontPath);
        var original = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FarmBoxMergeSharedFontBuilder.FontPath);
        if (rounded.atlasPopulationMode != AtlasPopulationMode.Static || rounded.sourceFontFile != null)
            throw new InvalidOperationException("The alternative must not require runtime font generation.");

        string characters = FarmBoxMergeSharedFontBuilder.RequiredCharacters(catalog);
        if (!rounded.HasCharacters(characters, out uint[] missing, false, false))
            throw new InvalidOperationException("Missing font characters: " + string.Join(",", missing));

        WritePreview(catalog, original, rounded);
        File.AppendAllText("Logs/FarmBoxMergeTests/rounded-font.txt",
            "\nPASS repeat rebuild preserves asset GUID and current font selections; four-language comparison rendered.");
    }

    private static void WritePreview(FarmBoxMergeLocalizationCatalog catalog, TMP_FontAsset original, TMP_FontAsset rounded)
    {
        GameObject canvasRoot = null, cameraRoot = null;
        RenderTexture target = null;
        Texture2D capture = null;
        RenderTexture previous = RenderTexture.active;
        try
        {
            canvasRoot = new GameObject("Temporary Font Comparison", typeof(RectTransform), typeof(Canvas));
            canvasRoot.hideFlags = HideFlags.HideAndDontSave;
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = canvasRoot.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1280, 900);
            rect.localScale = Vector3.one * 0.01f;
            canvasRoot.layer = 31;

            cameraRoot = new GameObject("Temporary Font Camera", typeof(Camera));
            cameraRoot.hideFlags = HideFlags.HideAndDontSave;
            var camera = cameraRoot.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(1f, 0.97f, 0.87f);
            camera.enabled = false;
            target = new RenderTexture(1280, 900, 24);
            target.Create();
            camera.targetTexture = target;

            AddLabel(rect, original, "CURRENT: LXGW WenKai Medium", 30, 25, 590, 55, 26);
            AddLabel(rect, rounded, "ALTERNATIVE: Nowar Rounded Bold", 660, 25, 590, 55, 26);
            foreach (FarmBoxMergeLanguage language in Enum.GetValues(typeof(FarmBoxMergeLanguage)))
            {
                string Get(string key) => catalog.translations.First(entry => entry.key == key).Get(language);
                string sample = language + "\n" + string.Format(Get("level"), 25) + "   " + Get("settings") +
                    "\n" + Get("merge_title") + "\n" + Get("haptics") + "   " + Get("play");
                float y = 100 + (int)language * 195;
                AddLabel(rect, original, sample, 30, y, 590, 185, 34);
                AddLabel(rect, rounded, sample, 660, y, 590, 185, 34);
            }
            Canvas.ForceUpdateCanvases();
            foreach (var text in canvasRoot.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
            camera.Render();
            RenderTexture.active = target;
            capture = new Texture2D(1280, 900, TextureFormat.RGB24, false);
            capture.ReadPixels(new Rect(0, 0, 1280, 900), 0, 0);
            capture.Apply();
            File.WriteAllBytes("Logs/FarmBoxMergeTests/rounded-font-comparison.png", capture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            if (canvasRoot != null) UnityEngine.Object.DestroyImmediate(canvasRoot);
            if (cameraRoot != null) UnityEngine.Object.DestroyImmediate(cameraRoot);
            if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            if (capture != null) UnityEngine.Object.DestroyImmediate(capture);
        }
    }

    private static void AddLabel(RectTransform parent, TMP_FontAsset font, string value, float x, float y, float width, float height, int size)
    {
        var child = new GameObject("Font Sample", typeof(RectTransform), typeof(TextMeshProUGUI));
        child.layer = 31;
        var rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        var text = child.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.color = new Color(0.2f, 0.16f, 0.1f);
        text.text = value;
        text.raycastTarget = false;
    }
}
