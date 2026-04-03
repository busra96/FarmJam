using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Signals;
using UnityEngine;

namespace FarmBlast
{
    public class EmptyBoxSpawnArea : MonoBehaviour
    {
        private const int RESPAWN_CHECK_DELAY_FRAMES = 2;

        private static readonly List<EmptyBoxSpawnArea> REGISTERED_SPAWN_AREAS = new List<EmptyBoxSpawnArea>();
        private static bool _isListeningSignals;
        private static bool _isRespawnCheckQueued;
        private static bool _isRespawningAll;

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

        private void OnEnable()
        {
            RegisterSpawnArea(this);
        }

        private void OnDisable()
        {
            UnregisterSpawnArea(this);
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

        private static void RegisterSpawnArea(EmptyBoxSpawnArea spawnArea)
        {
            if (spawnArea == null || REGISTERED_SPAWN_AREAS.Contains(spawnArea))
            {
                SubscribeToSignalsIfNeeded();
                return;
            }

            REGISTERED_SPAWN_AREAS.Add(spawnArea);
            SubscribeToSignalsIfNeeded();
        }

        private static void UnregisterSpawnArea(EmptyBoxSpawnArea spawnArea)
        {
            if (spawnArea != null)
            {
                REGISTERED_SPAWN_AREAS.Remove(spawnArea);
            }

            if (REGISTERED_SPAWN_AREAS.Count == 0 && _isListeningSignals)
            {
                FB_EmptyBoxSignals.OnTheEmptyBoxRemoved.RemoveListener(OnAnyEmptyBoxParentRemoved);
                _isListeningSignals = false;
            }
        }

        private static void SubscribeToSignalsIfNeeded()
        {
            if (_isListeningSignals)
            {
                return;
            }

            FB_EmptyBoxSignals.OnTheEmptyBoxRemoved.RemoveListener(OnAnyEmptyBoxParentRemoved);
            FB_EmptyBoxSignals.OnTheEmptyBoxRemoved.AddListener(OnAnyEmptyBoxParentRemoved);
            _isListeningSignals = true;
        }

        private static void OnAnyEmptyBoxParentRemoved(FB_EmptyUnitBoxParent removedParent)
        {
            if (_isRespawningAll || _isRespawnCheckQueued || removedParent == null)
            {
                return;
            }

            WaitAndRespawnAllIfNeeded().Forget();
        }

        private static async UniTaskVoid WaitAndRespawnAllIfNeeded()
        {
            _isRespawnCheckQueued = true;

            try
            {
                await UniTask.DelayFrame(RESPAWN_CHECK_DELAY_FRAMES);

                CleanupRegisteredAreas();
                if (!ShouldRespawnAllAreas())
                {
                    return;
                }

                _isRespawningAll = true;
                for (int i = 0; i < REGISTERED_SPAWN_AREAS.Count; i++)
                {
                    EmptyBoxSpawnArea spawnArea = REGISTERED_SPAWN_AREAS[i];
                    if (spawnArea == null || !spawnArea.isActiveAndEnabled)
                    {
                        continue;
                    }

                    spawnArea.Spawn();
                }
            }
            finally
            {
                _isRespawningAll = false;
                _isRespawnCheckQueued = false;
            }
        }

        private static void CleanupRegisteredAreas()
        {
            REGISTERED_SPAWN_AREAS.RemoveAll(spawnArea => spawnArea == null || !spawnArea.isActiveAndEnabled);

            for (int i = 0; i < REGISTERED_SPAWN_AREAS.Count; i++)
            {
                REGISTERED_SPAWN_AREAS[i].CleanupDestroyedParents();
            }
        }

        private static bool ShouldRespawnAllAreas()
        {
            if (REGISTERED_SPAWN_AREAS.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < REGISTERED_SPAWN_AREAS.Count; i++)
            {
                if (REGISTERED_SPAWN_AREAS[i]._spawnedParents.Count > 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
