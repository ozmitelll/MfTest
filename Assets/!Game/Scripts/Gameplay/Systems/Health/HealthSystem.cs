using System;
using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Entities;
using _Game.Scripts.Gameplay.Entities.Bosses;
using _Game.Scripts.Gameplay.Systems.Combat;
using _Game.Scripts.Gameplay.Systems.StatusEffects;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Systems.Health
{
    public class HealthSystem : MonoBehaviour
    {
        private Entity _ownerEntity;
        private StatusEffectSystem _statusEffectSystem;

        public float CurrentHealth { get; private set; }
        public float MaxHealth     { get; private set; }
        public bool  IsDead        { get; private set; }

        public event Action<float, float> OnHealthChanged;  // current, max
        public event Action               OnDied;

        public void Initialize(float maxHealth)
        {
            MaxHealth     = maxHealth;
            CurrentHealth = maxHealth;
            IsDead        = false;
        }

        public void SetMaxHealth(float maxHealth, bool preserveRatio)
        {
            float sanitizedMaxHealth = Mathf.Max(1f, maxHealth);
            float healthRatio = MaxHealth > 0f ? CurrentHealth / MaxHealth : 1f;

            MaxHealth = sanitizedMaxHealth;
            CurrentHealth = preserveRatio
                ? Mathf.Clamp(MaxHealth * healthRatio, 0f, MaxHealth)
                : Mathf.Min(CurrentHealth, MaxHealth);

            PublishHealthChanged();
        }

        public void TakeDamage(
            float amount,
            DamageType damageType = DamageType.Pure,
            bool isStatusDamage = false,
            StatusEffectDefinition sourceStatusEffect = null)
        {
            if (IsDead || amount <= 0f) return;

            _statusEffectSystem ??= GetComponent<StatusEffectSystem>();

            float finalDamage = _statusEffectSystem != null
                ? _statusEffectSystem.ModifyIncomingDamage(amount)
                : amount;

            if (_statusEffectSystem != null &&
                _statusEffectSystem.ShouldIgnoreIncomingDamage(finalDamage, CurrentHealth))
                return;

            float previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - finalDamage);
            float appliedDamage = previousHealth - CurrentHealth;

            if (appliedDamage > 0f)
            {
                _ownerEntity ??= GetComponent<Entity>();
                if (_ownerEntity != null)
                {
                    EventBus.Publish(new OnEntityDamagedEvent
                    {
                        Target = _ownerEntity,
                        Damage = appliedDamage,
                        DamageType = damageType,
                        IsStatusDamage = isStatusDamage,
                        StatusEffect = sourceStatusEffect
                    });
                }
            }

            PublishHealthChanged();

            if (CurrentHealth == 0f)
                Die();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;

            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            PublishHealthChanged();
        }

        private void Die()
        {
            IsDead = true;
            OnDied?.Invoke();
        }

        private void PublishHealthChanged()
        {
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            _ownerEntity ??= GetComponent<Entity>();
            if (_ownerEntity is not Boss boss)
                return;

            EventBus.Publish(new OnBossHealthChangedEvent
            {
                BossInstanceId = boss.GetInstanceID(),
                DisplayName = GetBossDisplayName(boss),
                Current = CurrentHealth,
                Max = MaxHealth
            });
        }

        private static string GetBossDisplayName(Boss boss)
        {
            string rawName = boss.Config != null ? boss.Config.name : boss.name;
            return rawName.Replace("(Clone)", string.Empty).Trim();
        }
    }
}
