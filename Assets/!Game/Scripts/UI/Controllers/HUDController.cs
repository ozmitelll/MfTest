using _Game.Scripts.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Game.Scripts.UI.Controllers
{
    public class HUDController
    {
        private readonly VisualElement _healthFill;
        private readonly Label         _healthText;

        public HUDController(VisualElement root)
        {
            _healthFill = root.Q("health-bar-fill");
            _healthText = root.Q<Label>("health-text");
        }

        public void Subscribe()   => EventBus.Subscribe<OnPlayerHealthChangedEvent>(OnHealthChanged);
        public void Dispose()     => EventBus.Unsubscribe<OnPlayerHealthChangedEvent>(OnHealthChanged);

        private void OnHealthChanged(OnPlayerHealthChangedEvent evt)
        {
            float pct = evt.Max > 0f ? evt.Current / evt.Max : 0f;
            _healthFill.style.width = Length.Percent(pct * 100f);
            _healthText.text = $"{Mathf.CeilToInt(evt.Current)} / {Mathf.CeilToInt(evt.Max)}";
        }
    }
}
