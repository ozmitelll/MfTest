namespace _Game.Scripts.Gameplay.Systems.Combat
{
    public enum DamageType
    {
        Pure,       // ignores armor
        Magical,    // reduced by MagicResistance
        Elemental   // reduced by ElementalResistance, triggers status effects
    }
}
