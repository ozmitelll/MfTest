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
    // OnEnemyDiedEvent будет добавлен когда появится класс Enemy
    // public struct OnEnemyDiedEvent { public Enemy Enemy; }

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
}
