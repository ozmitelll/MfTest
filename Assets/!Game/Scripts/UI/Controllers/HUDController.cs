using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Entities.Player.Systems;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Game.Scripts.UI.Controllers
{
    public class HUDController
    {
        // ── Health ───────────────────────────────────────────────────────
        private readonly VisualElement _healthFill;
        private readonly Label         _healthText;
        private readonly Label         _timerText;

        // ── Skills ───────────────────────────────────────────────────────
        private readonly VisualElement[] _skillIcons  = new VisualElement[4];
        private readonly VisualElement[] _cdOverlays  = new VisualElement[3]; // слоты 1,2,3
        private readonly Label[]         _cdTexts     = new Label[3];
        private readonly VisualElement   _passiveIcon;
        private PlayerSkillSystem        _skillSystem;

        public HUDController(VisualElement root)
        {
            _timerText = root.Q<Label>("timer-text");
            _healthFill = root.Q("health-bar-fill");
            _healthText = root.Q<Label>("health-text");

            for (int i = 0; i < 4; i++)
                _skillIcons[i] = root.Q($"skill-icon-{i}");

            for (int i = 0; i < 3; i++)
            {
                _cdOverlays[i] = root.Q($"skill-cd-overlay-{i + 1}");
                _cdTexts[i]    = root.Q<Label>($"skill-cd-text-{i + 1}");
            }

            _passiveIcon = root.Q("skill-icon-passive");
        }

        public void SetSkillSystem(PlayerSkillSystem skillSystem)
        {
            _skillSystem = skillSystem;

            for (int i = 0; i < 4; i++)
            {
                var icon = skillSystem.GetSkillIcon(i);
                if (icon != null)
                    _skillIcons[i].style.backgroundImage = new StyleBackground(icon);
            }

            var passive = skillSystem.GetPassiveIcon();
            if (passive != null)
                _passiveIcon.style.backgroundImage = new StyleBackground(passive);
        }

        public void Subscribe()
        {
            EventBus.Subscribe<OnPlayerHealthChangedEvent>(OnHealthChanged);
            EventBus.Subscribe<OnTimerTickEvent>(OnTimerTick);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<OnPlayerHealthChangedEvent>(OnHealthChanged);
            EventBus.Unsubscribe<OnTimerTickEvent>(OnTimerTick);
        }

        public void SetTimer(float timeSeconds)
        {
            if (_timerText == null)
                return;

            TimeSpan elapsed = TimeSpan.FromSeconds(Mathf.Max(0f, timeSeconds));
            _timerText.text = elapsed.TotalHours >= 1d
                ? $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}"
                : $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        }

        // Вызывается каждый кадр из GameBootstrap.Update()
        public void Tick()
        {
            if (_skillSystem == null) return;

            for (int i = 0; i < 3; i++)
            {
                float ratio     = _skillSystem.GetCooldownRatio(i + 1);
                float remaining = _skillSystem.GetRemainingCooldown(i + 1);

                _cdOverlays[i].style.height = Length.Percent(ratio * 100f);
                _cdTexts[i].text = remaining > 0.1f ? remaining.ToString("F1") : "";
            }
        }

        private void OnHealthChanged(OnPlayerHealthChangedEvent evt)
        {
            float pct = evt.Max > 0f ? evt.Current / evt.Max : 0f;
            _healthFill.style.width = Length.Percent(pct * 100f);
            _healthText.text = $"{Mathf.CeilToInt(evt.Current)} / {Mathf.CeilToInt(evt.Max)}";
        }

        private void OnTimerTick(OnTimerTickEvent evt) => SetTimer(evt.Time);
    }
}
