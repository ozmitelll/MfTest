using _Game.Scripts.Gameplay.Entities;
using _Game.Scripts.Gameplay.Skills;
using _Game.Scripts.Gameplay.Systems.StatusEffects;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Systems.Modifications
{
    [System.Serializable]
    public class ApplyStatusOnHitModificationEffectDefinition : ModificationEffectDefinition
    {
        [SerializeField] private SkillTargetMask _targets = SkillTargetMask.AllActive;
        [SerializeField] private StatusEffectDefinition _statusEffect;
        [SerializeField] [Min(1)] private int _stacksToApply = 1;
        [SerializeField] [Min(0f)] private float _durationOverride;

        public override bool AppliesOnInstall => false;

        public override bool CanModifySkill(SkillSlot skillSlot) =>
            _statusEffect != null &&
            skillSlot != SkillSlot.None &&
            (_targets & ToMask(skillSlot)) != 0;

        public override void ModifySkillContext(Entity entity, ref SkillContext context)
        {
            if (_statusEffect == null)
                return;

            context.AddOutgoingStatus(new OutgoingStatusRequest
            {
                Definition = _statusEffect,
                DurationOverride = _durationOverride,
                StackCount = _stacksToApply
            });
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
