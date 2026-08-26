using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FarmBoxMergeFeedbackInstaller
{
    private const string ScenePath = "Assets/FarmBoxMerge/FarmBoxMerge.unity";
    private const string FeedbackObjectName = "FarmBoxMergeGameFeel";

    [MenuItem("Tools/FarmBoxMerge/Apply Game Feel Polish")]
    public static void ApplyGameFeelPolish()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        FarmBoxMergeFeedbackController feedback = Object.FindFirstObjectByType<FarmBoxMergeFeedbackController>(FindObjectsInactive.Include);
        if (feedback == null)
        {
            GameObject feedbackObject = GameObject.Find(FeedbackObjectName);
            if (feedbackObject == null)
            {
                feedbackObject = new GameObject(FeedbackObjectName);
                Undo.RegisterCreatedObjectUndo(feedbackObject, "Create FarmBoxMerge game feel");
            }

            feedback = Undo.AddComponent<FarmBoxMergeFeedbackController>(feedbackObject);
        }

        SerializedObject serializedFeedback = new SerializedObject(feedback);
        const string audioRoot = "Assets/FarmBoxMerge/Dependencies/FarmJam/SFX/";
        AssignClip(serializedFeedback, "buttonClip", audioRoot + "Click/SFX_UI_Click_Generic_Cute.wav");
        AssignClip(serializedFeedback, "mergeClip", audioRoot + "Pop/SFX_Match_Pop_1.wav");
        AssignClip(serializedFeedback, "spawnClip", audioRoot + "Pop/SFX_Item_Spawn_Pop_2.wav");
        AssignClip(serializedFeedback, "itemLandClip", audioRoot + "Collect/Boxy/SFX_Player_Collect_Boxy_1.wav");
        AssignClip(serializedFeedback, "trashClip", audioRoot + "Click/SFX_UI_Click_Close_Cute.wav");
        AssignClip(serializedFeedback, "boxClearClip", audioRoot + "Collect/Bright/SFX_Player_Collect_Bright_2.wav");
        AssignClip(serializedFeedback, "winClip", audioRoot + "Success/SFX_UI_Success_Magical_2.wav");
        AssignClip(serializedFeedback, "confettiClip", audioRoot + "Confetti/SFX_Confetti_Explosion_Bright_1.wav");
        AssignClip(serializedFeedback, "failClip", audioRoot + "Fail/SFX_Fail_Cartoon_1.wav");
        AssignClip(serializedFeedback, "gameplayMusic", audioRoot + "Happy Fun Casual/MUSC_Life_Is_Simple_GameplayTheme_CMajor_123BPM.wav");
        serializedFeedback.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(feedback);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("FarmBoxMerge game feel polish applied.", feedback);
    }

    private static void AssignClip(SerializedObject target, string propertyName, string assetPath)
    {
        SerializedProperty property = target.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        }
    }
}
