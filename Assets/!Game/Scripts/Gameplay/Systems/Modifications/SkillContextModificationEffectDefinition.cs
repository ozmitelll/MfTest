using _Game.Scripts.Gameplay.Entities;
using _Game.Scripts.Gameplay.Skills;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Systems.Modifications
{
    [CreateAssetMenu(fileName = "SkillContextModificationEffect", menuName = "Modfall/Modifications/Skill Context Effect")]
    public class SkillContextModificationEffectDefinition : ModificationEffectDefinition
    {
        [SerializeField] private SkillTargetMask _targets = SkillTargetMask.AllActive;
        [SerializeField] private SkillModifierValueType _valueType;
        [SerializeField] private float _value;

        public override bool AppliesOnInstall => false;

        public override bool CanModifySkill(SkillSlot skillSlot) =>
            skillSlot != SkillSlot.None && (_targets & ToMask(skillSlot)) != 0;

        public override void ModifySkillContext(Entity entity, ref SkillContext context)
        {
            switch (_valueType)
            {
                case SkillModifierValueType.DamageFlat:
                    context.FlatDamageBonus += _value;
                    break;

                case SkillModifierValueType.DamagePercent:
                    context.DamageMultiplier += _value;
                    break;

                case SkillModifierValueType.CooldownPercent:
                    context.CooldownMultiplier += _value;
                    break;

                case SkillModifierValueType.ProjectileSpeedPercent:
                    context.ProjectileSpeedMultiplier += _value;
                    break;
            }
        }

        public override void Apply(Entity entity) { }

        public override void Remove(Entity entity) { }

        private static SkillTargetMask ToMask(SkillSlot skillSlot) => skillSlot switch
        {
            SkillSlot.Skill1 => SkillTargetMask.Skill1,
            SkillSlot.Skill2 => SkillTargetMask.Skill2,
            SkillSlot.Skill3 => SkillTargetMask.Skill3,
            SkillSlot.Skill4 => SkillTargetMask.Skill4,
            SkillSlot.Passive => SkillTargetMask.Passive,
            _ => SkillTargetMask.None
        };
    }
}
