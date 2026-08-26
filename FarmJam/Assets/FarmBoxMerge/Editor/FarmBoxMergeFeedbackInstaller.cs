using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FarmBoxMergeFeedbackInstaller
{
    private const string ScenePath = "Assets/FarmBoxMerge/FarmBoxMerge.unity";
    private const string FeedbackObjectName = "FarmBoxMergeGameFeel";
    private const string AudioCatalogPath = "Assets/FarmBoxMerge/Config/FarmBoxMergeAudioCatalog.asset";

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

        FarmBoxMergeAudioCatalog audioCatalog = AssetDatabase.LoadAssetAtPath<FarmBoxMergeAudioCatalog>(AudioCatalogPath);
        if (audioCatalog == null)
        {
            audioCatalog = ScriptableObject.CreateInstance<FarmBoxMergeAudioCatalog>();
            AssetDatabase.CreateAsset(audioCatalog, AudioCatalogPath);
        }

        SerializedObject serializedAudio = new SerializedObject(audioCatalog);
        const string audioRoot = "Assets/FarmBoxMerge/Dependencies/FarmJam/SFX/";
        AssignClip(serializedAudio, "<Button>k__BackingField", audioRoot + "Click/SFX_UI_Click_Generic_Cute.wav");
        AssignClip(serializedAudio, "<Merge>k__BackingField", audioRoot + "Pop/SFX_Match_Pop_1.wav");
        AssignClip(serializedAudio, "<Spawn>k__BackingField", audioRoot + "Pop/SFX_Item_Spawn_Pop_2.wav");
        AssignClip(serializedAudio, "<ItemLand>k__BackingField", audioRoot + "Collect/Boxy/SFX_Player_Collect_Boxy_1.wav");
        AssignClip(serializedAudio, "<Trash>k__BackingField", audioRoot + "Click/SFX_UI_Click_Close_Cute.wav");
        AssignClip(serializedAudio, "<BoxClear>k__BackingField", audioRoot + "Collect/Bright/SFX_Player_Collect_Bright_2.wav");
        AssignClip(serializedAudio, "<Win>k__BackingField", audioRoot + "Success/SFX_UI_Success_Magical_2.wav");
        AssignClip(serializedAudio, "<Confetti>k__BackingField", audioRoot + "Confetti/SFX_Confetti_Explosion_Bright_1.wav");
        AssignClip(serializedAudio, "<Fail>k__BackingField", audioRoot + "Fail/SFX_Fail_Cartoon_1.wav");
        AssignClip(serializedAudio, "<GameplayMusic>k__BackingField", audioRoot + "Happy Fun Casual/MUSC_Life_Is_Simple_GameplayTheme_CMajor_123BPM.wav");
        serializedAudio.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(feedback);
        EditorUtility.SetDirty(audioCatalog);
        AssetDatabase.SaveAssets();
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
