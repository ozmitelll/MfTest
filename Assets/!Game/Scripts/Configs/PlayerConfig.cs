using UnityEngine;

namespace _Game.Scripts.Configs
{
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Modfall/Configs/Player Config")]
    public class PlayerConfig : EntityConfig
    {
        [Header("Presentation")]
        public string DisplayName = "Operative";
        public string Archetype = "Hybrid";
        [TextArea] public string Summary;
        
        [Header("Character Cursor")]
        public CursorConfig GameplayCursor;
    }
}
