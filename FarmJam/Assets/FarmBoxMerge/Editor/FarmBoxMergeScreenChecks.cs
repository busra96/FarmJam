using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

internal sealed class FarmBoxMergeScreenChecks : IDisposable
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly Vector2Int[] Resolutions =
    {
        new Vector2Int(1080, 1920), new Vector2Int(828, 1792), new Vector2Int(1080, 2520),
        new Vector2Int(1768, 2208), new Vector2Int(1536, 2048), new Vector2Int(1600, 2560)
    };
    private readonly EditorWindow _gameView;
    private readonly PropertyInfo _selectedSize;
    private readonly object _group;
    private readonly int _originalSize;
    private readonly List<object> _temporarySizes = new List<object>();
    private readonly Type _sizeType;
    private readonly Type _sizeKind;
    private readonly List<string> _results;
    private int _index = -1;
    private double _checkAt;
    private bool _disposed;
    private bool _captured;

    public FarmBoxMergeScreenChecks(List<string> results)
    {
        _results = results;
        Type sizesType = FindType("UnityEditor.GameViewSizes");
        Type singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        object sizes = singleton.GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue(null);
        _group = sizesType.GetProperty("currentGroup", Members).GetValue(sizes);
        _sizeType = FindType("UnityEditor.GameViewSize");
        _sizeKind = FindType("UnityEditor.GameViewSizeType");
        _gameView = EditorWindow.GetWindow(FindType("UnityEditor.GameView"));
        _selectedSize = _gameView.GetType().GetProperty("selectedSizeIndex", Members);
        _originalSize = (int)_selectedSize.GetValue(_gameView);
    }

    public bool Step(double now)
    {
        if (_index < 0) { SelectNext(now); return false; }
        if (now < _checkAt) return false;
        if (_captured)
        {
            if (_index == Resolutions.Length - 1) return true;
            SelectNext(now);
            _captured = false;
            return false;
        }
        Vector2Int resolution = Resolutions[_index];
        if (Screen.width != resolution.x || Screen.height != resolution.y)
            throw new Exception("Game View did not apply " + resolution + ": " + Screen.width + "x" + Screen.height);
        var board = Object.FindFirstObjectByType<CardMergeBoard>();
        var layout = Object.FindFirstObjectByType<FarmBoxMergeAdaptiveLayout>();
        layout.ApplyNow();
        Canvas.ForceUpdateCanvases();
        CheckRect(board.CardContainer.parent as RectTransform, board.EventCamera, new Rect(0, 0, Screen.width, Screen.height));
        foreach (var button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            CheckRect(button.transform as RectTransform, board.EventCamera, new Rect(0, 0, Screen.width, Screen.height));
        var items = Object.FindFirstObjectByType<MergeItemSpawner>();
        if (items.SpawnedItems.Count > 0) CheckWorldPoint(items.SpawnedItems[0].transform.position, "queue front");
        foreach (var slot in Object.FindObjectsByType<FarmBoxMergeBoxSlotView>(FindObjectsSortMode.None))
        {
            CheckWorldPoint(slot.transform.position, slot.name);
            foreach (var renderer in slot.GetComponentsInChildren<Renderer>())
            {
                Bounds bounds = renderer.bounds;
                for (int i = 0; i < 8; i++)
                    CheckWorldPoint(new Vector3((i & 1) == 0 ? bounds.min.x : bounds.max.x,
                        (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                        (i & 4) == 0 ? bounds.min.z : bounds.max.z), renderer.name);
            }
        }
        Directory.CreateDirectory("Temp/FarmBoxMergeTests");
        ScreenCapture.CaptureScreenshot("Temp/FarmBoxMergeTests/screen-" + resolution.x + "x" + resolution.y + ".png");
        _results.Add("PASS actual Game View " + resolution.x + "x" + resolution.y + ": UI bounds, first item, all ghost bounds; FOV=" + Camera.main.fieldOfView.ToString("F2"));
        if (_index == 0)
        {
            Rect safeArea = new Rect(30, 80, Screen.width - 60, Screen.height - 200);
            layout.GetType().GetMethod("ConfigureSafeArea", Members).Invoke(layout,
                new object[] { Screen.width, Screen.height, safeArea, true });
            Canvas.ForceUpdateCanvases();
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
                if (button.name == "SettingsButton") CheckRect(button.transform as RectTransform, board.EventCamera, safeArea);
            CheckRect(board.CardContainer.parent as RectTransform, board.EventCamera, safeArea);
            _results.Add("PASS simulated notch, side insets and bottom gesture area: settings + card panel");
            layout.ApplyNow();
        }
        _captured = true;
        _checkAt = now + 0.2;
        return false;
    }

    private void SelectNext(double now)
    {
        _index++;
        Vector2Int resolution = Resolutions[_index];
        object size = Activator.CreateInstance(_sizeType, Members, null,
            new object[] { Enum.Parse(_sizeKind, "FixedResolution"), resolution.x, resolution.y, "FarmBoxMerge temporary test " + _index }, null);
        _group.GetType().GetMethod("AddCustomSize", Members).Invoke(_group, new[] { size });
        _temporarySizes.Add(size);
        SelectSize(Count("GetBuiltinCount") + Count("GetCustomCount") - 1);
        _gameView.Repaint();
        _checkAt = now + 0.7;
    }

    private int Count(string method) => (int)_group.GetType().GetMethod(method, Members).Invoke(_group, null);
    private void SelectSize(int index) => _gameView.GetType().GetMethod("SizeSelectionCallback", Members)
        .Invoke(_gameView, new object[] { index, null });
    private static Type FindType(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);

    private static void CheckRect(RectTransform rect, Camera camera, Rect allowed)
    {
        if (rect == null) return;
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        foreach (var corner in corners)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corner);
            if (point.x < allowed.xMin - 2 || point.x > allowed.xMax + 2 || point.y < allowed.yMin - 2 || point.y > allowed.yMax + 2)
                throw new Exception("UI outside safe bounds: " + rect.name + " " + point + " allowed=" + allowed);
        }
    }

    private static void CheckWorldPoint(Vector3 point, string label)
    {
        Vector3 viewport = Camera.main.WorldToViewportPoint(point);
        if (viewport.z <= 0 || viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1)
            throw new Exception("World content clipped: " + label + " viewport=" + viewport);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        SelectSize(_originalSize);
        MethodInfo remove = _group.GetType().GetMethod("RemoveCustomSize", Members);
        MethodInfo get = _group.GetType().GetMethod("GetGameViewSize", Members);
        for (int i = Count("GetBuiltinCount") + Count("GetCustomCount") - 1; i >= Count("GetBuiltinCount"); i--)
            if (_temporarySizes.Contains(get.Invoke(_group, new object[] { i })))
                remove.Invoke(_group, new object[] { i });
        _gameView.Repaint();
    }

}
