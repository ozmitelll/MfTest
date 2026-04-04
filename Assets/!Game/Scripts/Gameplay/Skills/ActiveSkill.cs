using UnityEngine;

namespace _Game.Scripts.Gameplay.Skills
{
    public abstract class ActiveSkill : Skill
    {
        [Header("Active")]
        public float Cooldown      = 1f;
        public bool  HoldToRepeat  = false;

        public virtual bool  CanActivate(SkillContext ctx) => true;
        // Переопредели чтобы считать кулдаун из стата владельца (например 1/AttackRate)
        public virtual float GetCooldown(SkillContext ctx) => Cooldown;

        public abstract void Activate(SkillContext ctx);
    }
}
