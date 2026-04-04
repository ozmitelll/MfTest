using _Game.Scripts.Gameplay.Entities;
using _Game.Scripts.Gameplay.Systems.Stats;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Systems.Modifications
{
    [CreateAssetMenu(fileName = "StatModificationEffect", menuName = "Modfall/Modifications/Stat Effect")]
    public class StatModificationEffectDefinition : ModificationEffectDefinition
    {
        [SerializeField] private StatType _statType;
        [SerializeField] private StatModifierMode _mode;
        [SerializeField] private float _value;

        public override void Apply(Entity entity)
        {
            Stat stat = entity.StatsSystem.GetStat(_statType);
            if (_mode == StatModifierMode.Flat)
            {
                stat.AddFlat(_value);
                return;
            }

            stat.AddPercent(_value);
        }

        public override void Remove(Entity entity)
        {
            Stat stat = entity.StatsSystem.GetStat(_statType);
            if (_mode == StatModifierMode.Flat)
            {
                stat.RemoveFlat(_value);
                return;
            }

            stat.RemovePercent(_value);
        }
    }
}
