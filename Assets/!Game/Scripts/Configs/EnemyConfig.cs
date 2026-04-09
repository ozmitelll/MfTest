using UnityEngine;

namespace _Game.Scripts.Configs
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Modfall/Configs/Enemy Config")]
    public class EnemyConfig : EntityConfig
    {
        [Header("AI")]
        public float DetectionRange = 10f;
        public float AggroRange     = 15f;
        public EnemyCombatRole CombatRole = EnemyCombatRole.Melee;
        [Range(1, 4)] public int PrimarySkillSlot = 1;
        [Min(0f)] public float RangeTolerance = 0.25f;
        [Min(0f)] public float RetreatDistance = 3f;
        [Min(0f)] public float RetreatDuration = 0.75f;

        [Header("Director")]
        [Min(1f)] public float DirectorCost = 10f;

        [Header("Rewards")]
        public int CoinDrop = 5;
    }
}
