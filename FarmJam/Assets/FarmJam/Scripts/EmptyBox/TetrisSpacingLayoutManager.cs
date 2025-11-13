using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Signals;
using UnityEngine;

public class TetrisSpacingLayoutManager : MonoBehaviour
{
    public List<EmptyBox> objects = new List<EmptyBox>(); // Dinamik obje listesi
    public float moveDuration = 0.1f; // Yumuşak geçiş süresi (saniye cinsinden)
    
    // Çıkarılan objelerin eski index'lerini saklayan dictionary
    private Dictionary<EmptyBox, int> removedIndexes = new Dictionary<EmptyBox, int>();

    /// <summary>
    /// Event Listener'ları aktifleştirir.
    /// </summary>
    private void OnEnable()
    {
        EmptyBoxSignals.OnAddedEmptyBox.AddListener(OnSpawnedEmptyBox);
      //  EmptyBoxSignals.OnTheEmptyBoxRemoved.AddListener(OnRemovedEmptyBox);
        EmptyBoxSignals.OnRemovedEmptyBox.AddListener(OnRemovedEmptyBox);
        EmptyBoxSignals.OnUpdateTetrisLayout.AddListener(OnUpdateTetrisLayout);
    }

    /// <summary>
    /// Event Listener'ları devre dışı bırakır.
    /// </summary>
    private void OnDisable()
    {
        EmptyBoxSignals.OnAddedEmptyBox.RemoveListener(OnSpawnedEmptyBox);
      //  EmptyBoxSignals.OnTheEmptyBoxRemoved.RemoveListener(OnRemovedEmptyBox);
        EmptyBoxSignals.OnRemovedEmptyBox.RemoveListener(OnRemovedEmptyBox);
        EmptyBoxSignals.OnUpdateTetrisLayout.RemoveListener(OnUpdateTetrisLayout);
    }

    /// <summary>
    /// Objelerin layout'unu günceller.
    /// </summary>
    private void OnUpdateTetrisLayout()
    {
        UpdateTetrisLayout();
    }

    /// <summary>
    /// Yeni bir EmptyBox eklendiğinde çağrılır.
    /// Eğer daha önce çıkarılmışsa, önceki index'ine eklenir.
    /// </summary>
    private void OnSpawnedEmptyBox(EmptyBox obj)
    {
        if (objects.Contains(obj)) return;

        if (removedIndexes.TryGetValue(obj, out int previousIndex))
        {
            // Önceden çıkarılmışsa, eski index'e geri ekle
            objects.Insert(Mathf.Min(previousIndex, objects.Count), obj);
            removedIndexes.Remove(obj); // Kaydedilen index'i temizle
        }
        else
        {
            // Yeni obje olarak listeye ekle
            objects.Add(obj);
        }

        UpdateTetrisLayout();
    }

    /// <summary>
    /// Layout'u güncelleyip objeleri smooth şekilde hizalar.
    /// </summary>
    public async UniTask UpdateTetrisLayout()
    {
        await UniTask.DelayFrame(5);
        UpdateLayout();
        await UniTask.DelayFrame(5);
        await PositionObjectsSmooth(); // Smooth geçişi başlat
    }

    /// <summary>
    /// Bir EmptyBox kaldırıldığında çağrılır. Eski index'i saklanır.
    /// </summary>
    private void OnRemovedEmptyBox(EmptyBox obj)
    {
        if (!objects.Contains(obj)) return;

        // Çıkarılmadan önce index'ini kaydet
        int removedIndex = objects.IndexOf(obj);
        removedIndexes[obj] = removedIndex;

        // Objeyi listeden kaldır
        objects.Remove(obj);
        UpdateTetrisLayout();
    }

    /// <summary>
    /// Manuel olarak A veya S tuşuna basıldığında işlemleri tetikler.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            UpdateLayout();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            _ = PositionObjectsSmooth(); // Smooth hizalamayı çağır
        }
    }

    /// <summary>
    /// Objelerin pivot noktalarını tekrar hesaplar.
    /// </summary>
    public void UpdateLayout()
    {
        if (objects == null || objects.Count == 0)
        {
            Debug.LogWarning("Liste boş, işlem yapılmadı.");
            return;
        }

        foreach (var obj in objects)
        {
            AdjustPivotToBottomCenter(obj);
        }
    }

    /// <summary>
    /// EmptyBox'ın child Collider'ına göre pivot noktasını ayarlar.
    /// </summary>
    void AdjustPivotToBottomCenter(EmptyBox emptyBox)
    {
        if (emptyBox == null || emptyBox.Collider == null)
        {
            Debug.LogError($"Objede Collider bulunamadı veya obje null: {emptyBox?.gameObject.name}");
            return;
        }

        Collider col = emptyBox.Collider;
        Bounds bounds = col.bounds;
        Vector3 newPivotPosition = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

        Transform parentTransform = emptyBox.transform;
        if (parentTransform == null)
        {
            Debug.LogWarning($"Objenin parent'ı yok: {emptyBox.gameObject.name}, işlem yapılamadı.");
            return;
        }

        Vector3 offset = parentTransform.position - newPivotPosition;
        parentTransform.position -= offset;

        foreach (Transform child in parentTransform)
        {
            child.position += offset;
        }
    }

    /// <summary>
    /// Objeleri ekranda yumuşak bir şekilde hareket ettirerek hizalar.
    /// </summary>
    public async UniTask PositionObjectsSmooth()
    {
        if (objects == null || objects.Count == 0)
        {
            Debug.LogWarning("Liste boş, hizalama yapılmadı.");
            return;
        }

        float screenWorldWidth = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, 0, 10)).x * 2;
        float[] widths = new float[objects.Count];
        float totalWidth = 0;

        // Objelerin genişliklerini hesapla
        for (int i = 0; i < objects.Count; i++)
        {
            Collider col = objects[i].Collider;
            if (col != null)
            {
                widths[i] = col.bounds.size.x;
                totalWidth += widths[i];
            }
        }

        // Objeler arasındaki boşluğu hesapla
        float totalSpacing = screenWorldWidth - totalWidth;
        float spacing = totalSpacing / (objects.Count + 1);
        float currentX = -screenWorldWidth / 2 + spacing;

        List<UniTask> moveTasks = new List<UniTask>();

        for (int i = 0; i < objects.Count; i++)
        {
            float halfWidth = widths[i] / 2;
            Vector3 targetPosition = new Vector3(currentX + halfWidth, 0, -10);

            moveTasks.Add(objects[i].transform
                .DOMove(targetPosition, moveDuration)
                .SetEase(Ease.InOutQuad)
                .SetUpdate(UpdateType.Normal, true)
                .AsyncWaitForCompletion().AsUniTask()); // HATAYI DÜZELTEN KOD


            currentX += widths[i] + spacing;
        }

        await UniTask.WhenAll(moveTasks);
    }

    /// <summary>
    /// Inspector'de obje listesi değiştiğinde otomatik olarak güncellenmesini sağlar.
    /// </summary>
    private void OnValidate()
    {
        UpdateLayout();
    }

}