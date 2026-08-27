using System.Collections.Generic;
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
    private ReorderableList _cardSpawnPlanList;
    private ReorderableList _boxSlotPlanList;
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
        EditorGUILayout.LabelField("LEVEL-1 KART SPAWN PLANI", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Her kart 1 değeriyle doğar. Renk toplamları burada belirlenir; oyun sırasını sabit şeffaf kutu çözüm akışından üretir. Böylece Retry aynı kart sırasını getirir ve 12 kartlık tahtada ihtiyaç duyulan merge kaynağı beş renge dağılıp kilitlenmez.",
            MessageType.None);
        _cardSpawnPlanList.DoLayoutList();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("ŞEFFAF KUTU AKIŞI", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "İlk üç satır başlangıç yuvalarına soldan sağa yerleşir. Sonraki her satır, dolup giden kutunun boşalttığı yuvaya gelir. Retry bu akışı baştan ve aynı sırada oynatır. Hedef renk yalnızca çözüm doğrulamasıdır; oyundaki şeffaf kutu beyazdır ve her rengi kabul eder.",
            MessageType.None);
        _boxSlotPlanList.DoLayoutList();

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
        EditorGUILayout.LabelField("Toplam spawnlanacak kart", _selectedLevel.TotalCardSpawnCount.ToString());
        EditorGUILayout.LabelField(
            "Başlangıçta görünecek kart",
            Mathf.Min(_selectedLevel.TotalCardSpawnCount, FarmBoxMergeRules.MaxCardsOnBoard).ToString());

        if (_selectedLevel.TotalCardSpawnCount == 0 || _selectedLevel.TotalItemCount == 0)
        {
            EditorGUILayout.HelpBox("Level başlamadan önce item akışı ve başlangıç kartlarını doldurun.", MessageType.Warning);
            return;
        }

        List<FarmBoxMergeBoxRequirement> requirements = new List<FarmBoxMergeBoxRequirement>();
        if (!FarmBoxMergeSlotPlanBuilder.TryBuildPlan(_selectedLevel, requirements, out string error))
        {
            EditorGUILayout.HelpBox($"Kutu planı geçersiz: {error}", MessageType.Error);
            return;
        }

        if (!_selectedLevel.HasAuthoredBoxSlotPlan)
        {
            EditorGUILayout.HelpBox(
                "Bu level için sabit şeffaf kutu akışı yok. Tools/FarmBoxMerge/Rebuild All Deterministic Slot Flows komutuyla oluşturabilirsiniz.",
                MessageType.Warning);
            return;
        }

        int[] countsBySize = new int[FarmBoxMergeRules.MaxCardCounter + 1];
        IReadOnlyList<FarmBoxMergeBoxSlotPlanEntry> authoredPlan = _selectedLevel.BoxSlotPlan;
        for (int i = 0; i < authoredPlan.Count; i++)
        {
            if (authoredPlan[i] != null)
            {
                countsBySize[FarmBoxMergeRules.ClampCardCounter(authoredPlan[i].boxSize)]++;
            }
        }

        EditorGUILayout.LabelField("Toplam şeffaf kutu grubu", authoredPlan.Count.ToString());
        EditorGUILayout.LabelField(
            "Kutu şekilleri",
            $"1'li: {countsBySize[1]}  ·  2'li: {countsBySize[2]}  ·  3'lü: {countsBySize[3]}  ·  4'lü: {countsBySize[4]}");
        if (FarmBoxMergeSlotPlanBuilder.TryValidateAuthoredPlan(_selectedLevel, out string authoredError))
        {
            EditorGUILayout.HelpBox(
                "Sabit akış doğrulandı: item sırası üç aktif kutu alanıyla tamamlanabiliyor, kart maliyetleri level planıyla eşleşiyor ve deterministik kart destesi bu çözüm sırasından üretilebiliyor.",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox($"Sabit akış geçersiz: {authoredError}", MessageType.Error);
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
                    ? $"{index + 1:00}.  {level.LevelName}  ·  {level.TotalItemCount} item  ·  {level.TotalCardSpawnCount} kart"
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

        SerializedProperty cardSpawnPlan = _levelObject.FindProperty("cardSpawnPlan");
        _cardSpawnPlanList = new ReorderableList(_levelObject, cardSpawnPlan, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Kart Rengi / Spawnlanacak Level-1 Kart Adedi"),
            drawElementCallback = (rect, index, active, focused) =>
            {
                SerializedProperty element = cardSpawnPlan.GetArrayElementAtIndex(index);
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

        SerializedProperty boxSlotPlan = _levelObject.FindProperty("boxSlotPlan");
        _boxSlotPlanList = new ReorderableList(_levelObject, boxSlotPlan, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(
                rect,
                "Hedef Renk / Kutu Sayısı / 4'lü Varyant (0 Kare, 1 L, 2 T, 3 Z)"),
            drawElementCallback = (rect, index, active, focused) =>
            {
                SerializedProperty element = boxSlotPlan.GetArrayElementAtIndex(index);
                rect.y += 2f;
                float colorWidth = rect.width * 0.42f;
                float sizeWidth = rect.width * 0.25f;
                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, colorWidth - 4f, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("intendedColor"),
                    GUIContent.none);
                EditorGUI.PropertyField(
                    new Rect(rect.x + colorWidth, rect.y, sizeWidth - 4f, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("boxSize"),
                    GUIContent.none);
                EditorGUI.PropertyField(
                    new Rect(
                        rect.x + colorWidth + sizeWidth,
                        rect.y,
                        rect.width - colorWidth - sizeWidth,
                        EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("fourBoxPatternVariant"),
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
        _cardSpawnPlanList = null;
        _boxSlotPlanList = null;
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
