using _Game.Scripts.Gameplay.Entities;
using _Game.Scripts.Gameplay.Systems.Combat;

namespace _Game.Scripts.Gameplay.Systems.StatusEffects
{
    public struct StatusEffectApplicationPayload
    {
        public StatusEffectDefinition Definition;
        public Entity Source;
        public float Duration;
        public int StackCount;
        public float SourceDamageSnapshot;
        public float SourceAttackRateSnapshot;
        public DamageType DamageType;
    }
}
