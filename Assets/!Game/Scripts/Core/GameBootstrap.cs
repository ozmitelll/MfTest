using _Game.Scripts.Gameplay.Entities.Player;
using _Game.Scripts.Gameplay.Director;
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
        private DirectorSystem _directorSystem;

        private void Awake()
        {
            ServiceLocator.Instance.Register(new InteractionService());
            ServiceLocator.Instance.Register(new PlayerService());
            ServiceLocator.Instance.Register(new ModificationScreenService());

            if (!ServiceLocator.Instance.Has<ProjectilePoolService>())
                ServiceLocator.Instance.Register(new ProjectilePoolService());
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

            Player playerPrefab = _sessionService.SelectedPlayerPrefab != null
                ? _sessionService.SelectedPlayerPrefab
                : testPlayer;

            if (playerPrefab == null)
            {
                Debug.LogError($"[{nameof(GameBootstrap)}] No player prefab assigned for gameplay startup.", this);
                return;
            }

            var player = Object.Instantiate(playerPrefab, _levelService.CurrentLevel.playerSpawnPoint.position, Quaternion.identity);
            ServiceLocator.Instance.Get<PlayerService>().SetPlayer(player);
            ServiceLocator.Instance.Get<CursorService>().SetCursor(playerPrefab.config.GameplayCursor);
            cameraController.SetTarget(player.transform);

            _directorSystem = new DirectorSystem(
                _sessionService.GameConfig,
                _sessionService,
                _levelService,
                ServiceLocator.Instance.Get<PlayerService>());
            _directorSystem.Initialize();

            _hudController.SetSkillSystem(player.SkillSystem);
            _hudController.SetInteractionContext(player, player.InteractionSystem);
            _hudController.SetCoins(player.Wallet.Coins);

            EventBus.Publish(new OnPlayerHealthChangedEvent
            {
                Current = player.HealthSystem.CurrentHealth,
                Max     = player.HealthSystem.MaxHealth
            });
        }

        private void Update()
        {
            _sessionService?.Tick(Time.deltaTime);
            _directorSystem?.Tick(Time.deltaTime);
            _hudController?.Tick();
        }

        private void OnDestroy()
        {
            _directorSystem?.Dispose();
            _hudController?.Dispose();
        }
    }
}
