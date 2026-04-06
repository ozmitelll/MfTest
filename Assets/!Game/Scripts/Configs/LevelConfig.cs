using _Game.Scripts.Gameplay;
using _Game.Scripts.Gameplay.Entities.Bosses;
using _Game.Scripts.Gameplay.Entities.Enemy;
using UnityEngine;

namespace _Game.Scripts.Configs
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Modfall/Configs/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [SerializeField] private Level _levelPrefab;

        [Header("Director")]
        [SerializeField] private Enemy[] _directorEnemyPrefabs;
        [Min(1f)] [SerializeField] private float _directorMinSpawnRadius = 16f;
        [Min(1f)] [SerializeField] private float _directorMaxSpawnRadius = 30f;
        [SerializeField] private float _directorSpawnHeight = 1f;

        [Header("Boss")]
        [SerializeField] private Boss[] _bossPrefabs;

        public Level LevelPrefab => _levelPrefab;
        public Enemy[] DirectorEnemyPrefabs => _directorEnemyPrefabs;
        public float DirectorMinSpawnRadius => _directorMinSpawnRadius;
        public float DirectorMaxSpawnRadius => _directorMaxSpawnRadius;
        public float DirectorSpawnHeight => _directorSpawnHeight;
        public Boss[] BossPrefabs => _bossPrefabs;
    }
}
