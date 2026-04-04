using _Game.Scripts.Configs;
using _Game.Scripts.Gameplay.Systems.Combat;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Systems.Stats
{
    public class StatsSystem : MonoBehaviour
    {
        public Stat MaxHealth          { get; private set; }
        public Stat MoveSpeed          { get; private set; }
        public Stat Armor              { get; private set; }
        public Stat MagicResistance    { get; private set; }
        public Stat ElementalResistance { get; private set; }
        public Stat AttackDamage       { get; private set; }
        public Stat AttackRange        { get; private set; }
        public Stat AttackRate         { get; private set; }

        public DamageType AttackDamageType { get; private set; }

        public void Initialize(EntityConfig config)
        {
            MaxHealth           = new Stat(config.MaxHealth);
            MoveSpeed           = new Stat(config.MoveSpeed);
            Armor               = new Stat(config.Armor);
            MagicResistance     = new Stat(config.MagicResistance);
            ElementalResistance = new Stat(config.ElementalResistance);
            AttackDamage        = new Stat(config.AttackDamage);
            AttackRange         = new Stat(config.AttackRange);
            AttackRate          = new Stat(config.AttackRate);
            AttackDamageType    = config.AttackDamageType;
        }
    }
}
