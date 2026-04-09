using _Game.Scripts.Gameplay.Entities;
using _Game.Scripts.Gameplay.Entities.Enemy;
using _Game.Scripts.Gameplay.Entities.Player;

namespace _Game.Scripts.Gameplay.Systems.Combat
{
    public static class EntityCombatUtility
    {
        public static bool AreHostile(Entity source, Entity target)
        {
            if (source == null || target == null || source == target)
                return false;

            bool sourceIsPlayer = source is Player;
            bool targetIsPlayer = target is Player;
            bool sourceIsEnemy = source is Enemy;
            bool targetIsEnemy = target is Enemy;

            return (sourceIsPlayer && targetIsEnemy) || (sourceIsEnemy && targetIsPlayer);
        }
    }
}