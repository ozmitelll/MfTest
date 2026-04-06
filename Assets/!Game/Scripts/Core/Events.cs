namespace _Game.Scripts.Core
{
    // --- Player ---
    public struct OnPlayerHealthChangedEvent
    {
        public float Current;
        public float Max;
    }

    public struct OnPlayerDiedEvent { }

    public struct OnEntityDamagedEvent
    {
        public _Game.Scripts.Gameplay.Entities.Entity Target;
        public float Damage;
        public _Game.Scripts.Gameplay.Systems.Combat.DamageType DamageType;
        public bool IsStatusDamage;
        public _Game.Scripts.Gameplay.Systems.StatusEffects.StatusEffectDefinition StatusEffect;
    }

    // --- Enemy ---
    public struct OnEnemyDiedEvent
    {
        public _Game.Scripts.Gameplay.Entities.Enemy.Enemy Enemy;
    }

    public struct OnBossDiedEvent
    {
        public _Game.Scripts.Gameplay.Entities.Bosses.Boss Boss;
    }

    public struct OnBossSpawnedEvent
    {
        public _Game.Scripts.Gameplay.Entities.Bosses.Boss Boss;
    }

    // --- Session ---
    public struct OnSessionEndedEvent { }

    public struct OnTimerTickEvent
    {
        public float Time;
    }

    // --- Economy ---
    public struct OnCoinsChangedEvent
    {
        public int Coins;
    }

    // --- Level ---
    public struct OnPortalChargedEvent { }

    public struct OnChestOpenedEvent
    {
        public _Game.Scripts.Gameplay.Interactables.Chest Chest;
    }

    // --- Modifications ---
    public struct OnModificationCardAddedEvent
    {
        public _Game.Scripts.Gameplay.Entities.Entity Entity;
        public _Game.Scripts.Gameplay.Systems.Modifications.ModificationCardInstance Card;
        public int TotalCards;
        public _Game.Scripts.Gameplay.Systems.Modifications.ModificationCardAddReason Reason;
    }

    public struct OnModificationInstalledEvent
    {
        public _Game.Scripts.Gameplay.Entities.Entity Entity;
        public _Game.Scripts.Gameplay.Systems.Modifications.ModificationCardInstance Card;
        public int SlotIndex;
        public int UsedCapacity;
        public int Capacity;
    }

    public struct OnModificationRemovedEvent
    {
        public _Game.Scripts.Gameplay.Entities.Entity Entity;
        public _Game.Scripts.Gameplay.Systems.Modifications.ModificationCardInstance Card;
        public int SlotIndex;
        public int UsedCapacity;
        public int Capacity;
    }
}
