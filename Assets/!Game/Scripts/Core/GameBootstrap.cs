using _Game.Scripts.Gameplay.Entities.Player;
using _Game.Scripts.Gameplay.TopDownCamera;
using _Game.Scripts.Services;
using UnityEngine;

namespace _Game.Scripts.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private CameraController cameraController;
        [SerializeField] private Player testPlayer; 
        private SessionService _sessionService;
        private LevelService _levelService;
        
        private void Start()
        {
            _levelService = new LevelService();
            ServiceLocator.Instance.Register(_levelService);
            
            _sessionService = ServiceLocator.Instance.Get<SessionService>();
            _levelService.LoadLevel(_sessionService.GetCurrentStageConfig());

            var player = Object.Instantiate(testPlayer, _levelService.CurrentLevel.playerSpawnPoint.position, Quaternion.identity);
            cameraController.SetTarget(player.transform);
        }
    }
}