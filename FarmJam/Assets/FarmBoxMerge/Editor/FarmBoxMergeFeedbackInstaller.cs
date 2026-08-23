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
        AssignClip(serializedFeedback, "buttonClip", "Assets/FarmJam/SFX/Click/SFX_UI_Click_Generic_Cute.wav");
        AssignClip(serializedFeedback, "mergeClip", "Assets/FarmJam/SFX/Pop/SFX_Match_Pop_1.wav");
        AssignClip(serializedFeedback, "spawnClip", "Assets/FarmJam/SFX/Pop/SFX_Item_Spawn_Pop_2.wav");
        AssignClip(serializedFeedback, "itemLandClip", "Assets/FarmJam/SFX/Collect/Boxy/SFX_Player_Collect_Boxy_1.wav");
        AssignClip(serializedFeedback, "trashClip", "Assets/FarmJam/SFX/Click/SFX_UI_Click_Close_Cute.wav");
        AssignClip(serializedFeedback, "boxClearClip", "Assets/FarmJam/SFX/Collect/Bright/SFX_Player_Collect_Bright_2.wav");
        AssignClip(serializedFeedback, "winClip", "Assets/FarmJam/SFX/Success/SFX_UI_Success_Magical_2.wav");
        AssignClip(serializedFeedback, "confettiClip", "Assets/FarmJam/SFX/Confetti/SFX_Confetti_Explosion_Bright_1.wav");
        AssignClip(serializedFeedback, "failClip", "Assets/FarmJam/SFX/Fail/SFX_Fail_Cartoon_1.wav");
        AssignClip(serializedFeedback, "gameplayMusic", "Assets/FarmJam/SFX/Happy Fun Casual/MUSC_Life_Is_Simple_GameplayTheme_CMajor_123BPM.wav");
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
