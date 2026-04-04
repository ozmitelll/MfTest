using _Game.Scripts.Gameplay.Systems.Combat;
using _Game.Scripts.Gameplay.Systems.Modifications;
using UnityEngine;

namespace _Game.Scripts.Configs
{
    public abstract class EntityConfig : ScriptableObject
    {
        [Header("Health")]
        public float MaxHealth = 100f;

        [Header("Movement")]
        public float MoveSpeed = 5f;

        [Header("Defense")]
        public float Armor                = 0f;
        public float MagicResistance      = 0f;
        public float ElementalResistance  = 0f;

        [Header("Combat")]
        public float      AttackDamage     = 10f;
        public float      AttackRange      = 2f;
        public float      AttackRate       = 1f;    // attacks per second
        public DamageType AttackDamageType = DamageType.Pure;

        [Header("Modifications")]
        [Min(0)] public int ModificationCapacity = 10;
        public StartingModificationEntry[] StartingModifications;
    }
}
