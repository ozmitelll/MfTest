using _Game.Scripts.Configs;
using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Entities.Player.Systems;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Entities.Player
{
    public class Player : Entity
    {
        [SerializeField] private PlayerMovementSystem _movementSystem;
        [SerializeField] private PlayerSkillSystem   _skillSystem;
        [SerializeField] private InteractionSystem   _interactionSystem;
        [SerializeField] private PlayerWallet        _wallet;
        public PlayerConfig config;

        public PlayerMovementSystem MovementSystem => _movementSystem;
        public PlayerSkillSystem SkillSystem => _skillSystem;
        public InteractionSystem InteractionSystem => _interactionSystem;
        public PlayerWallet Wallet =>
            _wallet != null ? _wallet : _wallet = GetComponent<PlayerWallet>() ?? gameObject.AddComponent<PlayerWallet>();

        private InputSystem_Actions _input;

        private void Awake()
        {
            _input = new InputSystem_Actions();
            InputBindingsPersistence.ApplySavedOverrides(_input);
            InitializeEntity(config);
            _movementSystem.Initialize(_input.Player, StatsSystem, config);
            _skillSystem.Initialize(_input.Player, this);
            _interactionSystem.Initialize(_input.Player, this);
            Wallet.Initialize(this);
        }

        private void OnEnable()
        {
            _input.Player.Enable();
            HealthSystem.OnHealthChanged += OnHealthChanged;
            HealthSystem.OnDied          += OnDied;
        }

        private void OnDisable()
        {
            _input.Player.Disable();
            HealthSystem.OnHealthChanged -= OnHealthChanged;
            HealthSystem.OnDied          -= OnDied;
        }

        private void OnHealthChanged(float current, float max) =>
            EventBus.Publish(new OnPlayerHealthChangedEvent { Current = current, Max = max });

        private void OnDied()
        {
            _input.Player.Disable();
            EventBus.Publish(new OnPlayerDiedEvent());
        }
    }
}
