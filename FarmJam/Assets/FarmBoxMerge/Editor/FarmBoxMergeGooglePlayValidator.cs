using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class FarmBoxMergeGooglePlayValidator
{
    private const string AdsSettingsPath = "Assets/FarmBoxMerge/Config/FarmBoxMergeAdsSettings.asset";
    private const string MainMenuScenePath = "Assets/FarmBoxMerge/Scenes/FarmBoxMergeMainMenu.unity";
    private const string GameplayScenePath = "Assets/FarmBoxMerge/Scenes/FarmBoxMerge.unity";
    private const string ResolverStatePath = "ProjectSettings/AndroidResolverDependencies.xml";

    [MenuItem("Tools/FarmBoxMerge/Validate Google Play Readiness")]
    public static void ValidateFromMenu()
    {
        ValidationSummary result = Validate();
        string message = $"Google Play readiness: {result.Errors} error(s), {result.Warnings} warning(s).";

        if (result.Errors > 0)
        {
            Debug.LogError(message);
            EditorUtility.DisplayDialog("FarmBoxMerge Google Play", message + "\nSee Console for details.", "OK");
            return;
        }

        if (result.Warnings > 0)
        {
            Debug.LogWarning(message);
            EditorUtility.DisplayDialog("FarmBoxMerge Google Play", message + "\nSee Console for details.", "OK");
            return;
        }

        Debug.Log(message);
        EditorUtility.DisplayDialog("FarmBoxMerge Google Play", "All automated checks passed.", "OK");
    }

    [MenuItem("Tools/FarmBoxMerge/Resolve Android Ad Dependencies")]
    public static void ResolveAndroidAdDependencies()
    {
        Type resolverType = Type.GetType("GooglePlayServices.PlayServicesResolver, Google.JarResolver");
        MethodInfo resolveMethod = resolverType?.GetMethod(
            "ResolveSync",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(bool) },
            null);

        if (resolveMethod == null)
        {
            Debug.LogError("Android dependency resolver is unavailable. Reimport Mobile Dependency Resolver.");
            return;
        }

        bool succeeded = (bool)resolveMethod.Invoke(null, new object[] { true });
        if (succeeded)
        {
            Debug.Log("Android ad dependencies resolved successfully.");
        }
        else
        {
            Debug.LogError("Android ad dependency resolution failed. Check the resolver output in Console.");
        }
    }

    public static ValidationSummary Validate()
    {
        ValidationSummary result = new ValidationSummary();
        NamedBuildTarget android = NamedBuildTarget.Android;

        Check(
            PlayerSettings.Android.targetSdkVersion == AndroidSdkVersions.AndroidApiLevel36,
            "Target API is Android 16 (API 36).",
            "Target API must be Android 16 (API 36) for current Google Play submissions.",
            result);
        Check(
            PlayerSettings.GetScriptingBackend(android) == ScriptingImplementation.IL2CPP,
            "Android scripting backend is IL2CPP.",
            "Android scripting backend must be IL2CPP for this release configuration.",
            result);
        Check(
            (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0,
            "ARM64 is enabled.",
            "ARM64 architecture is required for Google Play.",
            result);
        Check(
            PlayerSettings.Android.optimizedFramePacing,
            "Optimized frame pacing is enabled.",
            "Enable Optimized Frame Pacing for smoother Android rendering.",
            result);
        Check(
            PlayerSettings.Android.predictiveBackSupport,
            "Predictive back is enabled.",
            "Enable Predictive Back Support for Android 13 and newer.",
            result);

        string applicationId = PlayerSettings.GetApplicationIdentifier(android);
        Check(
            Regex.IsMatch(applicationId ?? string.Empty, "^[a-zA-Z][a-zA-Z0-9_]*(\\.[a-zA-Z][a-zA-Z0-9_]*)+$"),
            $"Application ID is {applicationId}.",
            "Android Application ID is empty or invalid.",
            result);
        Check(
            PlayerSettings.Android.bundleVersionCode > 0,
            $"Version code is {PlayerSettings.Android.bundleVersionCode}.",
            "Android version code must be greater than zero.",
            result);
        Check(
            !string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion),
            $"Version name is {PlayerSettings.bundleVersion}.",
            "Version name is empty.",
            result);

        ValidateScenes(result);
        ValidateAds(result);
        ValidateAndroidResolver(result);
        ValidateIcon(result);

        Warn(
            PlayerSettings.Android.useCustomKeystore,
            "A custom Android keystore is configured.",
            "Custom release keystore is not configured; a Play upload build cannot be signed yet.",
            result);
        Warn(
            EditorUserBuildSettings.buildAppBundle,
            "Build App Bundle is enabled.",
            "Build App Bundle is currently disabled; enable it for the Google Play upload build.",
            result);

        Debug.Log($"Google Play automated validation finished: {result.Errors} error(s), {result.Warnings} warning(s).");
        return result;
    }

    private static void ValidateScenes(ValidationSummary result)
    {
        string[] enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        Check(
            enabledScenes.Length >= 2
                && enabledScenes[0] == MainMenuScenePath
                && enabledScenes.Contains(GameplayScenePath),
            "Build scenes start with Main Menu and include Gameplay.",
            "Build Settings must start with FarmBoxMergeMainMenu and include FarmBoxMerge gameplay.",
            result);
    }

    private static void ValidateAds(ValidationSummary result)
    {
        FarmBoxMergeAdsSettings adsSettings =
            AssetDatabase.LoadAssetAtPath<FarmBoxMergeAdsSettings>(AdsSettingsPath);

        Check(
            adsSettings != null,
            "Ads settings asset exists.",
            $"Ads settings asset is missing at {AdsSettingsPath}.",
            result);

        if (adsSettings == null || !adsSettings.AdsEnabled)
        {
            return;
        }

        Check(
            adsSettings.HasAndroidConfiguration,
            "Android LevelPlay credentials are configured.",
            "Android LevelPlay App Key, rewarded Ad Unit ID, or interstitial Ad Unit ID is missing.",
            result);
    }

    private static void ValidateAndroidResolver(ValidationSummary result)
    {
        bool resolverInstalled =
            Type.GetType("GooglePlayServices.PlayServicesResolver, Google.JarResolver") != null;
        Check(
            resolverInstalled,
            "Mobile Dependency Resolver is installed.",
            "Mobile Dependency Resolver is missing, so Android ad libraries cannot be included.",
            result);
        Warn(
            File.Exists(ResolverStatePath),
            "Android ad dependencies have a resolver state file.",
            "Android dependencies have not been resolved yet. Run Tools > FarmBoxMerge > Resolve Android Ad Dependencies.",
            result);
    }

    private static void ValidateIcon(ValidationSummary result)
    {
        Texture2D[] icons = PlayerSettings.GetIcons(NamedBuildTarget.Android, IconKind.Application);
        Check(
            icons != null && icons.Any(icon => icon != null),
            "An Android application icon is assigned.",
            "Android application icon is missing.",
            result);
    }

    private static void Check(
        bool condition,
        string successMessage,
        string failureMessage,
        ValidationSummary result)
    {
        if (condition)
        {
            Debug.Log($"[Google Play] PASS: {successMessage}");
            return;
        }

        result.Errors++;
        Debug.LogError($"[Google Play] ERROR: {failureMessage}");
    }

    private static void Warn(
        bool condition,
        string successMessage,
        string warningMessage,
        ValidationSummary result)
    {
        if (condition)
        {
            Debug.Log($"[Google Play] PASS: {successMessage}");
            return;
        }

        result.Warnings++;
        Debug.LogWarning($"[Google Play] WARNING: {warningMessage}");
    }

    public sealed class ValidationSummary
    {
        public int Errors { get; set; }
        public int Warnings { get; set; }
    }
}
