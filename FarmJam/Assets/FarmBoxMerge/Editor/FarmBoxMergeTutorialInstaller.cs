using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FarmBoxMergeTutorialInstaller
{
    private const string ScenePath = "Assets/FarmBoxMerge/Scenes/FarmBoxMerge.unity";
    private const string ArtPath = "Assets/FarmBoxMerge/Dependencies/FarmJam/Art/Sprites/";

    [MenuItem("Tools/FarmBoxMerge/Tutorial/Install Canvas Guide")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)
        {
            Debug.LogWarning("Stop Play Mode and save the scene before installing the tutorial.");
            return;
        }
        Sprite hand1 = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "hand1.png");
        Sprite rounded = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FarmBoxMerge/Visuals/UI/RoundedPanel.png");
        if (hand1 == null || rounded == null)
        {
            Debug.LogError("Tutorial hand sprites or RoundedPanel sprite are missing.");
            return;
        }
        string previousScene = SceneManager.GetActiveScene().path;
        Scene scene = previousScene == ScenePath ? SceneManager.GetActiveScene() : EditorSceneManager.OpenScene(ScenePath);
        CardMergeBoard board = Object.FindFirstObjectByType<CardMergeBoard>();
        Canvas canvas = board != null ? board.GetComponentInParent<Canvas>() : null;
        if (canvas == null) { Debug.LogError("The authored gameplay Canvas is missing."); return; }
        FarmBoxMergeTutorialController controller = canvas.GetComponent<FarmBoxMergeTutorialController>();
        if (controller == null) controller = Undo.AddComponent<FarmBoxMergeTutorialController>(canvas.gameObject);
        Transform existing = canvas.transform.Find("TutorialOverlay");
        if (existing != null)
        {
            Debug.Log("Tutorial already installed. Edit its references and text on the Canvas component.");
            if (!string.IsNullOrEmpty(previousScene) && previousScene != ScenePath)
                EditorSceneManager.OpenScene(previousScene);
            return;
        }

        RectTransform root = Rect("TutorialOverlay", canvas.transform);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.sizeDelta = Vector2.zero;
        var group = Undo.AddComponent<CanvasGroup>(root.gameObject);
        group.blocksRaycasts = false;
        group.interactable = false;

        RectTransform highlight = Rect("TargetHighlight", root);
        var highlightImage = Undo.AddComponent<Image>(highlight.gameObject);
        highlightImage.sprite = rounded;
        highlightImage.type = Image.Type.Sliced;
        highlightImage.color = new Color(1f, 0.96f, 0.72f, 0.18f);
        highlightImage.raycastTarget = false;
        var outline = Undo.AddComponent<Outline>(highlight.gameObject);
        outline.effectColor = new Color(1f, 0.87f, 0.35f, 0.95f);
        outline.effectDistance = new Vector2(4f, -4f);

        RectTransform bubble = Rect("InstructionBubble", root);
        bubble.sizeDelta = new Vector2(860f, 112f);
        var bubbleImage = Undo.AddComponent<Image>(bubble.gameObject);
        bubbleImage.sprite = rounded;
        bubbleImage.type = Image.Type.Sliced;
        bubbleImage.color = new Color(1f, 0.97f, 0.87f, 0.98f);
        bubbleImage.raycastTarget = false;
        var shadow = Undo.AddComponent<Shadow>(bubble.gameObject);
        shadow.effectColor = new Color(0.2f, 0.16f, 0.09f, 0.24f);
        shadow.effectDistance = new Vector2(0f, -6f);
        RectTransform labelRect = Rect("Instruction", bubble);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = new Vector2(-24f, -12f);
        var label = Undo.AddComponent<TextMeshProUGUI>(labelRect.gameObject);
        var title = board.transform.parent.Find("CardsTitle")?.GetComponent<TextMeshProUGUI>();
        if (title != null) label.font = title.font;
        label.fontSize = 30f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.25f, 0.21f, 0.16f, 1f);
        label.raycastTarget = false;
        label.text = "MERGE MATCHING CARDS";

        RectTransform handRect = Rect("Hand", root);
        handRect.sizeDelta = new Vector2(180f, 180f);
        var hand = Undo.AddComponent<Image>(handRect.gameObject);
        hand.sprite = hand1;
        hand.preserveAspect = true;
        hand.raycastTarget = false;

        var serialized = new SerializedObject(controller);
        serialized.FindProperty("overlay").objectReferenceValue = root;
        serialized.FindProperty("overlayGroup").objectReferenceValue = group;
        serialized.FindProperty("instruction").objectReferenceValue = label;
        serialized.FindProperty("hand").objectReferenceValue = hand;
        serialized.FindProperty("targetHighlight").objectReferenceValue = highlight;
        serialized.FindProperty("tutorialHand").objectReferenceValue = hand1;
        serialized.ApplyModifiedProperties();
        root.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("FarmBoxMerge tutorial installed on the existing Canvas; no runtime Canvas is created.");
        if (!string.IsNullOrEmpty(previousScene) && previousScene != ScenePath)
            EditorSceneManager.OpenScene(previousScene);
    }

    [MenuItem("Tools/FarmBoxMerge/Tutorial/Reset Completion Flag")]
    public static void ResetCompletion()
    {
        var settings = AssetDatabase.LoadAssetAtPath<FarmBoxMergeSettings>("Assets/FarmBoxMerge/Config/FarmBoxMergeSettings.asset");
        PlayerPrefs.DeleteKey(FarmBoxMergeTutorialProgress.GetKey(settings));
        PlayerPrefs.Save();
        Debug.Log("Only tutorial completion was reset. The guide appears on level 1; saved level and settings were preserved.");
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create tutorial view");
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }
}
