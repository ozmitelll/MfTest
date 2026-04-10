using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Entities.Bosses;
using _Game.Scripts.Gameplay.Entities.Player;
using _Game.Scripts.Gameplay.Entities.Player.Systems;
using _Game.Scripts.Services;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace _Game.Scripts.Gameplay.Interactables
{
    public class Portal : MonoBehaviour, IInteractable
    {
        private enum PortalState
        {
            Idle,
            BossAlive,
            ReadyToCharge,
            Charged
        }

        [SerializeField] private string _summonPrompt = "Summon boss";
        [SerializeField] private string _chargePrompt = "Charging portal...";
        [SerializeField] private string _travelPrompt = "Press to enter next stage";
        [SerializeField] private float _bossSpawnForwardOffset = 4f;
        [SerializeField] private float _bossSpawnHeightOffset = 0.5f;
        [SerializeField, Min(0.1f)] private float _chargeDuration = 3f;
        [SerializeField, Min(0.1f)] private float _chargeRadius = 2.5f;
        [SerializeField] private Vector3 _chargeAreaOffset = new(0f, 0.5f, 0f);

        private Level _level;
        private Transform _spawnPoint;
        private Boss _activeBoss;
        private PlayerService _playerService;
        private LevelService _levelService;
        private SessionService _sessionService;
        private float _chargeElapsed;
        private bool _isPlayerInsideChargeArea;
        private bool _isTransitioning;
        private PortalState _state;

        public string InteractionPrompt => _state switch
        {
            PortalState.Idle when HasConfiguredBosses => $"{_summonPrompt}  [F]",
            PortalState.ReadyToCharge when _isPlayerInsideChargeArea => _chargePrompt,
            PortalState.ReadyToCharge => "Stand in the portal field",
            PortalState.Charged when !_isTransitioning => $"{_travelPrompt}  [F]",
            PortalState.Charged => "Transitioning...",
            _ => string.Empty
        };

        private bool HasConfiguredBosses => _level != null && _level.Config != null && _level.Config.BossPrefabs != null && _level.Config.BossPrefabs.Length > 0;

        public float ChargeProgressNormalized => _state == PortalState.Charged
            ? 1f
            : Mathf.Clamp01(_chargeElapsed / Mathf.Max(0.01f, _chargeDuration));

        public bool IsPlayerInsideChargeArea => _isPlayerInsideChargeArea;

        public bool IsCharging => _state == PortalState.ReadyToCharge &&
                                  _isPlayerInsideChargeArea &&
                                  _chargeElapsed < _chargeDuration;

        public bool IsCharged => _state == PortalState.Charged;

        public bool ShouldShowChargeBar => _state == PortalState.ReadyToCharge || _state == PortalState.Charged;

        public void Initialize(Level level, Transform spawnPoint)
        {
            _level = level;
            _spawnPoint = spawnPoint;
            _activeBoss = null;
            _chargeElapsed = 0f;
            _isPlayerInsideChargeArea = false;
            _isTransitioning = false;
            _state = PortalState.Idle;

            if (_spawnPoint != null)
                transform.SetPositionAndRotation(_spawnPoint.position, _spawnPoint.rotation);
        }

        private void Start()
        {
            ServiceLocator.Instance.Get<InteractionService>().Register(this);

            _playerService = ServiceLocator.Instance.Get<PlayerService>();

            _levelService = ServiceLocator.Instance.Get<LevelService>();

            _sessionService = ServiceLocator.Instance.Get<SessionService>();

            EventBus.Subscribe<OnBossDiedEvent>(OnBossDied);
        }

        private void Update()
        {
            if (_state != PortalState.ReadyToCharge)
            {
                _isPlayerInsideChargeArea = false;
                return;
            }

            _isPlayerInsideChargeArea = EvaluatePlayerInChargeArea();
            if (!_isPlayerInsideChargeArea)
                return;

            if (_chargeDuration <= 0f)
            {
                ChargePortal();
                return;
            }

            _chargeElapsed = Mathf.Min(_chargeElapsed + Time.deltaTime, _chargeDuration);
            if (_chargeElapsed >= _chargeDuration)
                ChargePortal();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnBossDiedEvent>(OnBossDied);

            if (ServiceLocator.Instance != null && ServiceLocator.Instance.Has<InteractionService>())
                ServiceLocator.Instance.Get<InteractionService>().Unregister(this);
        }

        public bool CanInteract(Player player)
        {
            return _state switch
            {
                PortalState.Idle => HasConfiguredBosses,
                PortalState.Charged => !_isTransitioning,
                _ => false
            };
        }

        public void Interact(Player player)
        {
            switch (_state)
            {
                case PortalState.Idle:
                    SpawnBoss();
                    break;
                case PortalState.Charged:
                    TravelToNextStage(player);
                    break;
            }
        }

        private void SpawnBoss()
        {
            Boss bossPrefab = GetRandomBossPrefab();
            if (bossPrefab == null)
            {
                Debug.LogWarning($"[{nameof(Portal)}] Boss pool is empty for level '{_level?.name}'.", this);
                return;
            }

            Vector3 spawnPosition = GetBossSpawnPosition();
            Quaternion spawnRotation = _spawnPoint != null ? _spawnPoint.rotation : transform.rotation;
            _activeBoss = Object.Instantiate(bossPrefab, spawnPosition, spawnRotation, _level != null ? _level.transform : null);
            _chargeElapsed = 0f;
            _isPlayerInsideChargeArea = false;
            _isTransitioning = false;
            _state = PortalState.BossAlive;

            EventBus.Publish(new OnBossSpawnedEvent
            {
                Boss = _activeBoss,
                BossInstanceId = _activeBoss.GetInstanceID(),
                DisplayName = GetBossDisplayName(_activeBoss),
                CurrentHealth = _activeBoss.HealthSystem.CurrentHealth,
                MaxHealth = _activeBoss.HealthSystem.MaxHealth
            });
        }

        private Boss GetRandomBossPrefab()
        {
            if (!HasConfiguredBosses)
                return null;

            Boss[] bossPrefabs = _level.Config.BossPrefabs;
            return bossPrefabs[Random.Range(0, bossPrefabs.Length)];
        }

        private Vector3 GetBossSpawnPosition()
        {
            Transform origin = _spawnPoint != null ? _spawnPoint : transform;
            return origin.position + origin.forward * _bossSpawnForwardOffset + Vector3.up * _bossSpawnHeightOffset;
        }

        private void OnBossDied(OnBossDiedEvent evt)
        {
            if (evt.Boss == null || evt.Boss != _activeBoss)
                return;

            _activeBoss = null;
            _chargeElapsed = 0f;
            _isPlayerInsideChargeArea = false;
            _isTransitioning = false;
            _state = PortalState.ReadyToCharge;
        }

        private void ChargePortal()
        {
            if (_state != PortalState.ReadyToCharge)
                return;

            _chargeElapsed = _chargeDuration;
            _isPlayerInsideChargeArea = false;
            _isTransitioning = false;
            _state = PortalState.Charged;
            Debug.Log("Заряжено", this);
            EventBus.Publish(new OnPortalChargedEvent());
        }

        private void TravelToNextStage(Player player)
        {
            if (_state != PortalState.Charged || _isTransitioning)
                return;

            ResolveRuntimeServices();

            _isTransitioning = true;

            if (!_sessionService.NextStage())
            {
                _sessionService.EndSession();
                return;
            }

            _levelService.LoadLevel(_sessionService.GetCurrentStageConfig());

            Level nextLevel = _levelService.CurrentLevel;
            if (nextLevel == null || nextLevel.playerSpawnPoint == null)
            {
                Debug.LogError($"[{nameof(Portal)}] Next level or player spawn point is missing after stage transition.", this);
                return;
            }

            if (player.TryGetComponent(out PlayerMovementSystem movementSystem))
                movementSystem.TeleportTo(nextLevel.playerSpawnPoint.position, nextLevel.playerSpawnPoint.rotation);
            else
                player.transform.SetPositionAndRotation(nextLevel.playerSpawnPoint.position, nextLevel.playerSpawnPoint.rotation);

            EventBus.Publish(new OnPlayerHealthChangedEvent
            {
                Current = player.HealthSystem.CurrentHealth,
                Max = player.HealthSystem.MaxHealth
            });
        }

        private bool EvaluatePlayerInChargeArea()
        {
            ResolveRuntimeServices();

            Player player = _playerService?.Player;
            if (player == null || player.HealthSystem.IsDead)
                return false;

            Vector3 chargeCenter = transform.position + _chargeAreaOffset;
            Vector3 playerPosition = player.transform.position;
            playerPosition.y = chargeCenter.y;

            return (playerPosition - chargeCenter).sqrMagnitude <= _chargeRadius * _chargeRadius;
        }

        private void ResolveRuntimeServices()
        {
            _playerService = ServiceLocator.Instance.Get<PlayerService>();
            _levelService = ServiceLocator.Instance.Get<LevelService>();
            _sessionService = ServiceLocator.Instance.Get<SessionService>();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.24f, 0.77f, 0.92f, 0.85f);
            Gizmos.DrawWireSphere(transform.position + _chargeAreaOffset, _chargeRadius);
        }

        private static string GetBossDisplayName(Boss boss)
        {
            string rawName = boss != null && boss.Config != null ? boss.Config.name : boss != null ? boss.name : string.Empty;
            return rawName.Replace("(Clone)", string.Empty).Trim();
        }
    }
}