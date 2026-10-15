using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class FarmBoxMergeItchIOArtwork
{
    [MenuItem("Tools/FarmBoxMerge/Itch.io/Export Cover and Banner")]
    public static void Run()
    {
        string folder = Path.Combine(FarmBoxMergeItchIOBuild.ReleaseRoot, "Artwork");
        Directory.CreateDirectory(folder);
        Render(folder, 630, 500, "cover-630x500.png");
        Render(folder, 1260, 300, "banner-1260x300.png");
        Debug.Log("[FarmJam Itch.io] Exported cover and banner using existing game artwork.");
    }

    private static void Render(string folder, int width, int height, string filename)
    {
        GameObject root = null, cameraObject = null;
        Texture2D capture = null;
        RenderTexture target = null, previous = RenderTexture.active;
        try
        {
            root = new GameObject("Temporary Farm Jam Artwork", typeof(RectTransform), typeof(Canvas));
            root.hideFlags = HideFlags.HideAndDontSave;
            root.layer = 31;
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one * 0.01f;
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            cameraObject = new GameObject("Temporary Artwork Camera", typeof(Camera));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            var camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = height * 0.005f;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.14f, 0.23f, 0.15f);
            target = new RenderTexture(width, height, 24);
            target.Create();
            camera.targetTexture = target;

            var background = Add(rect, "Farm Background", 0, 0, width, height).gameObject.AddComponent<RawImage>();
            background.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/FarmBoxMerge/Visuals/Backgrounds/FarmBackMainMenu.png");
            float sourceAspect = background.texture.width / (float)background.texture.height;
            float targetAspect = width / (float)height;
            float cropWidth = Mathf.Min(1f, targetAspect / sourceAspect);
            float cropHeight = Mathf.Min(1f, sourceAspect / targetAspect);
            background.uvRect = new Rect((1f - cropWidth) * 0.5f, (1f - cropHeight) * 0.5f, cropWidth, cropHeight);
            var tint = Add(rect, "Text Contrast", 0, 0, width, height).gameObject.AddComponent<Image>();
            tint.color = new Color(0.1f, 0.22f, 0.1f, 0.42f);
            bool banner = width > 900;
            float iconSize = banner ? 260 : 360;
            var icon = Add(rect, "Harvest Icon", width - iconSize - 22, height - iconSize - 20, iconSize, iconSize).gameObject.AddComponent<RawImage>();
            icon.texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/FarmBoxMerge/Visuals/UI/GameIcon.png");
            float textWidth = width - iconSize - 66;
            Label(rect, banner ? "FARM JAM" : "FARM\nJAM", banner ? 36 : 26, banner ? 38 : 50,
                textWidth, banner ? 120 : 250, banner ? 94 : 72, Color.white);
            Label(rect, banner ? "MERGE. PACK. HARVEST." : "MERGE &\nHARVEST", banner ? 40 : 28,
                banner ? 171 : 305, textWidth, banner ? 48 : 88, banner ? 32 : 26, new Color(1f, 0.83f, 0.35f));
            Label(rect, "35 PUZZLE LEVELS", banner ? 40 : 28, height - 54, textWidth, 36, banner ? 23 : 21, Color.white);
            Canvas.ForceUpdateCanvases();
            foreach (var text in root.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
            camera.Render();
            RenderTexture.active = target;
            capture = new Texture2D(width, height, TextureFormat.RGB24, false);
            capture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            capture.Apply();
            File.WriteAllBytes(Path.Combine(folder, filename), capture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
            if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            if (capture != null) UnityEngine.Object.DestroyImmediate(capture);
        }
    }

    private static RectTransform Add(RectTransform parent, string name, float x, float y, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 31;
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    private static void Label(RectTransform parent, string value, float x, float y, float width, float height, int size, Color color)
    {
        var text = Add(parent, "Title", x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FarmBoxMergeSharedFontBuilder.RoundedFontPath);
        text.fontSize = size;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = color;
        text.text = value;
    }
}
