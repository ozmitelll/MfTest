using _Game.Scripts.Configs;
using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Entities.Enemy.Systems;
using _Game.Scripts.Gameplay.Systems.Stats;
using _Game.Scripts.Services;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Entities.Enemy
{
    [RequireComponent(typeof(EnemyMovementSystem))]
    [RequireComponent(typeof(EnemyAIStateMachine))]
    public class Enemy : Entity
    {
        public EnemyConfig Config;

        [SerializeField] private EnemySkillSystem _skillSystem;
        [SerializeField] private EnemyMovementSystem _movementSystem;
        [SerializeField] private EnemyAIStateMachine _aiStateMachine;

        private int _appliedDifficultyLevel;

        public EnemySkillSystem SkillSystem =>
            _skillSystem != null ? _skillSystem : _skillSystem = GetComponent<EnemySkillSystem>() ?? gameObject.AddComponent<EnemySkillSystem>();

        public EnemyMovementSystem MovementSystem =>
            _movementSystem != null ? _movementSystem : _movementSystem = GetComponent<EnemyMovementSystem>() ?? gameObject.AddComponent<EnemyMovementSystem>();

        public EnemyAIStateMachine AIStateMachine =>
            _aiStateMachine != null ? _aiStateMachine : _aiStateMachine = GetComponent<EnemyAIStateMachine>() ?? gameObject.AddComponent<EnemyAIStateMachine>();

        protected virtual void Awake()
        {
            InitializeEntity(Config);
            MovementSystem.Initialize(this);
            SkillSystem.Initialize(this);
            AIStateMachine.Initialize(this);

            if (ServiceLocator.Instance != null && ServiceLocator.Instance.Has<SessionService>())
                ServiceLocator.Instance.Get<SessionService>().ApplyDifficulty(this);
        }

        protected virtual void OnEnable()  => HealthSystem.OnDied += OnDied;
        protected virtual void OnDisable() => HealthSystem.OnDied -= OnDied;

        protected virtual void OnDied()
        {
            EventBus.Publish(new OnEnemyDiedEvent { Enemy = this });
            Destroy(gameObject);
        }

        public void ApplyDifficulty(int targetDifficultyLevel, float statPercentPerLevel)
        {
            if (targetDifficultyLevel <= _appliedDifficultyLevel || statPercentPerLevel <= 0f)
                return;

            float additionalPercent = (targetDifficultyLevel - _appliedDifficultyLevel) * statPercentPerLevel;

            ApplyPercentBonus(StatsSystem.MaxHealth, additionalPercent);
            ApplyPercentBonus(StatsSystem.MoveSpeed, additionalPercent);
            ApplyPercentBonus(StatsSystem.Armor, additionalPercent);
            ApplyPercentBonus(StatsSystem.MagicResistance, additionalPercent);
            ApplyPercentBonus(StatsSystem.ElementalResistance, additionalPercent);
            ApplyPercentBonus(StatsSystem.AttackDamage, additionalPercent);
            ApplyPercentBonus(StatsSystem.AttackRange, additionalPercent);
            ApplyPercentBonus(StatsSystem.AttackRate, additionalPercent);

            _appliedDifficultyLevel = targetDifficultyLevel;
            HealthSystem.SetMaxHealth(StatsSystem.MaxHealth.Value, preserveRatio: true);
        }

        private static void ApplyPercentBonus(Stat stat, float additionalPercent)
        {
            if (stat != null)
                stat.AddPercent(additionalPercent);
        }
    }
}
