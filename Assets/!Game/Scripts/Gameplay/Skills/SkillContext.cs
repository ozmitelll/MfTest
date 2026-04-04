using _Game.Scripts.Gameplay.Entities;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Skills
{
    public struct SkillContext
    {
        public Entity  Owner;
        public SkillSlot SkillSlot;
        public Vector3 AimPosition;   // точка прицела в мировых координатах
        public Vector3 AimDirection;  // нормализованное направление от владельца к прицелу
        public float DamageMultiplier;
        public float FlatDamageBonus;
        public float CooldownMultiplier;
        public float ProjectileSpeedMultiplier;

        public float ResolveDamage(float baseDamage) =>
            (baseDamage + FlatDamageBonus) * DamageMultiplier;

        public float ResolveCooldown(float baseCooldown) =>
            Mathf.Max(0.01f, baseCooldown * CooldownMultiplier);

        public float ResolveProjectileSpeed(float baseSpeed) =>
            Mathf.Max(0f, baseSpeed * ProjectileSpeedMultiplier);
    }
}
