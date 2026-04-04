using UnityEngine;

namespace _Game.Scripts.Gameplay.Skills
{
    public abstract class Skill : ScriptableObject
    {
        [Header("Info")]
        public string SkillName;
        public Sprite Icon;
        [TextArea] public string Description;
    }
}
