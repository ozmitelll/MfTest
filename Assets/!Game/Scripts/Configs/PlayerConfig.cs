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

        [Header("Movement")]
        [Min(0f)] public float Gravity = 30f;
        public float GroundedVerticalVelocity = -2f;
        [Min(0f)] public float MaxFallSpeed = 50f;
        [Min(0f)] public float FallRespawnDistance = 8f;
        [Min(0f)] public float RespawnVerticalOffset = 0.25f;
        
        [Header("Character Cursor")]
        public CursorConfig GameplayCursor;
    }
}
