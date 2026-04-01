namespace _Game.Scripts.Core
{
    public struct OnPortalChargedEvent { }

    public struct OnPlayerDiedEvent { }

    public struct OnSessionEndedEvent { }

    public struct OnTimerTickEvent
    {
        public float Time;
    }

    public struct OnCoinsChangedEvent
    {
        public int Coins;
    }

    public struct OnPlayerHealthChangedEvent
    {
        public float Current;
        public float Max;
    }
}
