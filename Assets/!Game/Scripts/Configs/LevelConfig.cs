using _Game.Scripts.Gameplay;
using UnityEngine;

namespace _Game.Scripts.Configs
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Modfall/Configs/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [SerializeField] Level _levelPrefab;

        public Level LevelPrefab => _levelPrefab;
    }
}
