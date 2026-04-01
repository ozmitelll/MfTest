using UnityEngine;

namespace _Game.Scripts.Configs
{
    [CreateAssetMenu(fileName = "StageConfig", menuName = "Modfall/Configs/Stage Config")]
    public class StageConfig : ScriptableObject
    {
        [SerializeField] string _stageName;
        [SerializeField] LevelConfig[] _levels;

        public string StageName => _stageName;
        public LevelConfig[] Levels => _levels;

        public LevelConfig GetRandomLevel()
        {
            if (_levels == null || _levels.Length == 0) return null;
            return _levels[Random.Range(0, _levels.Length)];
        }
    }
}
