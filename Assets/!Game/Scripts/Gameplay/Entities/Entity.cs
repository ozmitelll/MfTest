using _Game.Scripts.Gameplay.Systems.Health;
using _Game.Scripts.Gameplay.Systems.Stats;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Entities
{
    public abstract class Entity : MonoBehaviour
    {
        public HealthSystem HealthSystem;
        public StatsSystem  StatsSystem;
    }
}