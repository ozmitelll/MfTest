using UnityEngine;
using UnityEngine.UIElements;

namespace _Game.Scripts.UI
{
    // Маркер на UIDocument для полосок здоровья врагов.
    // Sort Order: выше HUD (например 60), ниже курсора.
    [RequireComponent(typeof(UIDocument))]
    public class EnemyBarsLayer : MonoBehaviour
    {
        [SerializeField] private StyleSheet _styleSheet;

        public VisualElement Root { get; private set; }

        private void Awake()
        {
            Root = GetComponent<UIDocument>().rootVisualElement;
            if (_styleSheet != null)
                Root.styleSheets.Add(_styleSheet);
        }
    }
}
