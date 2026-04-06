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

        private Level _level;
        private Transform _spawnPoint;
        private Boss _activeBoss;
        private PortalState _state;

        public string InteractionPrompt => _state switch
        {
            PortalState.Idle when HasConfiguredBosses => $"{_summonPrompt}  [F]",
            PortalState.ReadyToCharge => $"{_chargePrompt}  [F]",
            PortalState.Charged => "Portal charged",
            _ => string.Empty
        };

        private bool HasConfiguredBosses => _level != null && _level.Config != null && _level.Config.BossPrefabs != null && _level.Config.BossPrefabs.Length > 0;

        public void Initialize(Level level, Transform spawnPoint)
        {
            _level = level;
            _spawnPoint = spawnPoint;
            _activeBoss = null;
            _state = PortalState.Idle;

            if (_spawnPoint != null)
                transform.SetPositionAndRotation(_spawnPoint.position, _spawnPoint.rotation);
        }

        private void Start()
        {
            if (ServiceLocator.Instance != null && ServiceLocator.Instance.Has<InteractionService>())
                ServiceLocator.Instance.Get<InteractionService>().Register(this);

            EventBus.Subscribe<OnBossDiedEvent>(OnBossDied);
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
                PortalState.ReadyToCharge => true,
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
            _state = PortalState.BossAlive;
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
            _state = PortalState.ReadyToCharge;
        }

        private void ChargePortal()
        {
            if (_state != PortalState.ReadyToCharge)
                return;

            _state = PortalState.Charged;
            Debug.Log("Заряжено", this);
            EventBus.Publish(new OnPortalChargedEvent());
        }
    }
}