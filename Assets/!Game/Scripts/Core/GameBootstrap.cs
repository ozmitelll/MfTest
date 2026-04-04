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
        private LevelService _levelService;
        private HUDController _hudController;

        private void Awake()
        {
            ServiceLocator.Instance.Register(new InteractionService());
            ServiceLocator.Instance.Register(new PlayerService());
            ServiceLocator.Instance.Register(new ModificationScreenService());
        }

        private void Start()
        {
            var root = _hudDocument.rootVisualElement;

            _hudController = new HUDController(root);
            _hudController.Subscribe();

            _levelService = new LevelService();
            ServiceLocator.Instance.Register(_levelService);

            _sessionService = ServiceLocator.Instance.Get<SessionService>();
            _sessionService.EnsureSessionRunning();
            _hudController.SetTimer(_sessionService.ElapsedTime);
            _levelService.LoadLevel(_sessionService.GetCurrentStageConfig());

            var player = Object.Instantiate(testPlayer, _levelService.CurrentLevel.playerSpawnPoint.position, Quaternion.identity);
            ServiceLocator.Instance.Get<PlayerService>().SetPlayer(player);
            ServiceLocator.Instance.Get<CursorService>().SetCursor(testPlayer.config.GameplayCursor);
            cameraController.SetTarget(player.transform);

            _hudController.SetSkillSystem(player.SkillSystem);

            EventBus.Publish(new OnPlayerHealthChangedEvent
            {
                Current = player.HealthSystem.CurrentHealth,
                Max     = player.HealthSystem.MaxHealth
            });
        }

        private void Update()
        {
            _sessionService?.Tick(Time.deltaTime);
            _hudController?.Tick();
        }

        private void OnDestroy()
        {
            _hudController?.Dispose();
        }
    }
}
