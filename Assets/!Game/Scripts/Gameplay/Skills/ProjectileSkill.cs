using UnityEngine;
using _Game.Scripts.Core;
using _Game.Scripts.Services;

namespace _Game.Scripts.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "ProjectileSkill", menuName = "Modfall/Skills/Projectile Skill")]
    public class ProjectileSkill : ActiveSkill
    {
        [Header("Projectile")]
        public GameObject ProjectilePrefab;
        public float      ProjectileSpeed = 15f;
        [Min(0f)] public float SpawnForwardOffset = 0.75f;
        [Min(0f)] public float SpawnHeightOffset = 1f;
        public bool RotateOwnerToAim = true;

        [Header("Fallback Projectile")]
        public bool UseRuntimeFallbackProjectile = true;
        [Min(0.05f)] public float FallbackProjectileRadius = 0.18f;

        // Кулдаун = 1 / AttackRate (чем выше стат, тем быстрее стрельба)
        public override float GetCooldown(SkillContext ctx)
        {
            var rate = ctx.Owner.StatsSystem.AttackRate.Value;
            float baseCooldown = rate > 0f ? 1f / rate : Cooldown;
            return ctx.ResolveCooldown(baseCooldown);
        }

        public override void Activate(SkillContext ctx)
        {
            Vector3 aimDirection = ctx.AimDirection;
            aimDirection.y = 0f;
            if (aimDirection.sqrMagnitude < 0.001f)
                aimDirection = ctx.Owner.transform.forward;

            aimDirection.Normalize();

            if (RotateOwnerToAim)
                ctx.Owner.transform.rotation = Quaternion.LookRotation(aimDirection);

            Vector3 spawnPosition = ctx.Owner.transform.position +
                                    aimDirection * SpawnForwardOffset +
                                    Vector3.up * SpawnHeightOffset;

            GameObject go = CreateProjectileInstance(spawnPosition, aimDirection);
            if (go == null)
                return;

            var proj = go.GetComponent<Projectile>() ?? go.AddComponent<Projectile>();
            proj.Launch(
                aimDirection,
                ctx.ResolveDamage(ctx.Owner.StatsSystem.AttackDamage.Value),
                ctx.ResolveProjectileSpeed(ProjectileSpeed),
                ctx.DamageType,
                ctx.Owner,
                ctx.BuildStatusPayloads()
            );
        }

        private GameObject CreateProjectileInstance(Vector3 spawnPosition, Vector3 aimDirection)
        {
            ProjectilePoolService poolService = ResolvePoolService();

            if (ProjectilePrefab != null)
                return poolService != null
                    ? poolService.GetProjectile(ProjectilePrefab, spawnPosition, Quaternion.LookRotation(aimDirection))?.gameObject
                    : Instantiate(ProjectilePrefab, spawnPosition, Quaternion.LookRotation(aimDirection));

            if (!UseRuntimeFallbackProjectile)
                return null;

            if (poolService != null)
            {
                Projectile pooledProjectile = poolService.GetRuntimeProjectile(
                    GetRuntimeFallbackPoolKey(),
                    CreateRuntimeFallbackProjectile,
                    spawnPosition,
                    Quaternion.LookRotation(aimDirection));
                return pooledProjectile != null ? pooledProjectile.gameObject : null;
            }

            GameObject projectile = CreateRuntimeFallbackProjectile();
            projectile.transform.SetPositionAndRotation(spawnPosition, Quaternion.LookRotation(aimDirection));
            return projectile;
        }

        private ProjectilePoolService ResolvePoolService()
        {
            if (ServiceLocator.Instance == null || !ServiceLocator.Instance.Has<ProjectilePoolService>())
                return null;

            return ServiceLocator.Instance.Get<ProjectilePoolService>();
        }

        private string GetRuntimeFallbackPoolKey() => $"{name}_{GetInstanceID()}_Projectile";

        private GameObject CreateRuntimeFallbackProjectile()
        {
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = $"{name}_RuntimeProjectile";
            projectile.transform.localScale = Vector3.one * (FallbackProjectileRadius * 2f);

            if (projectile.TryGetComponent(out SphereCollider sphereCollider))
                sphereCollider.isTrigger = true;

            Rigidbody rigidbody = projectile.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            return projectile;
        }
    }
}
