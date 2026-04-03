using System.Collections.Generic;
using UnityEngine;

namespace FarmBlast
{
    public class EmptyBoxSpawnArea : MonoBehaviour
    {
        [SerializeField] private FB_EmptyUnitBoxParent _emptyUnitBoxParentPrefab;
        [SerializeField] private Transform _spawnParent;
        [SerializeField] private bool _spawnOnStart = true;
        [SerializeField] private bool _replaceExistingSpawn = true;
        [SerializeField] private float _unitSpacing = 2.1f;
        [SerializeField] private FB_EmptyUnitBoxLayoutType _defaultLayoutType = FB_EmptyUnitBoxLayoutType.Single;
        [SerializeField] private List<FB_EmptyUnitBoxLayoutType> _spawnableLayouts = new List<FB_EmptyUnitBoxLayoutType>();
        [SerializeField] private List<FB_EmptyUnitBoxParent> _spawnedParents = new List<FB_EmptyUnitBoxParent>();

        public IReadOnlyList<FB_EmptyUnitBoxParent> SpawnedParents => _spawnedParents;

        private void Awake()
        {
            if (_spawnParent == null)
            {
                _spawnParent = transform;
            }
        }

        private void Start()
        {
            if (_spawnOnStart)
            {
                Spawn();
            }
        }

        [ContextMenu("Spawn Random Parent")]
        public FB_EmptyUnitBoxParent Spawn()
        {
            return Spawn(GetRandomLayoutType());
        }

        [ContextMenu("Spawn Default Parent")]
        public FB_EmptyUnitBoxParent SpawnDefault()
        {
            return Spawn(_defaultLayoutType);
        }

        public FB_EmptyUnitBoxParent Spawn(FB_EmptyUnitBoxLayoutType layoutType)
        {
            CleanupDestroyedParents();

            if (_emptyUnitBoxParentPrefab == null)
            {
                Debug.LogError("[EmptyBoxSpawnArea] FB_EmptyUnitBoxParent prefab referansi eksik.", this);
                return null;
            }

            if (_replaceExistingSpawn)
            {
                ClearSpawnedParents();
            }

            FB_EmptyUnitBoxParent spawnedParent =
                Instantiate(_emptyUnitBoxParentPrefab, transform.position, transform.rotation, _spawnParent);

            spawnedParent.transform.position = transform.position;
            spawnedParent.transform.rotation = transform.rotation;
            spawnedParent.name = $"{_emptyUnitBoxParentPrefab.name}_{layoutType}";
            spawnedParent.Initialize(layoutType, _unitSpacing);

            _spawnedParents.Add(spawnedParent);
            return spawnedParent;
        }

        public void RemoveSpawnedParent(FB_EmptyUnitBoxParent emptyUnitBoxParent)
        {
            if (emptyUnitBoxParent == null)
            {
                return;
            }

            _spawnedParents.Remove(emptyUnitBoxParent);
        }

        [ContextMenu("Clear Spawned Parents")]
        public void ClearSpawnedParents()
        {
            CleanupDestroyedParents();

            for (int i = _spawnedParents.Count - 1; i >= 0; i--)
            {
                FB_EmptyUnitBoxParent spawnedParent = _spawnedParents[i];

                if (spawnedParent == null)
                {
                    continue;
                }

                DestroySpawnedParent(spawnedParent.gameObject);
            }

            _spawnedParents.Clear();
        }

        private FB_EmptyUnitBoxLayoutType GetRandomLayoutType()
        {
            if (_spawnableLayouts.Count > 0)
            {
                return _spawnableLayouts[Random.Range(0, _spawnableLayouts.Count)];
            }

            IReadOnlyList<FB_EmptyUnitBoxLayoutType> defaultLayouts = FB_EmptyUnitBoxParent.SpawnableLayoutTypes;
            if (defaultLayouts.Count == 0)
            {
                return _defaultLayoutType;
            }

            return defaultLayouts[Random.Range(0, defaultLayouts.Count)];
        }

        private void CleanupDestroyedParents()
        {
            _spawnedParents.RemoveAll(parent => parent == null);
        }

        private static void DestroySpawnedParent(GameObject parentObject)
        {
            if (Application.isPlaying)
            {
                Destroy(parentObject);
                return;
            }

            DestroyImmediate(parentObject);
        }
    }
}
