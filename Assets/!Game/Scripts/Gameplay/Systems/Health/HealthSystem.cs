using System;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Systems.Health
{
    public class HealthSystem : MonoBehaviour
    {
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

            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if (CurrentHealth == 0f)
                Die();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;

            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        private void Die()
        {
            IsDead = true;
            OnDied?.Invoke();
        }
    }
}
