namespace _Game.Scripts.Core
{
    // --- Player ---
    public struct OnPlayerHealthChangedEvent
    {
        public float Current;
        public float Max;
    }

    public struct OnPlayerDiedEvent { }

    // --- Enemy ---
    public struct OnEnemyDiedEvent
    {
        public _Game.Scripts.Gameplay.Entities.Enemy.Enemy Enemy;
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
}
