using System.Collections.Generic;
using UnityEngine;

namespace FarmBlast
{
    public class CollectableBoxParent : MonoBehaviour
    {
        private class RespawnTemplateData
        {
            public int SlotId;
            public string BoxName;
            public int SiblingIndex;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;
            public CollectableBox Template;
        }

        private static readonly ColorType[] AVAILABLE_COLOR_TYPES = (ColorType[])System.Enum.GetValues(typeof(ColorType));

        public List<CollectableBox> CollectableBoxList;
        private readonly List<RespawnTemplateData> _respawnTemplates = new List<RespawnTemplateData>();
        private bool _templatesInitialized;

        public void Init()
        {
            EnsureRespawnTemplates();
            RefreshCollectableBoxes();
            AssignRandomColors();
        }

        public void RefreshCollectableBoxes()
        {
            if (CollectableBoxList == null)
            {
                CollectableBoxList = new List<CollectableBox>();
            }

            CollectableBoxList.Clear();
            CollectableBox[] collectableBoxes = GetComponentsInChildren<CollectableBox>(true);
            for (int i = 0; i < collectableBoxes.Length; i++)
            {
                if (collectableBoxes[i] != null && !collectableBoxes[i].IsTemplate)
                {
                    CollectableBoxList.Add(collectableBoxes[i]);
                }
            }
        }

        public void CleanupDestroyedCollectableBoxes()
        {
            if (CollectableBoxList == null)
            {
                CollectableBoxList = new List<CollectableBox>();
                return;
            }

            CollectableBoxList.RemoveAll(collectableBox => collectableBox == null);
        }

        public bool HasActiveCollectableBoxes()
        {
            CleanupDestroyedCollectableBoxes();

            for (int i = 0; i < CollectableBoxList.Count; i++)
            {
                CollectableBox collectableBox = CollectableBoxList[i];
                if (collectableBox != null && !collectableBox.IsDestroying)
                {
                    return true;
                }
            }

            return false;
        }

        public CollectableBox RespawnCollectableBox(int respawnSlotId)
        {
            CleanupDestroyedCollectableBoxes();
            CollectableBoxList.RemoveAll(collectableBox => collectableBox == null || collectableBox.RespawnSlotId == respawnSlotId);

            RespawnTemplateData templateData = GetTemplateData(respawnSlotId);
            if (templateData == null || templateData.Template == null)
            {
                return null;
            }

            CollectableBox respawnedCollectableBox = Instantiate(templateData.Template, transform);
            respawnedCollectableBox.name = templateData.BoxName;
            respawnedCollectableBox.transform.localPosition = templateData.LocalPosition;
            respawnedCollectableBox.transform.localRotation = templateData.LocalRotation;
            respawnedCollectableBox.transform.localScale = templateData.LocalScale;
            respawnedCollectableBox.transform.SetSiblingIndex(templateData.SiblingIndex);
            respawnedCollectableBox.PrepareRespawnedInstance(templateData.SlotId);
            respawnedCollectableBox.ColorType = GetRandomColorType();
            CollectableBoxList.Add(respawnedCollectableBox);
            return respawnedCollectableBox;
        }

        private void EnsureRespawnTemplates()
        {
            if (_templatesInitialized)
            {
                return;
            }

            RefreshCollectableBoxes();
            _respawnTemplates.Clear();

            for (int i = 0; i < CollectableBoxList.Count; i++)
            {
                CollectableBox collectableBox = CollectableBoxList[i];
                if (collectableBox == null)
                {
                    continue;
                }

                collectableBox.SetRespawnSlot(i);
                _respawnTemplates.Add(CreateTemplateData(collectableBox, i));
            }

            _templatesInitialized = true;
        }

        private RespawnTemplateData CreateTemplateData(CollectableBox collectableBox, int slotId)
        {
            CollectableBox template = Instantiate(collectableBox, transform);
            template.name = $"{collectableBox.name}_Template";
            template.transform.localPosition = collectableBox.transform.localPosition;
            template.transform.localRotation = collectableBox.transform.localRotation;
            template.transform.localScale = collectableBox.transform.localScale;
            template.SetRespawnSlot(slotId);
            template.MarkAsTemplate();

            return new RespawnTemplateData
            {
                SlotId = slotId,
                BoxName = collectableBox.name,
                SiblingIndex = collectableBox.transform.GetSiblingIndex(),
                LocalPosition = collectableBox.transform.localPosition,
                LocalRotation = collectableBox.transform.localRotation,
                LocalScale = collectableBox.transform.localScale,
                Template = template
            };
        }

        private RespawnTemplateData GetTemplateData(int slotId)
        {
            for (int i = 0; i < _respawnTemplates.Count; i++)
            {
                if (_respawnTemplates[i].SlotId == slotId)
                {
                    return _respawnTemplates[i];
                }
            }

            return null;
        }

        private void AssignRandomColors()
        {
            if (CollectableBoxList == null || CollectableBoxList.Count == 0 || AVAILABLE_COLOR_TYPES.Length == 0)
            {
                return;
            }

            for (int i = 0; i < CollectableBoxList.Count; i++)
            {
                CollectableBox collectableBox = CollectableBoxList[i];
                if (collectableBox == null)
                {
                    continue;
                }

                collectableBox.ColorType = GetRandomColorType();
            }
        }

        private static ColorType GetRandomColorType()
        {
            return AVAILABLE_COLOR_TYPES[Random.Range(0, AVAILABLE_COLOR_TYPES.Length)];
        }
    }

}
