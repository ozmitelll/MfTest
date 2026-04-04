using _Game.Scripts.Configs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace _Game.Scripts.Core
{
    [RequireComponent(typeof(UIDocument))]
    public class CursorController : MonoBehaviour
    {
        [SerializeField] private CursorConfig _defaultConfig;

        private VisualElement _cursor;
        private Vector2       _hotspot;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Cursor.visible = false;
        }

        // UIDocument строит дерево в OnEnable — Start гарантированно после него
        private void Start()
        {
            _cursor = GetComponent<UIDocument>().rootVisualElement.Q("cursor");

            if (_cursor == null)
            {
                Debug.LogError("[CursorController] VisualElement 'cursor' not found. Check Cursor.uxml.");
                return;
            }

            Apply(_defaultConfig);
        }

        private void LateUpdate()
        {
            if (_cursor == null) return;

            Vector2 mouse = Mouse.current.position.ReadValue();
            _cursor.style.left = mouse.x         - _hotspot.x;
            _cursor.style.top  = Screen.height - mouse.y - _hotspot.y;
        }

        public void Apply(CursorConfig config)
        {
            if (config == null) return;
            _hotspot                        = config.Hotspot;
            _cursor.style.width             = config.Size.x;
            _cursor.style.height            = config.Size.y;
            _cursor.style.backgroundImage   = new StyleBackground(config.Texture);
        }
    }
}
