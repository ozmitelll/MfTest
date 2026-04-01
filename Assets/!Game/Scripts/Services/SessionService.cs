using _Game.Scripts.Configs;
using _Game.Scripts.Core;

namespace _Game.Scripts.Services
{
    public class SessionService : IService
    {
        public int CurrentStage { get; private set; }
        
        private GameConfig _gameConfig;
        
        public void SetGameConfig(GameConfig gameCfg) => _gameConfig = gameCfg;
        public StageConfig GetCurrentStageConfig() => _gameConfig.stages[CurrentStage];

        public void StartSession()
        {
            CurrentStage = 0;
            ServiceLocator.Instance.Get<SceneService>().LoadScene(SceneNames.Game, 0);
        }

        public bool NextStage()
        {
            CurrentStage++;
            return CurrentStage < _gameConfig.stages.Length;
        }

        public void EndSession()
        {
            CurrentStage = 0;
            ServiceLocator.Instance.Get<SceneService>().LoadScene(SceneNames.Menu, 0);
        }
    }
}