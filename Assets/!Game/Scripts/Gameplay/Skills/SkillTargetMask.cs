using System;

namespace _Game.Scripts.Gameplay.Skills
{
    [Flags]
    public enum SkillTargetMask
    {
        None = 0,
        Skill1 = 1 << 0,
        Skill2 = 1 << 1,
        Skill3 = 1 << 2,
        Skill4 = 1 << 3,
        Passive = 1 << 4,
        AllActive = Skill1 | Skill2 | Skill3 | Skill4,
        All = AllActive | Passive
    }
}
