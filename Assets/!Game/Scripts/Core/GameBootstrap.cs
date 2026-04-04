using _Game.Scripts.Gameplay.Entities.Player;
using _Game.Scripts.Gameplay.TopDownCamera;
using _Game.Scripts.Services;
using _Game.Scripts.UI.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Game.Scripts.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private CameraController cameraController;
        [SerializeField] private UIDocument       _hudDocument;
        [SerializeField] private Player           testPlayer;

        private SessionService _sessionService;
        private LevelService   _levelService;
        private HUDController  _hudController;

        private void Awake()
        {
            ServiceLocator.Instance.Register(new InteractionService());
        }

        private void Start()
        {
            _hudController = new HUDController(_hudDocument.rootVisualElement);
            _hudController.Subscribe();

            _levelService = new LevelService();
            ServiceLocator.Instance.Register(_levelService);

            _sessionService = ServiceLocator.Instance.Get<SessionService>();
            _levelService.LoadLevel(_sessionService.GetCurrentStageConfig());

            var player = Object.Instantiate(testPlayer, _levelService.CurrentLevel.playerSpawnPoint.position, Quaternion.identity);
            ServiceLocator.Instance.Get<CursorService>().SetCursor(testPlayer.config.GameplayCursor);
            cameraController.SetTarget(player.transform);

            _hudController.SetSkillSystem(player.SkillSystem);

            EventBus.Publish(new OnPlayerHealthChangedEvent
            {
                Current = player.HealthSystem.CurrentHealth,
                Max     = player.HealthSystem.MaxHealth
            });
        }

        private void Update() => _hudController?.Tick();

        private void OnDestroy() => _hudController?.Dispose();
    }
}