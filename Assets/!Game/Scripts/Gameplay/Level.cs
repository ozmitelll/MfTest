using _Game.Scripts.Configs;
using _Game.Scripts.Gameplay.Entities.Enemy;
using UnityEngine;

namespace _Game.Scripts.Gameplay
{
    public class Level : MonoBehaviour
    {
        private const float GroundProbeHeight = 50f;
        private const float GroundProbeDistance = 200f;

        [Header("Core")]
        public Transform playerSpawnPoint;

        [Header("Director")]
        [SerializeField] private Collider[] _directorSpawnSurfaces;

        private LevelConfig _config;
        private Collider[] _cachedDirectorSpawnSurfaces;

        public LevelConfig Config => _config;
        public Enemy[] DirectorEnemyPrefabs => _config != null ? _config.DirectorEnemyPrefabs : null;

        public bool HasDirectorEnemies => DirectorEnemyPrefabs != null && DirectorEnemyPrefabs.Length > 0;

        public void Initialize(LevelConfig config)
        {
            _config = config;
            _cachedDirectorSpawnSurfaces = null;
        }

        public bool TryGetRandomDirectorSpawnPosition(Vector3 center, out Vector3 spawnPosition)
        {
            if (_config == null)
            {
                spawnPosition = default;
                return false;
            }

            return TryGetRandomGroundedDirectorPosition(
                center,
                _config.DirectorMinSpawnRadius,
                _config.DirectorMaxSpawnRadius,
                out spawnPosition);
        }

        public bool TryGetRandomDirectorWavePosition(Vector3 center, float maxRadius, out Vector3 spawnPosition)
        {
            return TryGetRandomGroundedDirectorPosition(center, 0f, maxRadius, out spawnPosition);
        }

        private bool TryGetRandomGroundedDirectorPosition(Vector3 center, float minRadius, float maxRadius, out Vector3 spawnPosition)
        {
            if (_config == null)
            {
                spawnPosition = default;
                return false;
            }

            float sanitizedMinRadius = Mathf.Max(0f, minRadius);
            float sanitizedMaxRadius = Mathf.Max(sanitizedMinRadius + 0.1f, maxRadius);

            Vector2 direction = Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude < 0.001f)
                direction = Vector2.right;

            float distance = Random.Range(sanitizedMinRadius, sanitizedMaxRadius);
            Vector3 candidate = center + new Vector3(direction.x, 0f, direction.y) * distance;
            return TryProjectToSpawnSurface(candidate, out spawnPosition);
        }

        private bool TryProjectToSpawnSurface(Vector3 point, out Vector3 spawnPosition)
        {
            Collider[] spawnSurfaces = GetDirectorSpawnSurfaces();
            if (spawnSurfaces == null || spawnSurfaces.Length == 0)
            {
                spawnPosition = default;
                return false;
            }

            Vector3 rayOrigin = new Vector3(point.x, transform.position.y + GroundProbeHeight, point.z);
            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, GroundProbeDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                spawnPosition = default;
                return false;
            }

            if (!IsValidDirectorSpawnSurface(hit.collider))
            {
                spawnPosition = default;
                return false;
            }

            spawnPosition = hit.point + Vector3.up * _config.DirectorSpawnHeight;
            return true;
        }

        private Collider[] GetDirectorSpawnSurfaces()
        {
            if (_cachedDirectorSpawnSurfaces != null && _cachedDirectorSpawnSurfaces.Length > 0)
                return _cachedDirectorSpawnSurfaces;

            if (_directorSpawnSurfaces != null && _directorSpawnSurfaces.Length > 0)
            {
                _cachedDirectorSpawnSurfaces = _directorSpawnSurfaces;
                return _cachedDirectorSpawnSurfaces;
            }

            _cachedDirectorSpawnSurfaces = GetComponentsInChildren<Collider>(includeInactive: true);
            return _cachedDirectorSpawnSurfaces;
        }

        private bool IsValidDirectorSpawnSurface(Collider collider)
        {
            if (collider == null || collider.isTrigger)
                return false;

            Collider[] spawnSurfaces = GetDirectorSpawnSurfaces();
            for (int i = 0; i < spawnSurfaces.Length; i++)
            {
                if (spawnSurfaces[i] == collider)
                    return true;
            }

            return false;
        }
    }
}