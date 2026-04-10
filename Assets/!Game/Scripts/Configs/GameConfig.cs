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
        [Min(0f)] public float EnemyCoinRewardPercentPerDifficulty = 0.15f;

        [Header("Director")]
        [Min(0f)] public float DirectorStartingCredits = 18f;
        [Min(0.1f)] public float DirectorCreditsPerSecond = 6f;
        [Min(0f)] public float DirectorCreditsPerSecondPerDifficulty = 1.5f;
        [Min(0.1f)] public float DirectorSpawnIntervalSeconds = 1f;
        [Min(1)] public int DirectorMinWaveSize = 2;
        [Min(1)] public int DirectorMaxWaveSize = 4;
        [Min(0.5f)] public float DirectorWaveSpreadRadius = 4f;
        [Min(1)] public int DirectorBaseMaxAliveEnemies = 5;
        [Min(0)] public int DirectorMaxAliveEnemiesPerDifficulty = 1;
        [Min(1f)] public float DirectorPlayerStrengthThreshold = 1.2f;
        [Min(0f)] public float DirectorExpectedPlayerPowerPerDifficulty = 0.15f;

        [Header("Content")]
        public StageConfig[] stages;
        public ModificationCardDatabase ModificationCardDatabase;
    }
}
