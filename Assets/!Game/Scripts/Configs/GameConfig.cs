using UnityEngine;

namespace _Game.Scripts.Configs
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Modfall/Configs/Game Config", order = 0)]
    public class GameConfig : ScriptableObject
    {
        public StageConfig[] stages;
    }
}