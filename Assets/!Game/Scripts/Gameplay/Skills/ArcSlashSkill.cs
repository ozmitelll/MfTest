using System.Collections.Generic;
using _Game.Scripts.Gameplay.Entities;
using _Game.Scripts.Gameplay.Systems.Combat;
using _Game.Scripts.Gameplay.Systems.StatusEffects;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "ArcSlashSkill", menuName = "Modfall/Skills/Arc Slash Skill")]
    public class ArcSlashSkill : ActiveSkill
    {
        private const int InitialHitBufferSize = 16;

        [Header("Arc Slash")]
        [Min(0.1f)] public float Range = 3f;
        [Range(1f, 360f)] public float ArcAngle = 180f;
        [Min(0f)] public float ForwardOffset = 1f;
        public LayerMask HitMask = ~0;
        public bool RotateOwnerToAim = true;

        [System.NonSerialized] private Collider[] _hitBuffer = new Collider[InitialHitBufferSize];

        public override float GetCooldown(SkillContext ctx)
        {
            float rate = ctx.Owner.StatsSystem.AttackRate.Value;
            float baseCooldown = rate > 0f ? 1f / rate : Cooldown;
            return ctx.ResolveCooldown(baseCooldown);
        }

        public override void Activate(SkillContext ctx)
        {
            Transform owner = ctx.Owner.transform;
            Vector3 aimDirection = GetAimDirection(ctx, owner);

            if (RotateOwnerToAim)
                owner.rotation = Quaternion.LookRotation(aimDirection);

            Vector3 attackCenter = owner.position + aimDirection * ForwardOffset;
            int hitCount = CollectHits(attackCenter);
            float halfArc = ArcAngle * 0.5f;
            float damage = ctx.ResolveDamage(ctx.Owner.StatsSystem.AttackDamage.Value);
            DamageType damageType = ctx.DamageType;
            StatusEffectApplicationPayload[] statusPayloads = ctx.BuildStatusPayloads();
            var hitEntities = new HashSet<Entity>();

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _hitBuffer[i];
                Entity entity = hit.GetComponent<Entity>() ?? hit.GetComponentInParent<Entity>();
                if (entity == null ||
                    entity.HealthSystem.IsDead ||
                    !EntityCombatUtility.AreHostile(ctx.Owner, entity) ||
                    !hitEntities.Add(entity))
                    continue;

                Vector3 toTarget = entity.transform.position - owner.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude < 0.001f)
                    continue;

                if (Vector3.Angle(aimDirection, toTarget.normalized) > halfArc)
                    continue;

                entity.HealthSystem.TakeDamage(damage, damageType);

                for (int statusIndex = 0; statusIndex < statusPayloads.Length; statusIndex++)
                    entity.StatusEffectSystem.ApplyStatus(statusPayloads[statusIndex]);
            }
        }

        private int CollectHits(Vector3 attackCenter)
        {
            if (_hitBuffer == null || _hitBuffer.Length == 0)
                _hitBuffer = new Collider[InitialHitBufferSize];

            int hitCount;

            do
            {
                hitCount = Physics.OverlapSphereNonAlloc(
                    attackCenter,
                    Range,
                    _hitBuffer,
                    HitMask,
                    QueryTriggerInteraction.Ignore);

                if (hitCount < _hitBuffer.Length)
                    return hitCount;

                _hitBuffer = new Collider[_hitBuffer.Length * 2];
            }
            while (true);
        }

        private static Vector3 GetAimDirection(SkillContext ctx, Transform owner)
        {
            Vector3 aimDirection = ctx.AimDirection;
            aimDirection.y = 0f;

            if (aimDirection.sqrMagnitude < 0.001f)
                aimDirection = owner.forward;

            aimDirection.y = 0f;
            if (aimDirection.sqrMagnitude < 0.001f)
                aimDirection = Vector3.forward;

            return aimDirection.normalized;
        }
    }
}