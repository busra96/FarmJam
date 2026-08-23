using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class FarmBoxMergeLevelEditorWindow : EditorWindow
{
    private const string DefaultLevelFolder = "Assets/FarmBoxMerge/Levels";

    private FarmBoxMergeLevelCatalog _catalog;
    private FarmBoxMergeLevelDefinition _selectedLevel;
    private SerializedObject _catalogObject;
    private SerializedObject _levelObject;
    private ReorderableList _catalogList;
    private ReorderableList _itemRunList;
    private ReorderableList _startingCardList;
    private Vector2 _levelScroll;

    [MenuItem("Tools/FarmBoxMerge/Level Editor")]
    public static void Open()
    {
        GetWindow<FarmBoxMergeLevelEditorWindow>("FarmBoxMerge Levels");
    }

    private void OnEnable()
    {
        minSize = new Vector2(880f, 500f);
        FindDefaultCatalog();
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (_catalog == null)
        {
            DrawEmptyCatalogState();
            return;
        }

        EnsureSerializedObjects();
        _catalogObject.Update();

        EditorGUILayout.BeginHorizontal();
        DrawCatalogPanel();
        DrawLevelPanel();
        EditorGUILayout.EndHorizontal();

        _catalogObject.ApplyModifiedProperties();
        _levelObject?.ApplyModifiedProperties();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUI.BeginChangeCheck();
        FarmBoxMergeLevelCatalog selectedCatalog = (FarmBoxMergeLevelCatalog)EditorGUILayout.ObjectField(
            _catalog,
            typeof(FarmBoxMergeLevelCatalog),
            false,
            GUILayout.MinWidth(240f));
        if (EditorGUI.EndChangeCheck())
        {
            SelectCatalog(selectedCatalog);
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Katalog Oluştur", EditorStyles.toolbarButton))
        {
            CreateCatalog();
        }

        using (new EditorGUI.DisabledScope(_catalog == null))
        {
            if (GUILayout.Button("Kaydet", EditorStyles.toolbarButton))
            {
                SaveAll();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawEmptyCatalogState()
    {
        EditorGUILayout.Space(24f);
        EditorGUILayout.HelpBox(
            "Level oluşturmak için önce bir Level Catalog seçin veya yeni katalog oluşturun.",
            MessageType.Info);

        if (GUILayout.Button("Varsayılan Level Catalog Oluştur", GUILayout.Height(36f)))
        {
            CreateCatalog();
        }
    }

    private void DrawCatalogPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(320f));
        EditorGUILayout.LabelField("LEVEL SIRASI", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Satırları sürükleyerek oynanma sırasını değiştirin.", MessageType.None);
        _catalogList.DoLayoutList();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Yeni Level"))
        {
            CreateLevel();
        }

        using (new EditorGUI.DisabledScope(_selectedLevel == null))
        {
            if (GUILayout.Button("Kopyala"))
            {
                DuplicateSelectedLevel();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        FarmBoxMergeLevelDefinition assetToAdd = (FarmBoxMergeLevelDefinition)EditorGUILayout.ObjectField(
            null,
            typeof(FarmBoxMergeLevelDefinition),
            false);
        if (assetToAdd != null)
        {
            AddLevelToCatalog(assetToAdd);
        }

        using (new EditorGUI.DisabledScope(_selectedLevel == null))
        {
            if (GUILayout.Button("Listeden Çıkar", GUILayout.Width(105f)))
            {
                RemoveSelectedFromCatalog();
            }
        }
        EditorGUILayout.EndHorizontal();

        using (new EditorGUI.DisabledScope(_selectedLevel == null))
        {
            if (GUILayout.Button("Level Asset'ini Sil"))
            {
                DeleteSelectedLevel();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawLevelPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        if (_selectedLevel == null || _levelObject == null)
        {
            EditorGUILayout.Space(20f);
            EditorGUILayout.HelpBox("Düzenlemek için soldan bir level seçin.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        _levelObject.Update();
        _levelScroll = EditorGUILayout.BeginScrollView(_levelScroll);
        EditorGUILayout.LabelField($"LEVEL DÜZENLE — {_selectedLevel.LevelName}", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_levelObject.FindProperty("levelName"), new GUIContent("Level Adı"));
        EditorGUILayout.PropertyField(_levelObject.FindProperty("designerNotes"), new GUIContent("Tasarım Notları"));

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("ITEM AKIŞI", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Renk ve adet satırları yukarıdan aşağıya açılır. Örneğin Green × 3 ardından Orange × 2 gelir.",
            MessageType.None);
        _itemRunList.DoLayoutList();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("BAŞLANGIÇ KARTLARI", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Kartlar listedeki sırayla alt tahtaya eklenir. Sahne en fazla 12 kart gösterir.",
            MessageType.None);
        _startingCardList.DoLayoutList();

        DrawLevelSummary();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawLevelSummary()
    {
        _levelObject.ApplyModifiedProperties();
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Özet", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Toplam item", _selectedLevel.TotalItemCount.ToString());
        EditorGUILayout.LabelField("Başlangıç kartı", _selectedLevel.StartingCards.Count.ToString());
        EditorGUILayout.LabelField("Kartların toplam kutu kapasitesi", _selectedLevel.StartingCardCapacity.ToString());

        if (_selectedLevel.StartingCards.Count > FarmBoxMergeRules.MaxCardsOnBoard)
        {
            EditorGUILayout.HelpBox("Başlangıç kartı sayısı 12'yi aşıyor. Fazla kartlar runtime'da oluşturulmaz.", MessageType.Error);
        }
        else if (_selectedLevel.StartingCards.Count == 0 || _selectedLevel.TotalItemCount == 0)
        {
            EditorGUILayout.HelpBox("Level başlamadan önce item akışı ve başlangıç kartlarını doldurun.", MessageType.Warning);
        }
    }

    private void EnsureSerializedObjects()
    {
        if (_catalogObject == null || _catalogObject.targetObject != _catalog)
        {
            BuildCatalogList();
        }

        if (_selectedLevel != null && (_levelObject == null || _levelObject.targetObject != _selectedLevel))
        {
            BuildLevelLists();
        }
    }

    private void BuildCatalogList()
    {
        _catalogObject = new SerializedObject(_catalog);
        SerializedProperty levels = _catalogObject.FindProperty("levels");
        _catalogList = new ReorderableList(_catalogObject, levels, true, false, false, false)
        {
            elementHeight = EditorGUIUtility.singleLineHeight + 5f,
            drawElementCallback = (rect, index, active, focused) =>
            {
                SerializedProperty element = levels.GetArrayElementAtIndex(index);
                FarmBoxMergeLevelDefinition level = element.objectReferenceValue as FarmBoxMergeLevelDefinition;
                string label = level != null
                    ? $"{index + 1:00}.  {level.LevelName}  ·  {level.TotalItemCount} item  ·  {level.StartingCards.Count} kart"
                    : $"{index + 1:00}.  Eksik Level Referansı";
                EditorGUI.LabelField(rect, label);
            },
            onSelectCallback = list => SelectLevel(levels.GetArrayElementAtIndex(list.index).objectReferenceValue as FarmBoxMergeLevelDefinition),
            onReorderCallback = _ =>
            {
                _catalogObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_catalog);
            }
        };
    }

    private void BuildLevelLists()
    {
        _levelObject = new SerializedObject(_selectedLevel);

        SerializedProperty itemRuns = _levelObject.FindProperty("itemSequence");
        _itemRunList = new ReorderableList(_levelObject, itemRuns, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Renk / Arka Arkaya Gelecek Adet"),
            drawElementCallback = (rect, index, active, focused) =>
            {
                SerializedProperty element = itemRuns.GetArrayElementAtIndex(index);
                rect.y += 2f;
                float colorWidth = rect.width * 0.62f;
                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, colorWidth - 4f, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("colorType"),
                    GUIContent.none);
                EditorGUI.PropertyField(
                    new Rect(rect.x + colorWidth, rect.y, rect.width - colorWidth, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("count"),
                    GUIContent.none);
            }
        };

        SerializedProperty cards = _levelObject.FindProperty("startingCards");
        _startingCardList = new ReorderableList(_levelObject, cards, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Kart Rengi / Kart Üzerindeki Sayı"),
            drawElementCallback = (rect, index, active, focused) =>
            {
                SerializedProperty element = cards.GetArrayElementAtIndex(index);
                rect.y += 2f;
                float colorWidth = rect.width * 0.62f;
                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, colorWidth - 4f, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("colorType"),
                    GUIContent.none);
                EditorGUI.PropertyField(
                    new Rect(rect.x + colorWidth, rect.y, rect.width - colorWidth, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("counter"),
                    GUIContent.none);
            }
        };
    }

    private void SelectCatalog(FarmBoxMergeLevelCatalog catalog)
    {
        _catalog = catalog;
        _catalogObject = null;
        SelectLevel(_catalog != null && _catalog.Count > 0 ? _catalog.GetLevel(0) : null);
    }

    private void SelectLevel(FarmBoxMergeLevelDefinition level)
    {
        _selectedLevel = level;
        _levelObject = null;
        _itemRunList = null;
        _startingCardList = null;
        Repaint();
    }

    private void FindDefaultCatalog()
    {
        string[] catalogGuids = AssetDatabase.FindAssets("t:FarmBoxMergeLevelCatalog", new[] { DefaultLevelFolder });
        if (catalogGuids.Length == 0)
        {
            catalogGuids = AssetDatabase.FindAssets("t:FarmBoxMergeLevelCatalog");
        }

        if (catalogGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(catalogGuids[0]);
            SelectCatalog(AssetDatabase.LoadAssetAtPath<FarmBoxMergeLevelCatalog>(path));
        }
    }

    private void CreateCatalog()
    {
        EnsureLevelFolder();
        string path = EditorUtility.SaveFilePanelInProject(
            "Level Catalog Oluştur",
            "FarmBoxMergeLevelCatalog",
            "asset",
            "Level sırasını tutacak katalog asset'ini kaydedin.",
            DefaultLevelFolder);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        FarmBoxMergeLevelCatalog catalog = CreateInstance<FarmBoxMergeLevelCatalog>();
        AssetDatabase.CreateAsset(catalog, path);
        AssetDatabase.SaveAssets();
        SelectCatalog(catalog);
        Selection.activeObject = catalog;
    }

    private void CreateLevel()
    {
        EnsureLevelFolder();
        string path = EditorUtility.SaveFilePanelInProject(
            "Yeni Level Oluştur",
            $"Level_{_catalog.Count + 1:000}",
            "asset",
            "Level asset'ini kaydedin.",
            DefaultLevelFolder);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        FarmBoxMergeLevelDefinition level = CreateInstance<FarmBoxMergeLevelDefinition>();
        level.Initialize(Path.GetFileNameWithoutExtension(path));
        AssetDatabase.CreateAsset(level, path);
        AddLevelToCatalog(level);
        AssetDatabase.SaveAssets();
        Selection.activeObject = level;
    }

    private void DuplicateSelectedLevel()
    {
        SaveAll();
        string sourcePath = AssetDatabase.GetAssetPath(_selectedLevel);
        string destinationPath = AssetDatabase.GenerateUniqueAssetPath(
            Path.Combine(Path.GetDirectoryName(sourcePath) ?? DefaultLevelFolder, $"{_selectedLevel.name}_Copy.asset").Replace('\\', '/'));
        if (!AssetDatabase.CopyAsset(sourcePath, destinationPath))
        {
            Debug.LogError("FarmBoxMerge level asset'i kopyalanamadı.");
            return;
        }

        AssetDatabase.ImportAsset(destinationPath);
        FarmBoxMergeLevelDefinition duplicate = AssetDatabase.LoadAssetAtPath<FarmBoxMergeLevelDefinition>(destinationPath);
        SerializedObject duplicateObject = new SerializedObject(duplicate);
        duplicateObject.FindProperty("levelId").stringValue = System.Guid.NewGuid().ToString("N");
        duplicateObject.FindProperty("levelName").stringValue = $"{_selectedLevel.LevelName} Copy";
        duplicateObject.ApplyModifiedPropertiesWithoutUndo();
        AddLevelToCatalog(duplicate);
        SaveAll();
        Selection.activeObject = duplicate;
    }

    private void AddLevelToCatalog(FarmBoxMergeLevelDefinition level)
    {
        EnsureSerializedObjects();
        SerializedProperty levels = _catalogObject.FindProperty("levels");
        for (int i = 0; i < levels.arraySize; i++)
        {
            if (levels.GetArrayElementAtIndex(i).objectReferenceValue == level)
            {
                SelectLevel(level);
                return;
            }
        }

        int index = levels.arraySize;
        levels.InsertArrayElementAtIndex(index);
        levels.GetArrayElementAtIndex(index).objectReferenceValue = level;
        _catalogObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(_catalog);
        SelectLevel(level);
        BuildCatalogList();
        _catalogList.index = index;
    }

    private void RemoveSelectedFromCatalog()
    {
        EnsureSerializedObjects();
        SerializedProperty levels = _catalogObject.FindProperty("levels");
        int selectedIndex = -1;
        for (int i = 0; i < levels.arraySize; i++)
        {
            if (levels.GetArrayElementAtIndex(i).objectReferenceValue == _selectedLevel)
            {
                selectedIndex = i;
                break;
            }
        }

        if (selectedIndex < 0)
        {
            return;
        }

        levels.DeleteArrayElementAtIndex(selectedIndex);
        _catalogObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(_catalog);
        SelectLevel(_catalog.Count > 0 ? _catalog.GetLevel(Mathf.Min(selectedIndex, _catalog.Count - 1)) : null);
        BuildCatalogList();
    }

    private void DeleteSelectedLevel()
    {
        string assetPath = AssetDatabase.GetAssetPath(_selectedLevel);
        if (!EditorUtility.DisplayDialog(
                "Level Asset'ini Sil",
                $"'{_selectedLevel.LevelName}' kalıcı olarak silinecek. Devam edilsin mi?",
                "Sil",
                "Vazgeç"))
        {
            return;
        }

        RemoveSelectedFromCatalog();
        AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.SaveAssets();
    }

    private void SaveAll()
    {
        _levelObject?.ApplyModifiedProperties();
        _catalogObject?.ApplyModifiedProperties();
        if (_selectedLevel != null)
        {
            EditorUtility.SetDirty(_selectedLevel);
        }

        if (_catalog != null)
        {
            EditorUtility.SetDirty(_catalog);
        }

        AssetDatabase.SaveAssets();
    }

    private static void EnsureLevelFolder()
    {
        if (!AssetDatabase.IsValidFolder(DefaultLevelFolder))
        {
            Directory.CreateDirectory(DefaultLevelFolder);
            AssetDatabase.Refresh();
        }
    }
}
