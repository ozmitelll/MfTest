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
        public PlayerConfig config;

        public PlayerSkillSystem SkillSystem => _skillSystem;

        private InputSystem_Actions _input;

        private void Awake()
        {
            _input = new InputSystem_Actions();
            StatsSystem.Initialize(config);
            HealthSystem.Initialize(StatsSystem.MaxHealth.Value);
            _movementSystem.Initialize(_input.Player, StatsSystem);
            _skillSystem.Initialize(_input.Player, this);
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

        private void OnDied() =>
            EventBus.Publish(new OnPlayerDiedEvent());
    }
}
