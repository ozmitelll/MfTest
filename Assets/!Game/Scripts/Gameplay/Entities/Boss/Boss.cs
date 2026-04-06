using _Game.Scripts.Configs;
using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Systems.Stats;
using _Game.Scripts.Services;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Entities.Bosses
{
    [RequireComponent(typeof(BossSkillSystem))]
    public class Boss : Entity
    {
        public EnemyConfig Config;

        [SerializeField] private BossSkillSystem _skillSystem;

        private int _appliedDifficultyLevel;

        public BossSkillSystem SkillSystem =>
            _skillSystem != null ? _skillSystem : _skillSystem = GetComponent<BossSkillSystem>() ?? gameObject.AddComponent<BossSkillSystem>();

        private void Awake()
        {
            InitializeEntity(Config);
            SkillSystem.Initialize(this);

            if (ServiceLocator.Instance != null && ServiceLocator.Instance.Has<SessionService>())
                ServiceLocator.Instance.Get<SessionService>().ApplyDifficulty(this);
        }

        private void OnEnable() => HealthSystem.OnDied += OnDied;

        private void OnDisable() => HealthSystem.OnDied -= OnDied;

        private void OnDied()
        {
            EventBus.Publish(new OnBossDiedEvent { Boss = this });
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