namespace _Game.Scripts.Gameplay.Systems.Stats
{
    public class Stat
    {
        public float Base         { get; }
        public float FlatBonus    { get; private set; }
        public float PercentBonus { get; private set; }  // 0.1 = +10%

        // (Base + flat) * (1 + percent)
        public float Value => (Base + FlatBonus) * (1f + PercentBonus);

        public Stat(float baseValue) => Base = baseValue;

        public void AddFlat(float amount)       => FlatBonus    += amount;
        public void RemoveFlat(float amount)    => FlatBonus    -= amount;
        public void AddPercent(float amount)    => PercentBonus += amount;
        public void RemovePercent(float amount) => PercentBonus -= amount;
    }
}
