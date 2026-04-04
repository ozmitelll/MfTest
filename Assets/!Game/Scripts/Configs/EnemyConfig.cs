using UnityEngine;

namespace _Game.Scripts.Configs
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Modfall/Configs/Enemy Config")]
    public class EnemyConfig : EntityConfig
    {
        [Header("AI")]
        public float DetectionRange = 10f;
        public float AggroRange     = 15f;

        [Header("Director")]
        [Min(1f)] public float DirectorCost = 10f;

        [Header("Rewards")]
        public int CoinDrop = 5;
    }
}
