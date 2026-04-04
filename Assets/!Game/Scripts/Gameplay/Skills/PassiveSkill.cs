using _Game.Scripts.Gameplay.Entities;

namespace _Game.Scripts.Gameplay.Skills
{
    public abstract class PassiveSkill : Skill
    {
        public abstract void OnApply(Entity owner);
        public abstract void OnRemove(Entity owner);
    }
}
