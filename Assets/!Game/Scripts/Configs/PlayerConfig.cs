using UnityEngine;

namespace _Game.Scripts.Configs
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Modfall/Configs/Player Config")]
    public class PlayerConfig : EntityConfig
    {
        
        [Header("Character Cursor")]
        public CursorConfig GameplayCursor;
    }
}
