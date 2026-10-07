using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class FarmBoxMergeSharedFontBuilder
{
    public const string FontPath = "Assets/FarmBoxMerge/Art/Fonts/Multilingual/FarmBoxMerge Multilingual SDF.asset";
    private const string SourcePath = "Assets/FarmBoxMerge/Art/Fonts/Multilingual/Editor/LXGWWenKai-Medium.ttf";

    [MenuItem("Tools/FarmBoxMerge/Localization/Rebuild and Assign Shared Font")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before rebuilding the shared localization font.");
            return;
        }
        var catalog = AssetDatabase.LoadAssetAtPath<FarmBoxMergeLocalizationCatalog>(FarmBoxMergeLocalizationInstaller.CatalogPath);
        var source = AssetDatabase.LoadAssetAtPath<Font>(SourcePath);
        if (catalog == null || source == null) throw new InvalidOperationException("Localization catalog or LXGW WenKai Medium source font is missing.");
        string characters = RequiredCharacters(catalog);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        bool created = font == null;
        if (created)
        {
            font = TMP_FontAsset.CreateFontAsset(source, 90, 8, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            if (font == null) throw new InvalidOperationException("Unable to load the LXGW WenKai font face.");
            font.name = "FarmBoxMerge Multilingual SDF";
            font.material.name = font.name + " Material";
        }
        else Undo.RecordObject(font, "Extend shared localization font");

        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        font.isMultiAtlasTexturesEnabled = true;
        // TMP's nonserialized Editor source cache is cleared by domain reloads.
        var sourceReference = new SerializedObject(font);
        sourceReference.FindProperty("m_SourceFontFile").objectReferenceValue = source;
        sourceReference.ApplyModifiedPropertiesWithoutUndo();
        if (!font.HasCharacters(characters, out uint[] absentCodepoints, false, false))
            font.TryAddCharacters(new string(absentCodepoints.Select(c => (char)c).ToArray()), out _);
        if (!font.HasCharacters(characters, out absentCodepoints, false, false))
        {
            string message = "Shared font cannot cover required characters: " + string.Join(", ", absentCodepoints.Select(c => $"U+{c:X4}"));
            if (created)
            {
                foreach (Texture2D texture in font.atlasTextures) UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(font.material);
                UnityEngine.Object.DestroyImmediate(font);
            }
            else font.atlasPopulationMode = AtlasPopulationMode.Static;
            throw new InvalidOperationException(message);
        }
        // Bake known UI glyphs once; no runtime CJK atlas generation or source-font dependency.
        font.atlasPopulationMode = AtlasPopulationMode.Static;
        var serialized = new SerializedObject(font);
        serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        if (created)
        {
            AssetDatabase.CreateAsset(font, FontPath);
            AssetDatabase.AddObjectToAsset(font.material, font);
        }
        foreach (Texture2D texture in font.atlasTextures)
        {
            if (!AssetDatabase.Contains(texture))
            {
                texture.name = font.name + " Atlas " + Array.IndexOf(font.atlasTextures, texture);
                AssetDatabase.AddObjectToAsset(texture, font);
            }
            EditorUtility.SetDirty(texture);
        }
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssetIfDirty(font);
        uint[] missingCodepoints;
        if (!font.HasCharacters(characters, out missingCodepoints, false, false))
            throw new InvalidOperationException("Saved shared font validation failed: " + string.Join(",", missingCodepoints));

        Undo.RecordObject(catalog, "Assign shared font to four languages");
        catalog.fonts = Enum.GetValues(typeof(FarmBoxMergeLanguage)).Cast<FarmBoxMergeLanguage>()
            .Select(language => new FarmBoxMergeLocalizationCatalog.LanguageFont(language) { font = font }).ToArray();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssetIfDirty(catalog);
        string report = $"PASS LXGW WenKai Medium shared by all four languages. {characters.Length} unique glyphs verified without fallback; {font.atlasTextures.Length} baked 2048x2048 atlas(es). Source font kept in Editor folder for rebuilds; runtime population is Static.";
        Directory.CreateDirectory("Logs/FarmBoxMergeTests");
        File.WriteAllText("Logs/FarmBoxMergeTests/shared-font.txt", report);
        Debug.Log("[FarmBoxMerge Localization] " + report);
    }

    public static string RequiredCharacters(FarmBoxMergeLocalizationCatalog catalog)
    {
        var characters = new SortedSet<char>();
        for (int value = 32; value <= 126; value++) characters.Add((char)value);
        foreach (char c in "ÇĞİÖŞÜçğıöşüÁÉÍÑÓÚÜáéíñóúü¿¡English Türkçe Español Chinese 中文简体汉字语言") characters.Add(c);
        foreach (var translation in catalog.translations)
            foreach (FarmBoxMergeLanguage language in Enum.GetValues(typeof(FarmBoxMergeLanguage)))
                foreach (char c in Regex.Replace(translation.Get(language) ?? "", "<[^>]+>", ""))
                    if (!char.IsControl(c)) characters.Add(c);
        return new string(characters.ToArray());
    }
}
