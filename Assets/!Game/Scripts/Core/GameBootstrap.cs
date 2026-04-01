using _Game.Scripts.Services;
using UnityEngine;

namespace _Game.Scripts.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        private SessionService _sessionService;
        private LevelService _levelService;
        private void Start()
        {
            _levelService = new LevelService();
            ServiceLocator.Instance.Register(_levelService);
            
            _sessionService = ServiceLocator.Instance.Get<SessionService>();
            
            
            _levelService.LoadLevel(_sessionService.GetCurrentStageConfig());

        }
    }
}