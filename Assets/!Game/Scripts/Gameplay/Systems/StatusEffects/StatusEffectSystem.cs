using System;
using System.Collections.Generic;
using _Game.Scripts.Gameplay.Entities;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Systems.StatusEffects
{
    public class StatusEffectSystem : MonoBehaviour
    {
        [Serializable]
        private sealed class ActiveStatusEffect
        {
            public StatusEffectDefinition Definition;
            public Entity Source;
            public float RemainingDuration;
            public int Stacks;
            public float SourceDamageSnapshot;
            public float SourceAttackRateSnapshot;
            public float TickElapsed;
        }

        private readonly List<ActiveStatusEffect> _activeEffects = new();

        private Entity _owner;

        public bool BlocksMovement
        {
            get
            {
                foreach (ActiveStatusEffect effect in _activeEffects)
                    if (effect.Definition.BlocksMovement)
                        return true;

                return false;
            }
        }

        public bool BlocksSkills
        {
            get
            {
                foreach (ActiveStatusEffect effect in _activeEffects)
                    if (effect.Definition.BlocksSkills)
                        return true;

                return false;
            }
        }

        public void Initialize(Entity owner)
        {
            _owner = owner;
            _activeEffects.Clear();
        }

        private void Update()
        {
            if (_owner == null || _owner.HealthSystem.IsDead)
                return;

            float deltaTime = Time.deltaTime;
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                ActiveStatusEffect effect = _activeEffects[i];
                effect.RemainingDuration -= deltaTime;

                float tickInterval = ResolveTickInterval(effect);
                if (tickInterval > 0f)
                {
                    effect.TickElapsed += deltaTime;
                    while (effect.TickElapsed >= tickInterval)
                    {
                        effect.TickElapsed -= tickInterval;
                        ApplyTick(effect, tickInterval);
                    }
                }

                if (effect.RemainingDuration > 0f)
                    continue;

                _activeEffects.RemoveAt(i);
            }
        }

        public void ApplyStatus(StatusEffectApplicationPayload payload)
        {
            if (payload.Definition == null || payload.StackCount <= 0)
                return;

            ActiveStatusEffect activeEffect = FindActiveEffect(payload.Definition);
            if (activeEffect == null)
            {
                _activeEffects.Add(new ActiveStatusEffect
                {
                    Definition = payload.Definition,
                    Source = payload.Source,
                    RemainingDuration = payload.Duration,
                    Stacks = Mathf.Clamp(payload.StackCount, 1, payload.Definition.MaxStacks),
                    SourceDamageSnapshot = payload.SourceDamageSnapshot,
                    SourceAttackRateSnapshot = payload.SourceAttackRateSnapshot,
                    TickElapsed = 0f
                });

                return;
            }

            activeEffect.Source = payload.Source;
            activeEffect.SourceDamageSnapshot = payload.SourceDamageSnapshot;
            activeEffect.SourceAttackRateSnapshot = payload.SourceAttackRateSnapshot;

            switch (payload.Definition.StackingMode)
            {
                case StatusEffectStackingMode.RefreshDuration:
                    activeEffect.RemainingDuration = payload.Duration;
                    break;

                case StatusEffectStackingMode.AddStacksAndRefreshDuration:
                    activeEffect.Stacks = Mathf.Clamp(
                        activeEffect.Stacks + payload.StackCount,
                        1,
                        payload.Definition.MaxStacks);
                    activeEffect.RemainingDuration = payload.Duration;
                    break;
            }

            if (payload.Definition.RestartTickOnReapply)
                activeEffect.TickElapsed = 0f;
        }

        public float GetMoveSpeedMultiplier()
        {
            float multiplier = 1f;

            foreach (ActiveStatusEffect effect in _activeEffects)
            {
                float stackedPercent = GetStackedPercent(
                    effect.Definition.MoveSpeedPercentPerStack,
                    effect.Stacks,
                    effect.Definition.MoveSpeedStackingMode);

                if (Mathf.Approximately(stackedPercent, 0f))
                    continue;

                multiplier *= Mathf.Max(0.05f, 1f + stackedPercent);
            }

            return multiplier;
        }

        public float ModifyIncomingDamage(float damage)
        {
            float modifiedDamage = damage;

            foreach (ActiveStatusEffect effect in _activeEffects)
            {
                float extraPercent = effect.Definition.IncomingDamageTakenPercentPerStack * effect.Stacks;
                if (Mathf.Approximately(extraPercent, 0f))
                    continue;

                modifiedDamage *= Mathf.Max(0f, 1f + extraPercent);
            }

            return modifiedDamage;
        }

        public bool ShouldIgnoreIncomingDamage(float incomingDamage, float currentHealth)
        {
            foreach (ActiveStatusEffect effect in _activeEffects)
            {
                if (!effect.Definition.IgnoreNonLethalDamage)
                    continue;

                if (incomingDamage < currentHealth)
                    return true;
            }

            return false;
        }

        private ActiveStatusEffect FindActiveEffect(StatusEffectDefinition definition)
        {
            foreach (ActiveStatusEffect effect in _activeEffects)
            {
                if (effect.Definition == definition)
                    return effect;
            }

            return null;
        }

        private float ResolveTickInterval(ActiveStatusEffect effect) => effect.Definition.TickMode switch
        {
            StatusEffectTickMode.None => 0f,
            StatusEffectTickMode.FixedInterval => Mathf.Max(0.05f, effect.Definition.FixedTickInterval),
            StatusEffectTickMode.SourceAttackRate => effect.SourceAttackRateSnapshot > 0f
                ? 1f / effect.SourceAttackRateSnapshot
                : 1f,
            _ => 0f
        };

        private void ApplyTick(ActiveStatusEffect effect, float tickInterval)
        {
            float damagePerSecond =
                effect.Definition.FlatDamagePerSecond +
                effect.Definition.SourceDamagePercentPerSecond * effect.SourceDamageSnapshot +
                effect.Definition.TargetCurrentHealthPercentPerSecond * _owner.HealthSystem.CurrentHealth;

            if (damagePerSecond <= 0f)
                return;

            float tickDamage = damagePerSecond * tickInterval * Mathf.Max(1, effect.Stacks);
            _owner.HealthSystem.TakeDamage(tickDamage);
        }

        private static float GetStackedPercent(
            float perStackPercent,
            int stacks,
            StatusModifierStackingMode stackingMode)
        {
            if (Mathf.Approximately(perStackPercent, 0f) || stacks <= 0)
                return 0f;

            if (stackingMode == StatusModifierStackingMode.Linear)
                return perStackPercent * stacks;

            float magnitude = Mathf.Abs(perStackPercent);
            float sign = Mathf.Sign(perStackPercent);
            float logarithmicMagnitude = 1f - 1f / (1f + magnitude * stacks);
            return sign * logarithmicMagnitude;
        }
    }
}
