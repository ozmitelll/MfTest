using System.Collections.Generic;
using _Game.Scripts.Configs;
using _Game.Scripts.Core;
using _Game.Scripts.Gameplay;
using _Game.Scripts.Gameplay.Entities.Enemy;
using _Game.Scripts.Gameplay.Entities.Player;
using _Game.Scripts.Services;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Game.Scripts.Gameplay.Director
{
    public sealed class DirectorSystem
    {
        private const float SpawnOccupancyRadius = 2.5f;
        private const float PlayerPressureWindow = 0.75f;
        private const int WaveCenterAttempts = 10;
        private const int WaveMemberAttempts = 10;

        private readonly List<Enemy> _availableEnemyPrefabs = new();
        private readonly List<Enemy> _activeEnemies = new();

        private readonly GameConfig _gameConfig;
        private readonly SessionService _sessionService;
        private readonly LevelService _levelService;
        private readonly PlayerService _playerService;

        private Level _currentLevel;
        private float _credits;
        private float _spawnTimer;
        private float _lowestSpawnCost = float.MaxValue;
        private float _highestSpawnCost;

        public DirectorSystem(
            GameConfig gameConfig,
            SessionService sessionService,
            LevelService levelService,
            PlayerService playerService)
        {
            _gameConfig = gameConfig;
            _sessionService = sessionService;
            _levelService = levelService;
            _playerService = playerService;
        }

        public void Initialize()
        {
            EventBus.Subscribe<OnEnemyDiedEvent>(OnEnemyDied);
            _levelService.OnLevelLoaded += OnLevelLoaded;
            _levelService.OnLevelUnloaded += OnLevelUnloaded;

            ResetRuntimeState();

            if (_levelService.CurrentLevel != null)
                OnLevelLoaded(_levelService.CurrentLevel);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<OnEnemyDiedEvent>(OnEnemyDied);

            if (_levelService != null)
            {
                _levelService.OnLevelLoaded -= OnLevelLoaded;
                _levelService.OnLevelUnloaded -= OnLevelUnloaded;
            }

            ClearLevelState();
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || _gameConfig == null || !_sessionService.IsSessionActive || _availableEnemyPrefabs.Count == 0)
                return;

            RefreshActiveEnemies();

            _credits = Mathf.Min(_credits + GetCreditsPerSecond() * deltaTime, GetMaxStoredCredits());
            _spawnTimer += deltaTime;

            float spawnInterval = Mathf.Max(0.1f, _gameConfig.DirectorSpawnIntervalSeconds);
            while (_spawnTimer >= spawnInterval)
            {
                _spawnTimer -= spawnInterval;
                TrySpawnWave();
            }
        }

        private void OnLevelLoaded(Level level)
        {
            CacheLevelEnemies(level);
            _credits = Mathf.Max(_credits, _gameConfig.DirectorStartingCredits);
        }

        private void OnLevelUnloaded() => ClearLevelState();

        private void OnEnemyDied(OnEnemyDiedEvent evt)
        {
            if (evt.Enemy != null)
                _activeEnemies.Remove(evt.Enemy);
        }

        private void ResetRuntimeState()
        {
            _credits = Mathf.Max(0f, _gameConfig.DirectorStartingCredits);
            _spawnTimer = 0f;
            _activeEnemies.Clear();
        }

        private void CacheLevelEnemies(Level level)
        {
            ClearLevelState();

            _currentLevel = level;

            if (level == null || !level.HasDirectorEnemies)
                return;

            foreach (Enemy enemyPrefab in level.DirectorEnemyPrefabs)
            {
                if (enemyPrefab == null || enemyPrefab.Config == null)
                    continue;

                _availableEnemyPrefabs.Add(enemyPrefab);
                float cost = Mathf.Max(1f, enemyPrefab.Config.DirectorCost);
                _lowestSpawnCost = Mathf.Min(_lowestSpawnCost, cost);
                _highestSpawnCost = Mathf.Max(_highestSpawnCost, cost);
            }
        }

        private void ClearLevelState()
        {
            _currentLevel = null;
            _availableEnemyPrefabs.Clear();
            _activeEnemies.Clear();
            _lowestSpawnCost = float.MaxValue;
            _highestSpawnCost = 0f;
        }

        private bool TrySpawnWave()
        {
            int availableSlots = GetMaxAliveEnemies() - _activeEnemies.Count;
            if (availableSlots <= 0 || _lowestSpawnCost == float.MaxValue || _credits < _lowestSpawnCost)
                return false;

            float playerPressure = GetPlayerPressure();
            int targetWaveSize = GetTargetWaveSize(availableSlots);
            if (targetWaveSize <= 0)
                return false;

            if (!TryGetWaveCenter(out Vector3 waveCenter))
                return false;

            float remainingCredits = _credits;
            var reservedWavePositions = new List<Vector3>(targetWaveSize);
            int spawnedCount = 0;

            while (spawnedCount < targetWaveSize)
            {
                if (!TrySelectEnemyPrefab(remainingCredits, playerPressure, out Enemy enemyPrefab))
                    break;

                if (!TryGetWaveMemberPosition(waveCenter, reservedWavePositions, out Vector3 spawnPosition))
                    break;

                Enemy enemy = Object.Instantiate(
                    enemyPrefab,
                    spawnPosition,
                    Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                    _currentLevel.transform);

                enemy.gameObject.SetActive(true);

                float cost = Mathf.Max(1f, enemyPrefab.Config.DirectorCost);
                remainingCredits = Mathf.Max(0f, remainingCredits - cost);
                reservedWavePositions.Add(spawnPosition);
                _activeEnemies.Add(enemy);
                spawnedCount++;
            }

            if (spawnedCount == 0)
                return false;

            _credits = remainingCredits;
            return true;
        }

        private bool TrySelectEnemyPrefab(float availableCredits, float playerPressure, out Enemy enemyPrefab)
        {
            RefreshActiveEnemies();

            var candidates = new List<SpawnCandidate>(_availableEnemyPrefabs.Count);

            foreach (Enemy candidatePrefab in _availableEnemyPrefabs)
            {
                if (candidatePrefab == null || candidatePrefab.Config == null)
                    continue;

                AddCandidate(candidates, candidatePrefab, availableCredits, playerPressure);
            }

            if (candidates.Count == 0)
            {
                enemyPrefab = null;
                return false;
            }

            float totalWeight = 0f;
            foreach (SpawnCandidate entry in candidates)
                totalWeight += entry.Weight;

            if (totalWeight <= 0f)
            {
                enemyPrefab = null;
                return false;
            }

            float roll = Random.value * totalWeight;
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= candidates[i].Weight;
                if (roll > 0f)
                    continue;

                enemyPrefab = candidates[i].EnemyPrefab;
                return true;
            }

            enemyPrefab = candidates[candidates.Count - 1].EnemyPrefab;
            return true;
        }

        private void AddCandidate(List<SpawnCandidate> candidates, Enemy enemyPrefab, float availableCredits, float playerPressure)
        {
            float cost = Mathf.Max(1f, enemyPrefab.Config.DirectorCost);
            if (availableCredits < cost)
                return;

            float normalizedCost = _highestSpawnCost > _lowestSpawnCost
                ? Mathf.InverseLerp(_lowestSpawnCost, _highestSpawnCost, cost)
                : 0f;

            float weight = Mathf.Lerp(1f, 1f + normalizedCost * 2f, playerPressure);
            candidates.Add(new SpawnCandidate(enemyPrefab, cost, weight));
        }

        private int GetTargetWaveSize(int availableSlots)
        {
            int minWaveSize = Mathf.Max(1, _gameConfig.DirectorMinWaveSize);
            int maxWaveSize = Mathf.Max(minWaveSize, _gameConfig.DirectorMaxWaveSize);
            int maxAffordableCount = Mathf.FloorToInt(_credits / Mathf.Max(1f, _lowestSpawnCost));

            if (maxAffordableCount <= 0)
                return 0;

            int desiredWaveSize = Random.Range(minWaveSize, maxWaveSize + 1);
            return Mathf.Clamp(desiredWaveSize, 1, Mathf.Min(availableSlots, maxAffordableCount));
        }

        private float GetCreditsPerSecond() =>
            Mathf.Max(0.1f,
                _gameConfig.DirectorCreditsPerSecond +
                _sessionService.CurrentDifficultyLevel * _gameConfig.DirectorCreditsPerSecondPerDifficulty);

        private int GetMaxAliveEnemies() =>
            Mathf.Max(1,
                _gameConfig.DirectorBaseMaxAliveEnemies +
                _sessionService.CurrentDifficultyLevel * _gameConfig.DirectorMaxAliveEnemiesPerDifficulty);

        private float GetMaxStoredCredits()
        {
            float fallbackCap = Mathf.Max(_gameConfig.DirectorStartingCredits, 30f);
            if (_highestSpawnCost <= 0f)
                return fallbackCap;

            return Mathf.Max(fallbackCap, _highestSpawnCost * 1.5f);
        }

        private float GetPlayerPressure()
        {
            float threshold = Mathf.Max(1f, _gameConfig.DirectorPlayerStrengthThreshold);
            float playerAdvantage = EvaluatePlayerAdvantage();

            if (playerAdvantage <= threshold)
                return 0f;

            return Mathf.Clamp01((playerAdvantage - threshold) / PlayerPressureWindow);
        }

        private float EvaluatePlayerAdvantage()
        {
            Player player = _playerService.Player;
            if (player == null || player.config == null)
                return 1f;

            PlayerConfig config = player.config;

            float damageRatio = player.StatsSystem.AttackDamage.Value / Mathf.Max(1f, config.AttackDamage);
            float attackRateRatio = player.StatsSystem.AttackRate.Value / Mathf.Max(0.1f, config.AttackRate);
            float healthRatio = player.HealthSystem.MaxHealth / Mathf.Max(1f, config.MaxHealth);
            float loadoutRatio = player.ModificationLoadoutSystem.Capacity > 0
                ? (float)player.ModificationLoadoutSystem.UsedCapacity / player.ModificationLoadoutSystem.Capacity
                : 0f;

            float rawPower = (damageRatio + attackRateRatio + healthRatio) / 3f + loadoutRatio * 0.5f;
            float expectedPower = 1f + _sessionService.CurrentDifficultyLevel * _gameConfig.DirectorExpectedPlayerPowerPerDifficulty;

            return rawPower / Mathf.Max(0.1f, expectedPower);
        }

        private bool TryGetWaveCenter(out Vector3 spawnPosition)
        {
            Player player = _playerService.Player;
            if (_currentLevel == null || player == null)
            {
                spawnPosition = default;
                return false;
            }

            for (int attempt = 0; attempt < WaveCenterAttempts; attempt++)
            {
                if (!_currentLevel.TryGetRandomDirectorSpawnPosition(player.transform.position, out spawnPosition))
                    continue;

                if (IsSpawnPositionAvailable(spawnPosition, null))
                    return true;
            }

            spawnPosition = default;
            return false;
        }

        private bool TryGetWaveMemberPosition(Vector3 waveCenter, List<Vector3> reservedWavePositions, out Vector3 spawnPosition)
        {
            if (reservedWavePositions.Count == 0 && IsSpawnPositionAvailable(waveCenter, reservedWavePositions))
            {
                spawnPosition = waveCenter;
                return true;
            }

            for (int attempt = 0; attempt < WaveMemberAttempts; attempt++)
            {
                if (!_currentLevel.TryGetRandomDirectorWavePosition(waveCenter, _gameConfig.DirectorWaveSpreadRadius, out spawnPosition))
                    continue;

                if (IsSpawnPositionAvailable(spawnPosition, reservedWavePositions))
                    return true;
            }

            spawnPosition = default;
            return false;
        }

        private bool IsSpawnPositionAvailable(Vector3 spawnPosition, List<Vector3> reservedWavePositions)
        {
            for (int i = 0; i < _activeEnemies.Count; i++)
            {
                Enemy enemy = _activeEnemies[i];
                if (enemy == null)
                    continue;

                if (Vector3.Distance(enemy.transform.position, spawnPosition) <= SpawnOccupancyRadius)
                    return false;
            }

            if (reservedWavePositions != null)
            {
                for (int i = 0; i < reservedWavePositions.Count; i++)
                {
                    if (Vector3.Distance(reservedWavePositions[i], spawnPosition) <= SpawnOccupancyRadius)
                        return false;
                }
            }

            return true;
        }

        private void RefreshActiveEnemies()
        {
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                if (_activeEnemies[i] == null)
                    _activeEnemies.RemoveAt(i);
            }
        }

        private readonly struct SpawnCandidate
        {
            public SpawnCandidate(Enemy enemyPrefab, float cost, float weight)
            {
                EnemyPrefab = enemyPrefab;
                Cost = cost;
                Weight = weight;
            }

            public Enemy EnemyPrefab { get; }
            public float Cost { get; }
            public float Weight { get; }
        }
    }
}