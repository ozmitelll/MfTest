using _Game.Scripts.Configs;
using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Entities.Enemy;
using UnityEngine;

namespace _Game.Scripts.Services
{
    public class SessionService : IService
    {
        public int CurrentStage { get; private set; }
        public float ElapsedTime { get; private set; }
        public int CurrentDifficultyLevel { get; private set; }
        public bool IsSessionActive { get; private set; }
        public GameConfig GameConfig => _gameConfig;
        
        private GameConfig _gameConfig;
        private float _nextTimerTickTime;
        
        public void SetGameConfig(GameConfig gameCfg) => _gameConfig = gameCfg;

        public StageConfig GetCurrentStageConfig()
        {
            EnsureGameConfigIsValid();
            return _gameConfig.stages[Mathf.Clamp(CurrentStage, 0, _gameConfig.stages.Length - 1)];
        }

        public void StartSession()
        {
            CurrentStage = 0;
            BeginSessionRuntime();
            ServiceLocator.Instance.Get<SceneService>().LoadScene(SceneNames.Game, 0);
        }

        public void EnsureSessionRunning()
        {
            EnsureGameConfigIsValid();

            if (IsSessionActive)
                return;

            BeginSessionRuntime();
        }

        public void Tick(float deltaTime)
        {
            if (!IsSessionActive || _gameConfig == null || deltaTime <= 0f)
                return;

            ElapsedTime += deltaTime;
            PublishTimerTicks();
            UpdateDifficulty();
        }

        public void ApplyDifficulty(Enemy enemy)
        {
            if (enemy == null || _gameConfig == null)
                return;

            enemy.ApplyDifficulty(CurrentDifficultyLevel, _gameConfig.EnemyDifficultyStatPercentPerLevel);
        }

        public bool NextStage()
        {
            CurrentStage++;
            return CurrentStage < _gameConfig.stages.Length;
        }

        public void EndSession()
        {
            EventBus.Publish(new OnSessionEndedEvent());
            ResetRuntimeState();
            CurrentStage = 0;
            ServiceLocator.Instance.Get<SceneService>().LoadScene(SceneNames.Menu, 0);
        }

        private void BeginSessionRuntime()
        {
            ResetRuntimeState();
            IsSessionActive = true;
            EventBus.Publish(new OnTimerTickEvent { Time = 0f });
        }

        private void ResetRuntimeState()
        {
            ElapsedTime = 0f;
            CurrentDifficultyLevel = 0;
            IsSessionActive = false;
            _nextTimerTickTime = GetTimerTickInterval();
        }

        private void PublishTimerTicks()
        {
            float tickInterval = GetTimerTickInterval();

            while (ElapsedTime >= _nextTimerTickTime)
            {
                EventBus.Publish(new OnTimerTickEvent { Time = _nextTimerTickTime });
                _nextTimerTickTime += tickInterval;
            }
        }

        private void UpdateDifficulty()
        {
            float stepInterval = _gameConfig.DifficultyIncreaseIntervalSeconds;
            if (stepInterval <= 0f)
                return;

            int targetDifficultyLevel = Mathf.FloorToInt(ElapsedTime / stepInterval);
            if (targetDifficultyLevel <= CurrentDifficultyLevel)
                return;

            CurrentDifficultyLevel = targetDifficultyLevel;

            Enemy[] activeEnemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (Enemy enemy in activeEnemies)
                ApplyDifficulty(enemy);
        }

        private float GetTimerTickInterval() => _gameConfig != null
            ? Mathf.Max(0.1f, _gameConfig.TimerTickIntervalSeconds)
            : 1f;

        private void EnsureGameConfigIsValid()
        {
            if (_gameConfig == null)
                throw new System.InvalidOperationException($"{nameof(SessionService)} requires an assigned {nameof(GameConfig)}.");

            if (_gameConfig.stages == null || _gameConfig.stages.Length == 0)
                throw new System.InvalidOperationException($"{nameof(GameConfig)} must contain at least one stage.");
        }
    }
}