using UnityEngine;

namespace _Game.Scripts.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "ProjectileSkill", menuName = "Modfall/Skills/Projectile Skill")]
    public class ProjectileSkill : ActiveSkill
    {
        [Header("Projectile")]
        public GameObject ProjectilePrefab;
        public float      ProjectileSpeed = 15f;

        // Кулдаун = 1 / AttackRate (чем выше стат, тем быстрее стрельба)
        public override float GetCooldown(SkillContext ctx)
        {
            var rate = ctx.Owner.StatsSystem.AttackRate.Value;
            float baseCooldown = rate > 0f ? 1f / rate : Cooldown;
            return ctx.ResolveCooldown(baseCooldown);
        }

        public override void Activate(SkillContext ctx)
        {
            if (ProjectilePrefab == null) return;

            var go   = Instantiate(ProjectilePrefab, ctx.Owner.transform.position, Quaternion.identity);
            var proj = go.GetComponent<Projectile>();
            proj.Launch(
                ctx.AimDirection,
                ctx.ResolveDamage(ctx.Owner.StatsSystem.AttackDamage.Value),
                ctx.ResolveProjectileSpeed(ProjectileSpeed),
                ctx.DamageType,
                ctx.Owner,
                ctx.BuildStatusPayloads()
            );
        }
    }
}
