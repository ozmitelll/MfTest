using System.Collections.Generic;
using _Game.Scripts.Gameplay.Entities;
using _Game.Scripts.Gameplay.Systems.StatusEffects;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "ArcSlashSkill", menuName = "Modfall/Skills/Arc Slash Skill")]
    public class ArcSlashSkill : ActiveSkill
    {
        [Header("Arc Slash")]
        [Min(0.1f)] public float Range = 3f;
        [Range(1f, 360f)] public float ArcAngle = 180f;
        [Min(0f)] public float ForwardOffset = 1f;
        public LayerMask HitMask = ~0;
        public bool RotateOwnerToAim = true;

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
            Collider[] hits = Physics.OverlapSphere(attackCenter, Range, HitMask, QueryTriggerInteraction.Ignore);
            float halfArc = ArcAngle * 0.5f;
            float damage = ctx.ResolveDamage(ctx.Owner.StatsSystem.AttackDamage.Value);
            StatusEffectApplicationPayload[] statusPayloads = ctx.BuildStatusPayloads();
            var hitEntities = new HashSet<Entity>();

            for (int i = 0; i < hits.Length; i++)
            {
                Entity entity = hits[i].GetComponent<Entity>() ?? hits[i].GetComponentInParent<Entity>();
                if (entity == null || entity == ctx.Owner || entity.HealthSystem.IsDead || !hitEntities.Add(entity))
                    continue;

                Vector3 toTarget = entity.transform.position - owner.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude < 0.001f)
                    continue;

                if (Vector3.Angle(aimDirection, toTarget.normalized) > halfArc)
                    continue;

                entity.HealthSystem.TakeDamage(damage);

                for (int statusIndex = 0; statusIndex < statusPayloads.Length; statusIndex++)
                    entity.StatusEffectSystem.ApplyStatus(statusPayloads[statusIndex]);
            }
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