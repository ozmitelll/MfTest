using _Game.Scripts.Gameplay.Entities;
using _Game.Scripts.Gameplay.Systems.Combat;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Skills
{
    public abstract class ActiveSkill : Skill
    {
        [Header("Active")]
        public float Cooldown      = 1f;
        public bool  HoldToRepeat  = false;

        [Header("Damage")]
        public bool UseOwnerAttackDamageType = true;
        public DamageType DamageType = DamageType.Pure;

        public virtual bool  CanActivate(SkillContext ctx) => true;
        // Переопредели чтобы считать кулдаун из стата владельца (например 1/AttackRate)
        public virtual float GetCooldown(SkillContext ctx) => ctx.ResolveCooldown(Cooldown);

        public DamageType ResolveDamageType(Entity owner)
        {
            if (UseOwnerAttackDamageType && owner != null)
                return owner.StatsSystem.AttackDamageType;

            return DamageType;
        }

        public abstract void Activate(SkillContext ctx);
    }
}
