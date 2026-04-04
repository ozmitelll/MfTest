using System;
using _Game.Scripts.Configs;
using _Game.Scripts.Core;
using _Game.Scripts.Gameplay;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace _Game.Scripts.Services
{
    public class LevelService : IService
    {
        public Level CurrentLevel { get; private set; }
        public LevelConfig CurrentLevelConfig { get; private set; }
        public event Action<Level> OnLevelLoaded;
        public event Action OnLevelUnloaded;

        public void LoadLevel(StageConfig stage)
        {
            UnloadLevel();
            
            var levelCfg = stage.Levels[Random.Range(0, stage.Levels.Length)];
            CurrentLevelConfig = levelCfg;
            CurrentLevel = Object.Instantiate(levelCfg.LevelPrefab);
            CurrentLevel.Initialize(levelCfg);
            OnLevelLoaded?.Invoke(CurrentLevel);
        }

        private void UnloadLevel()
        {
            if(CurrentLevel == null) return;
            
            Object.Destroy(CurrentLevel.gameObject);
            CurrentLevel = null;
            CurrentLevelConfig = null;
            OnLevelUnloaded?.Invoke();
        }
    }
}