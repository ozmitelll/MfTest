using _Game.Scripts.Gameplay.Entities;

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
    }
}
