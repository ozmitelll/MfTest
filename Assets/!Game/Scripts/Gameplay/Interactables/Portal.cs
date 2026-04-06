using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Entities.Bosses;
using _Game.Scripts.Gameplay.Entities.Player;
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
        [SerializeField] private string _chargePrompt = "Charge portal";
        [SerializeField] private float _bossSpawnForwardOffset = 4f;
        [SerializeField] private float _bossSpawnHeightOffset = 0.5f;
        [SerializeField, Min(0.1f)] private float _chargeDuration = 3f;
        [SerializeField, Min(0.1f)] private float _chargeRadius = 2.5f;
        [SerializeField] private Vector3 _chargeAreaOffset = new(0f, 0.5f, 0f);

        private Level _level;
        private Transform _spawnPoint;
        private Boss _activeBoss;
        private PlayerService _playerService;
        private float _chargeElapsed;
        private bool _isPlayerInsideChargeArea;
        private PortalState _state;

        public string InteractionPrompt => _state switch
        {
            PortalState.Idle when HasConfiguredBosses => $"{_summonPrompt}  [F]",
            PortalState.ReadyToCharge when _isPlayerInsideChargeArea => _chargePrompt,
            PortalState.ReadyToCharge => "Stand in the portal field",
            PortalState.Charged => "Portal charged",
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
            _state = PortalState.Idle;

            if (_spawnPoint != null)
                transform.SetPositionAndRotation(_spawnPoint.position, _spawnPoint.rotation);
        }

        private void Start()
        {
            if (ServiceLocator.Instance != null && ServiceLocator.Instance.Has<InteractionService>())
                ServiceLocator.Instance.Get<InteractionService>().Register(this);

            if (ServiceLocator.Instance != null && ServiceLocator.Instance.Has<PlayerService>())
                _playerService = ServiceLocator.Instance.Get<PlayerService>();

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
            if (player == null)
                return false;

            return _state switch
            {
                PortalState.Idle => HasConfiguredBosses,
                _ => false
            };
        }

        public void Interact(Player player)
        {
            if (player == null)
                return;

            switch (_state)
            {
                case PortalState.Idle:
                    SpawnBoss();
                    break;
                case PortalState.ReadyToCharge:
                    ChargePortal();
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
            _state = PortalState.BossAlive;

            EventBus.Publish(new OnBossSpawnedEvent { Boss = _activeBoss });
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
            _state = PortalState.ReadyToCharge;
        }

        private void ChargePortal()
        {
            if (_state != PortalState.ReadyToCharge)
                return;

            _chargeElapsed = _chargeDuration;
            _isPlayerInsideChargeArea = false;
            _state = PortalState.Charged;
            Debug.Log("Заряжено", this);
            EventBus.Publish(new OnPortalChargedEvent());
        }

        private bool EvaluatePlayerInChargeArea()
        {
            if (_playerService == null && ServiceLocator.Instance != null && ServiceLocator.Instance.Has<PlayerService>())
                _playerService = ServiceLocator.Instance.Get<PlayerService>();

            Player player = _playerService?.Player;
            if (player == null)
                return false;

            Vector3 chargeCenter = transform.position + _chargeAreaOffset;
            Vector3 playerPosition = player.transform.position;
            playerPosition.y = chargeCenter.y;

            return (playerPosition - chargeCenter).sqrMagnitude <= _chargeRadius * _chargeRadius;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.24f, 0.77f, 0.92f, 0.85f);
            Gizmos.DrawWireSphere(transform.position + _chargeAreaOffset, _chargeRadius);
        }
    }
}