using _Game.Scripts.Configs;
using _Game.Scripts.Core;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Entities.Enemy
{
    public class Enemy : Entity
    {
        [SerializeField] public EnemyConfig Config;

        private void Awake()
        {
            StatsSystem.Initialize(Config);
            HealthSystem.Initialize(StatsSystem.MaxHealth.Value);
        }

        private void OnEnable()  => HealthSystem.OnDied += OnDied;
        private void OnDisable() => HealthSystem.OnDied -= OnDied;

        private void OnDied()
        {
            EventBus.Publish(new OnEnemyDiedEvent { Enemy = this });
            Destroy(gameObject);
        }
    }
}