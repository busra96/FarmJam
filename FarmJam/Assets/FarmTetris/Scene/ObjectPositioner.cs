namespace FarmTetris
{
    using System;
    using UnityEngine;

    public class ObjectPositioner : MonoBehaviour
    {
        public GameObject[] objects; // 3 objeyi buraya atayÄ±n

        
        
        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space))
                PositionObjects();
        }

       
       void Start()
        {
            if (objects.Length != 3)
            {
                Debug.LogError("LÃ¼tfen 3 obje ekleyin.");
                return;
            }

            // 1. Ã–nce Pivot NoktalarÄ±nÄ± DÃ¼zenle
            for (int i = 0; i < objects.Length; i++)
            {
                AdjustPivotToBottomCenter(objects[i]);
            }

            // 2. Objeleri Ekranda Ortala
            PositionObjects();
        }

        void AdjustPivotToBottomCenter(GameObject obj)
        {
            Collider col = obj.GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogError($"Objede Collider bulunamadÄ±: {obj.name}");
                return;
            }

            Bounds bounds = col.bounds;
            Vector3 newPivot = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

            // Yeni pivotu oluÅŸturmak iÃ§in bir boÅŸ GameObject oluÅŸturuyoruz
            GameObject pivotObject = new GameObject(obj.name + "_Pivot");
            pivotObject.transform.position = newPivot;

            // Orijinal objeyi pivot'un child'Ä± yapÄ±yoruz
            obj.transform.SetParent(pivotObject.transform, true);
            
            // Listeyi gÃ¼ncelle
            objects[System.Array.IndexOf(objects, obj)] = pivotObject;
        }

        public void PositionObjects()
        {
            // Ekran geniÅŸliÄŸini world space'e Ã§evir
            float screenWorldWidth = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, 0, 10)).x * 2;

            // Objelerin geniÅŸliklerini hesapla
            float[] widths = new float[objects.Length];
            float totalWidth = 0;

            for (int i = 0; i < objects.Length; i++)
            {
                Collider col = objects[i].GetComponentInChildren<Collider>();
                if (col != null)
                {
                    widths[i] = col.bounds.size.x;
                    totalWidth += widths[i];
                }
            }

            // Objeler arasÄ±ndaki boÅŸluÄŸu hesapla
            float totalSpacing = screenWorldWidth - totalWidth;
            float spacing = totalSpacing / (objects.Length + 1);

            // Objeleri konumlandÄ±r
            float currentX = -screenWorldWidth / 2 + spacing;

            for (int i = 0; i < objects.Length; i++)
            {
                float halfWidth = widths[i] / 2;
                objects[i].transform.position = new Vector3(currentX + halfWidth, 0, -10);
                currentX += widths[i] + spacing;
            }
        }

    }
}

