using UnityEngine;

public interface IFarmBoxMergeTutorialProgress
{
    bool Completed { get; }
    void Complete();
}

public sealed class FarmBoxMergeTutorialProgress : IFarmBoxMergeTutorialProgress
{
    private readonly string _key;

    public FarmBoxMergeTutorialProgress(FarmBoxMergeSettings settings)
    {
        _key = GetKey(settings);
    }

    public bool Completed => PlayerPrefs.GetInt(_key, 0) == 1;

    public void Complete()
    {
        PlayerPrefs.SetInt(_key, 1);
        PlayerPrefs.Save();
    }

    public static string GetKey(FarmBoxMergeSettings settings)
    {
        string prefix = settings != null && !string.IsNullOrWhiteSpace(settings.PlayerPrefsPrefix)
            ? settings.PlayerPrefsPrefix.Trim() : "FarmBoxMerge.Settings";
        return prefix + ".TutorialCompleted";
    }
}
