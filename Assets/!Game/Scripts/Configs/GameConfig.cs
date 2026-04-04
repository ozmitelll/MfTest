using UnityEngine;
using _Game.Scripts.Gameplay.Systems.Modifications;

namespace _Game.Scripts.Configs
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Modfall/Configs/Game Config", order = 0)]
    public class GameConfig : ScriptableObject
    {
        [Header("Session")]
        [Min(0.1f)] public float TimerTickIntervalSeconds = 1f;
        [Min(1f)] public float DifficultyIncreaseIntervalSeconds = 60f;
        [Min(0f)] public float EnemyDifficultyStatPercentPerLevel = 0.1f;

        [Header("Content")]
        public StageConfig[] stages;
        public ModificationCardDatabase ModificationCardDatabase;
    }
}
