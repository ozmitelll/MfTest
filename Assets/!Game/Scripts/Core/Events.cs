namespace _Game.Scripts.Core
{
    // --- Player ---
    public struct OnPlayerHealthChangedEvent
    {
        public float Current;
        public float Max;
    }

    public struct OnPlayerDiedEvent { }

    public struct OnPlayerSkillIconsChangedEvent
    {
        public UnityEngine.Sprite Skill0Icon;
        public UnityEngine.Sprite Skill1Icon;
        public UnityEngine.Sprite Skill2Icon;
        public UnityEngine.Sprite Skill3Icon;
        public UnityEngine.Sprite PassiveIcon;
    }

    public struct OnPlayerSkillCooldownsChangedEvent
    {
        public float Slot1Ratio;
        public float Slot2Ratio;
        public float Slot3Ratio;
        public float Slot1Remaining;
        public float Slot2Remaining;
        public float Slot3Remaining;
    }

    public struct OnInteractionPromptChangedEvent
    {
        public string Prompt;
    }

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
        public int BossInstanceId;
    }

    public struct OnBossSpawnedEvent
    {
        public _Game.Scripts.Gameplay.Entities.Bosses.Boss Boss;
        public int BossInstanceId;
        public string DisplayName;
        public float CurrentHealth;
        public float MaxHealth;
    }

    public struct OnBossHealthChangedEvent
    {
        public int BossInstanceId;
        public string DisplayName;
        public float Current;
        public float Max;
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
        public bool IsPlayerEntity;
    }

    public struct OnModificationInstalledEvent
    {
        public _Game.Scripts.Gameplay.Entities.Entity Entity;
        public _Game.Scripts.Gameplay.Systems.Modifications.ModificationCardInstance Card;
        public int SlotIndex;
        public int UsedCapacity;
        public int Capacity;
        public bool IsPlayerEntity;
    }

    public struct OnModificationRemovedEvent
    {
        public _Game.Scripts.Gameplay.Entities.Entity Entity;
        public _Game.Scripts.Gameplay.Systems.Modifications.ModificationCardInstance Card;
        public int SlotIndex;
        public int UsedCapacity;
        public int Capacity;
        public bool IsPlayerEntity;
    }
}
