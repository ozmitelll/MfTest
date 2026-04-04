using System;
using _Game.Scripts.Gameplay.Entities;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Systems.Modifications
{
    [Serializable]
    public abstract class ModificationEffectDefinition
    {
        public virtual bool AppliesOnInstall => true;

        public virtual bool CanModifySkill(_Game.Scripts.Gameplay.Skills.SkillSlot skillSlot) => false;

        public virtual void ModifySkillContext(Entity entity, ref _Game.Scripts.Gameplay.Skills.SkillContext context) { }

        public abstract void Apply(Entity entity);
        public abstract void Remove(Entity entity);
    }
}
