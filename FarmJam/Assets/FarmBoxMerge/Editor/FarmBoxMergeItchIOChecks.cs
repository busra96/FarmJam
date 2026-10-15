using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class FarmBoxMergeItchIOChecks
{
    [MenuItem("Tools/FarmBoxMerge/Itch.io/Run Release Checks")]
    public static void Run()
    {
        string project = Path.GetFullPath(Path.Combine(Application.dataPath, "..")) + Path.DirectorySeparatorChar;
        string release = Path.GetFullPath(FarmBoxMergeItchIOBuild.ReleaseRoot);
        if (release.StartsWith(project, StringComparison.OrdinalIgnoreCase))
            throw new Exception("Web release output must remain outside the Unity project.");
        int rewarded = 0, failed = 0;
        var ads = new FarmBoxMergeOfflineAdsService();
        ads.Initialize();
        if (!ads.ShowRewarded(ads.RetryPlacement, () => rewarded++) || !ads.ShowRewarded(ads.RefreshPlacement, () => rewarded++))
            throw new Exception("Offline retry/refresh unavailable.");
        ads.ShowRewarded(ads.AddCardPlacement, () => rewarded++, () => failed++);
        ads.ShowRewarded(ads.TrashPlacement, () => rewarded++, () => failed++);
        if (rewarded != 2 || failed != 2 || ads.IsShowingAd || ads.ShowInterstitialAfterLevel(15, () => rewarded++))
            throw new Exception("Offline action limits or interstitial handling failed.");
        if (new FarmBoxMergeOfflineAnalyticsService().IsCollectionEnabled) throw new Exception("Offline analytics must stay disabled.");

        int levels = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:FarmBoxMergeLevelDefinition", new[] { "Assets/FarmBoxMerge" }))
        {
            var level = AssetDatabase.LoadAssetAtPath<FarmBoxMergeLevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (!FarmBoxMergeSlotPlanBuilder.TryValidateAuthoredPlan(level, out string error)) throw new Exception(level.name + ": " + error);
            typeof(FarmBoxMergeRegressionRunner).GetMethod("ValidateDeckIndependently", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { level });
            levels++;
        }
        if (levels != 35) throw new Exception("Expected 35 authored levels.");
        foreach (string check in new[] { "ValidateSettingsPersistence", "ValidateRewardCallbacks", "ValidatePatternsAndFraming" })
            typeof(FarmBoxMergeRegressionRunner).GetMethod(check, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        Directory.CreateDirectory("Logs/FarmBoxMergeTests");
        string report = "PASS 35 authored plans and independent deck solutions; offline retry/refresh; no unlimited Add Card/Trash; no interstitials or analytics; mobile reward callbacks preserved; settings persistence; 16 patterns and six screen aspect ratios.";
        File.WriteAllText("Logs/FarmBoxMergeTests/itchio.txt", report);
        Debug.Log("[FarmJam Itch.io] " + report);
    }
}
