using System.Collections.Generic;
using _Game.Scripts.Gameplay.Entities;
using _Game.Scripts.Gameplay.Systems.Combat;
using _Game.Scripts.Gameplay.Systems.StatusEffects;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Skills
{
    public struct SkillContext
    {
        public Entity  Owner;
        public SkillSlot SkillSlot;
        public DamageType DamageType;
        public Vector3 AimPosition;   // точка прицела в мировых координатах
        public Vector3 AimDirection;  // нормализованное направление от владельца к прицелу
        public float DamageMultiplier;
        public float FlatDamageBonus;
        public float CooldownMultiplier;
        public float ProjectileSpeedMultiplier;
        public List<OutgoingStatusRequest> OutgoingStatuses;

        public float ResolveDamage(float baseDamage) =>
            (baseDamage + FlatDamageBonus) * DamageMultiplier;

        public float ResolveCooldown(float baseCooldown) =>
            Mathf.Max(0.01f, baseCooldown * CooldownMultiplier);

        public float ResolveProjectileSpeed(float baseSpeed) =>
            Mathf.Max(0f, baseSpeed * ProjectileSpeedMultiplier);

        public void AddOutgoingStatus(OutgoingStatusRequest statusRequest)
        {
            OutgoingStatuses ??= new List<OutgoingStatusRequest>();
            OutgoingStatuses.Add(statusRequest);
        }

        public StatusEffectApplicationPayload[] BuildStatusPayloads()
        {
            if (OutgoingStatuses == null || OutgoingStatuses.Count == 0)
                return System.Array.Empty<StatusEffectApplicationPayload>();

            float resolvedDamage = ResolveDamage(Owner.StatsSystem.AttackDamage.Value);
            float attackRate = Owner.StatsSystem.AttackRate.Value;
            var payloads = new StatusEffectApplicationPayload[OutgoingStatuses.Count];

            for (int i = 0; i < OutgoingStatuses.Count; i++)
            {
                OutgoingStatusRequest request = OutgoingStatuses[i];
                if (request.Definition == null)
                    continue;

                payloads[i] = new StatusEffectApplicationPayload
                {
                    Definition = request.Definition,
                    Source = Owner,
                    Duration = request.DurationOverride > 0f ? request.DurationOverride : request.Definition.DefaultDuration,
                    StackCount = Mathf.Max(1, request.StackCount),
                    SourceDamageSnapshot = resolvedDamage,
                    SourceAttackRateSnapshot = attackRate,
                    DamageType = DamageType
                };
            }

            return payloads;
        }
    }
}
