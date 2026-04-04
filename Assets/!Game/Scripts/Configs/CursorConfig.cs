using UnityEngine;

namespace _Game.Scripts.Configs
{
    [CreateAssetMenu(fileName = "CursorConfig", menuName = "Configs/CursorConfig")]
    public class CursorConfig : ScriptableObject
    {
        public Texture2D Texture;
        public Vector2   Hotspot;
        public Vector2   Size = new Vector2(32f, 32f);
    }
}
