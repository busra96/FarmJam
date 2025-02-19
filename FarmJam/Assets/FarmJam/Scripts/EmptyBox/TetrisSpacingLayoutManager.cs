using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Signals;
using UnityEngine;

public class TetrisSpacingLayoutManager : MonoBehaviour
{
   public List<EmptyBox> objects = new List<EmptyBox>(); // Dinamik obje listesi
    public float moveDuration = 0.5f; // Yumuşak geçiş süresi

    private void OnEnable()
    {
        EmptyBoxSignals.OnAddedEmptyBox.AddListener(OnSpawnedEmptyBox); 
        EmptyBoxSignals.OnTheEmptyBoxRemoved.AddListener(OnRemovedEmptyBox); 
        EmptyBoxSignals.OnRemovedEmptyBox.AddListener(OnRemovedEmptyBox);
        EmptyBoxSignals.OnUpdateTetrisLayout.AddListener(OnUpdateTetrisLayout);
    }

    private void OnDisable()
    {
        EmptyBoxSignals.OnAddedEmptyBox.RemoveListener(OnSpawnedEmptyBox); 
        EmptyBoxSignals.OnTheEmptyBoxRemoved.RemoveListener(OnRemovedEmptyBox); 
        EmptyBoxSignals.OnRemovedEmptyBox.RemoveListener(OnRemovedEmptyBox);
        EmptyBoxSignals.OnUpdateTetrisLayout.RemoveListener(OnUpdateTetrisLayout);
    }

    private void OnUpdateTetrisLayout()
    {
        UpdateTetrisLayout();
    }

    private void OnSpawnedEmptyBox(EmptyBox obj)
    {
        if(objects.Contains(obj))
            return;
       
        objects.Add(obj);
        UpdateTetrisLayout();
    }

    public async UniTask UpdateTetrisLayout()
    {
        await UniTask.DelayFrame(20);
        UpdateLayout();
        await UniTask.DelayFrame(20);
        await PositionObjectsSmooth(); // Smooth geçişi çağır
    }

    private void OnRemovedEmptyBox(EmptyBox obj)
    {
        if(!objects.Contains(obj))
            return;
       
        objects.Remove(obj);
        UpdateTetrisLayout();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            UpdateLayout();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            _ = PositionObjectsSmooth(); // Smooth versiyonu çağır
        }
    }

    /// <summary>
    /// Objeleri güncelleyerek pivot noktalarını tekrar hesaplar ve konumlarını belirler.
    /// </summary>
    public void UpdateLayout()
    {
        if (objects == null || objects.Count == 0)
        {
            Debug.LogWarning("Liste boş, işlem yapılmadı.");
            return;
        }

        // 1. Önce Pivot Noktalarını Düzenle (Child Collider'a göre EmptyBox'ı hizala)
        foreach (var obj in objects)
        {
            AdjustPivotToBottomCenter(obj);
        }
    }

    /// <summary>
    /// EmptyBox'ın kendi child Collider'ına göre pivot noktasını ayarlar.
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

        // Mevcut EmptyBox'ı, Child'ın collider'ına göre hizala
        Vector3 offset = parentTransform.position - newPivotPosition;
        parentTransform.position -= offset;

        // Çocukları orijinal pozisyonlarını koruyarak hizala
        foreach (Transform child in parentTransform)
        {
            child.position += offset;
        }
    }

    /// <summary>
    /// Ekran genişliği ve obje boyutlarına göre objeleri hizalar (SERT geçiş).
    /// </summary>
    public void PositionObjects()
    {
        if (objects == null || objects.Count == 0)
        {
            Debug.LogWarning("Liste boş, hizalama yapılmadı.");
            return;
        }

        float screenWorldWidth = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, 0, 10)).x * 2;
        float[] widths = new float[objects.Count];
        float totalWidth = 0;

        for (int i = 0; i < objects.Count; i++)
        {
            Collider col = objects[i].Collider;
            if (col != null)
            {
                widths[i] = col.bounds.size.x;
                totalWidth += widths[i];
            }
        }

        float totalSpacing = screenWorldWidth - totalWidth;
        float spacing = totalSpacing / (objects.Count + 1);
        float currentX = -screenWorldWidth / 2 + spacing;

        for (int i = 0; i < objects.Count; i++)
        {
            float halfWidth = widths[i] / 2;
            objects[i].transform.position = new Vector3(currentX + halfWidth, 0, -10);
            currentX += widths[i] + spacing;
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

        for (int i = 0; i < objects.Count; i++)
        {
            Collider col = objects[i].Collider;
            if (col != null)
            {
                widths[i] = col.bounds.size.x;
                totalWidth += widths[i];
            }
        }

        float totalSpacing = screenWorldWidth - totalWidth;
        float spacing = totalSpacing / (objects.Count + 1);
        float currentX = -screenWorldWidth / 2 + spacing;

        List<UniTask> moveTasks = new List<UniTask>(); // Paralel hareket işlemleri için liste

        for (int i = 0; i < objects.Count; i++)
        {
            float halfWidth = widths[i] / 2;
            Vector3 targetPosition = new Vector3(currentX + halfWidth, 0, -10);
            
            // UniTask yerine klasik DoTween kullanımı
            moveTasks.Add(UniTask.Create(async () => 
                await objects[i].transform.DOMove(targetPosition, moveDuration).SetEase(Ease.OutQuad).AsyncWaitForCompletion()));

            currentX += widths[i] + spacing;
        }

        await UniTask.WhenAll(moveTasks); // Tüm hareketlerin tamamlanmasını bekle
    }

    /// <summary>
    /// Editor'de obje listesi değiştiğinde otomatik olarak güncellenmesini sağlar.
    /// </summary>
    private void OnValidate()
    {
        UpdateLayout();
    }

}