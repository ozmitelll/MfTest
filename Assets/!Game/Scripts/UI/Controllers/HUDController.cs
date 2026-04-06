using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Entities.Player.Systems;
using System;
using _Game.Scripts.Gameplay.Entities.Bosses;
using _Game.Scripts.Gameplay.Entities.Player;
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
        private readonly VisualElement   _bossBar;
        private readonly VisualElement   _bossBarFill;
        private readonly Label           _bossBarName;
        private readonly Label           _bossBarValue;
        private readonly Label           _interactionPrompt;
        private Boss                     _activeBoss;
        private Player                   _player;
        private InteractionSystem        _interactionSystem;
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
            _bossBar = root.Q("boss-health-bar");
            _bossBarFill = root.Q("boss-health-bar__fill");
            _bossBarName = root.Q<Label>("boss-health-bar__name");
            _bossBarValue = root.Q<Label>("boss-health-bar__value");
            _interactionPrompt = root.Q<Label>("interaction-prompt");

            if (_bossBar != null)
                _bossBar.style.display = DisplayStyle.None;

            if (_interactionPrompt != null)
                _interactionPrompt.style.display = DisplayStyle.None;
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

        public void SetInteractionContext(Player player, InteractionSystem interactionSystem)
        {
            _player = player;
            _interactionSystem = interactionSystem;
        }

        public void Subscribe()
        {
            EventBus.Subscribe<OnPlayerHealthChangedEvent>(OnHealthChanged);
            EventBus.Subscribe<OnTimerTickEvent>(OnTimerTick);
            EventBus.Subscribe<OnBossSpawnedEvent>(OnBossSpawned);
            EventBus.Subscribe<OnBossDiedEvent>(OnBossDied);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<OnPlayerHealthChangedEvent>(OnHealthChanged);
            EventBus.Unsubscribe<OnTimerTickEvent>(OnTimerTick);
            EventBus.Unsubscribe<OnBossSpawnedEvent>(OnBossSpawned);
            EventBus.Unsubscribe<OnBossDiedEvent>(OnBossDied);
            DetachBoss();
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
            UpdateInteractionPrompt();

            if (_skillSystem == null)
                return;

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

        private void OnBossSpawned(OnBossSpawnedEvent evt) => AttachBoss(evt.Boss);

        private void OnBossDied(OnBossDiedEvent evt)
        {
            if (evt.Boss == _activeBoss)
                DetachBoss();
        }

        private void AttachBoss(Boss boss)
        {
            DetachBoss();

            if (boss == null || _bossBar == null || _bossBarFill == null)
                return;

            _activeBoss = boss;
            _activeBoss.HealthSystem.OnHealthChanged += OnBossHealthChanged;
            _bossBarName.text = GetBossDisplayName(_activeBoss);
            _bossBar.style.display = DisplayStyle.Flex;
            OnBossHealthChanged(_activeBoss.HealthSystem.CurrentHealth, _activeBoss.HealthSystem.MaxHealth);
        }

        private void DetachBoss()
        {
            if (_activeBoss != null)
                _activeBoss.HealthSystem.OnHealthChanged -= OnBossHealthChanged;

            _activeBoss = null;

            if (_bossBar != null)
                _bossBar.style.display = DisplayStyle.None;
        }

        private void OnBossHealthChanged(float current, float max)
        {
            if (_bossBarFill == null)
                return;

            float pct = max > 0f ? current / max : 0f;
            _bossBarFill.style.width = Length.Percent(pct * 100f);

            if (_bossBarValue != null)
                _bossBarValue.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        private static string GetBossDisplayName(Boss boss)
        {
            string rawName = boss.Config != null ? boss.Config.name : boss.name;
            return rawName.Replace("(Clone)", string.Empty).Trim();
        }

        private void UpdateInteractionPrompt()
        {
            if (_interactionPrompt == null)
                return;

            string prompt = string.Empty;
            if (_player != null && _interactionSystem?.CurrentInteractable?.CanInteract(_player) == true)
                prompt = _interactionSystem.CurrentInteractable.InteractionPrompt;

            bool hasPrompt = !string.IsNullOrWhiteSpace(prompt);
            _interactionPrompt.text = hasPrompt ? prompt : string.Empty;
            _interactionPrompt.style.display = hasPrompt ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
