using UnityEngine.SceneManagement;

public static class FarmBoxMergeSceneFlow
{
    public const string MainMenuScene = "FarmBoxMergeMainMenu";
    public const string GameplayScene = "FarmBoxMerge";

    public static void LoadGameplay()
    {
        SceneManager.LoadScene(GameplayScene, LoadSceneMode.Single);
    }

    public static void LoadMainMenu()
    {
        SceneManager.LoadScene(MainMenuScene, LoadSceneMode.Single);
    }
}
