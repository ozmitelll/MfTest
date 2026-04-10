using _Game.Scripts.Core;
using System;
using System.Collections.Generic;
using _Game.Scripts.Gameplay.Systems.Modifications;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Game.Scripts.UI.Controllers
{
    public class HUDController
    {
        private enum ModificationToastPhase
        {
            Hidden,
            Entering,
            Holding,
            Exiting
        }

        private const float ModificationToastEnterDuration = 0.18f;
        private const float ModificationToastHoldDuration = 1f;
        private const float ModificationToastExitDuration = 0.22f;
        private const float ModificationToastVisibleRight = 24f;
        private const float ModificationToastHiddenRight = -380f;

        // ── Health ───────────────────────────────────────────────────────
        private readonly VisualElement _healthFill;
        private readonly Label         _healthText;
        private readonly Label         _timerText;
        private readonly Label         _coinsText;

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
        private readonly VisualElement   _modificationPickupToast;
        private readonly VisualElement   _modificationPickupCard;
        private readonly Label           _modificationPickupBadge;
        private readonly Label           _modificationPickupName;
        private readonly Label           _modificationPickupMeta;
        private readonly Label           _modificationPickupDescription;
        private int                      _activeBossInstanceId = -1;
        private readonly Queue<ModificationCardInstance> _pendingModificationPickups = new();
        private ModificationToastPhase _modificationToastPhase;
        private float _modificationToastTimer;

        public HUDController(VisualElement root)
        {
            _timerText = root.Q<Label>("timer-text");
            _coinsText = root.Q<Label>("coins-text");
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
            _modificationPickupToast = root.Q("modification-pickup-toast");
            _modificationPickupCard = root.Q("modification-pickup-card");
            _modificationPickupBadge = root.Q<Label>("modification-pickup-badge");
            _modificationPickupName = root.Q<Label>("modification-pickup-name");
            _modificationPickupMeta = root.Q<Label>("modification-pickup-meta");
            _modificationPickupDescription = root.Q<Label>("modification-pickup-description");

            if (_bossBar != null)
                _bossBar.style.display = DisplayStyle.None;

            if (_interactionPrompt != null)
                _interactionPrompt.style.display = DisplayStyle.None;

            if (_modificationPickupToast != null)
            {
                _modificationPickupToast.style.display = DisplayStyle.None;
                _modificationPickupToast.style.opacity = 0f;
                _modificationPickupToast.style.right = ModificationToastHiddenRight;
            }
        }

        public void Subscribe()
        {
            EventBus.Subscribe<OnPlayerHealthChangedEvent>(OnHealthChanged);
            EventBus.Subscribe<OnTimerTickEvent>(OnTimerTick);
            EventBus.Subscribe<OnCoinsChangedEvent>(OnCoinsChanged);
            EventBus.Subscribe<OnBossSpawnedEvent>(OnBossSpawned);
            EventBus.Subscribe<OnBossDiedEvent>(OnBossDied);
            EventBus.Subscribe<OnBossHealthChangedEvent>(OnBossHealthChanged);
            EventBus.Subscribe<OnPlayerSkillIconsChangedEvent>(OnSkillIconsChanged);
            EventBus.Subscribe<OnPlayerSkillCooldownsChangedEvent>(OnSkillCooldownsChanged);
            EventBus.Subscribe<OnInteractionPromptChangedEvent>(OnInteractionPromptChanged);
            EventBus.Subscribe<OnModificationCardAddedEvent>(OnModificationCardAdded);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<OnPlayerHealthChangedEvent>(OnHealthChanged);
            EventBus.Unsubscribe<OnTimerTickEvent>(OnTimerTick);
            EventBus.Unsubscribe<OnCoinsChangedEvent>(OnCoinsChanged);
            EventBus.Unsubscribe<OnBossSpawnedEvent>(OnBossSpawned);
            EventBus.Unsubscribe<OnBossDiedEvent>(OnBossDied);
            EventBus.Unsubscribe<OnBossHealthChangedEvent>(OnBossHealthChanged);
            EventBus.Unsubscribe<OnPlayerSkillIconsChangedEvent>(OnSkillIconsChanged);
            EventBus.Unsubscribe<OnPlayerSkillCooldownsChangedEvent>(OnSkillCooldownsChanged);
            EventBus.Unsubscribe<OnInteractionPromptChangedEvent>(OnInteractionPromptChanged);
            EventBus.Unsubscribe<OnModificationCardAddedEvent>(OnModificationCardAdded);
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

        public void SetCoins(int coins)
        {
            if (_coinsText == null)
                return;

            _coinsText.text = coins.ToString();
        }

        // Вызывается каждый кадр из GameBootstrap.Update()
        public void Tick()
        {
            UpdateModificationPickupToast();
        }

        private void OnHealthChanged(OnPlayerHealthChangedEvent evt)
        {
            float pct = evt.Max > 0f ? evt.Current / evt.Max : 0f;
            _healthFill.style.width = Length.Percent(pct * 100f);
            _healthText.text = $"{Mathf.CeilToInt(evt.Current)} / {Mathf.CeilToInt(evt.Max)}";
        }

        private void OnTimerTick(OnTimerTickEvent evt) => SetTimer(evt.Time);

        private void OnCoinsChanged(OnCoinsChangedEvent evt) => SetCoins(evt.Coins);

        private void OnBossSpawned(OnBossSpawnedEvent evt)
        {
            if (_bossBar == null || _bossBarFill == null)
                return;

            _activeBossInstanceId = evt.BossInstanceId;
            if (_bossBarName != null)
                _bossBarName.text = evt.DisplayName;

            _bossBar.style.display = DisplayStyle.Flex;
            ApplyBossHealth(evt.CurrentHealth, evt.MaxHealth);
        }

        private void OnBossDied(OnBossDiedEvent evt)
        {
            if (evt.BossInstanceId == _activeBossInstanceId)
                DetachBoss();
        }

        private void OnBossHealthChanged(OnBossHealthChangedEvent evt)
        {
            if (evt.BossInstanceId != _activeBossInstanceId)
                return;

            if (_bossBarName != null && !string.IsNullOrWhiteSpace(evt.DisplayName))
                _bossBarName.text = evt.DisplayName;

            ApplyBossHealth(evt.Current, evt.Max);
        }

        private void OnSkillIconsChanged(OnPlayerSkillIconsChangedEvent evt)
        {
            ApplySkillIcon(0, evt.Skill0Icon);
            ApplySkillIcon(1, evt.Skill1Icon);
            ApplySkillIcon(2, evt.Skill2Icon);
            ApplySkillIcon(3, evt.Skill3Icon);

            if (_passiveIcon != null && evt.PassiveIcon != null)
                _passiveIcon.style.backgroundImage = new StyleBackground(evt.PassiveIcon);
        }

        private void OnSkillCooldownsChanged(OnPlayerSkillCooldownsChangedEvent evt)
        {
            ApplyCooldown(0, evt.Slot1Ratio, evt.Slot1Remaining);
            ApplyCooldown(1, evt.Slot2Ratio, evt.Slot2Remaining);
            ApplyCooldown(2, evt.Slot3Ratio, evt.Slot3Remaining);
        }

        private void OnInteractionPromptChanged(OnInteractionPromptChangedEvent evt)
        {
            if (_interactionPrompt == null)
                return;

            bool hasPrompt = !string.IsNullOrWhiteSpace(evt.Prompt);
            _interactionPrompt.text = hasPrompt ? evt.Prompt : string.Empty;
            _interactionPrompt.style.display = hasPrompt ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnModificationCardAdded(OnModificationCardAddedEvent evt)
        {
            if (!evt.IsPlayerEntity || evt.Reason != ModificationCardAddReason.LootPickup)
                return;

            if (evt.Card?.Definition == null)
                return;

            _pendingModificationPickups.Enqueue(evt.Card);
        }

        private void DetachBoss()
        {
            _activeBossInstanceId = -1;

            if (_bossBar != null)
                _bossBar.style.display = DisplayStyle.None;
        }

        private void ApplyBossHealth(float current, float max)
        {
            if (_bossBarFill == null)
                return;

            float pct = max > 0f ? current / max : 0f;
            _bossBarFill.style.width = Length.Percent(pct * 100f);

            if (_bossBarValue != null)
                _bossBarValue.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        private void ApplySkillIcon(int index, Sprite icon)
        {
            if (index < 0 || index >= _skillIcons.Length || _skillIcons[index] == null || icon == null)
                return;

            _skillIcons[index].style.backgroundImage = new StyleBackground(icon);
        }

        private void ApplyCooldown(int index, float ratio, float remaining)
        {
            if (index < 0 || index >= _cdOverlays.Length)
                return;

            _cdOverlays[index].style.height = Length.Percent(Mathf.Clamp01(ratio) * 100f);
            _cdTexts[index].text = remaining > 0.1f ? remaining.ToString("F1") : string.Empty;
        }

        private void UpdateModificationPickupToast()
        {
            if (_modificationPickupToast == null)
                return;

            if (_modificationToastPhase == ModificationToastPhase.Hidden)
            {
                if (_pendingModificationPickups.Count > 0)
                    BeginNextModificationToast();

                return;
            }

            _modificationToastTimer += Time.deltaTime;

            switch (_modificationToastPhase)
            {
                case ModificationToastPhase.Entering:
                {
                    float progress = Mathf.Clamp01(_modificationToastTimer / ModificationToastEnterDuration);
                    ApplyModificationToastProgress(progress);

                    if (progress >= 1f)
                    {
                        _modificationToastPhase = ModificationToastPhase.Holding;
                        _modificationToastTimer = 0f;
                    }

                    break;
                }
                case ModificationToastPhase.Holding:
                    ApplyModificationToastProgress(1f);

                    if (_modificationToastTimer >= ModificationToastHoldDuration)
                    {
                        _modificationToastPhase = ModificationToastPhase.Exiting;
                        _modificationToastTimer = 0f;
                    }

                    break;
                case ModificationToastPhase.Exiting:
                {
                    float progress = Mathf.Clamp01(_modificationToastTimer / ModificationToastExitDuration);
                    ApplyModificationToastProgress(1f - progress);

                    if (progress >= 1f)
                    {
                        _modificationToastPhase = ModificationToastPhase.Hidden;
                        _modificationToastTimer = 0f;
                        HideModificationToast();

                        if (_pendingModificationPickups.Count > 0)
                            BeginNextModificationToast();
                    }

                    break;
                }
            }
        }

        private void BeginNextModificationToast()
        {
            if (_pendingModificationPickups.Count == 0)
                return;

            ModificationCardInstance card = _pendingModificationPickups.Dequeue();
            ApplyModificationToastCard(card.Definition);
            _modificationToastPhase = ModificationToastPhase.Entering;
            _modificationToastTimer = 0f;
            _modificationPickupToast.style.display = DisplayStyle.Flex;
            ApplyModificationToastProgress(0f);
        }

        private void HideModificationToast()
        {
            _modificationPickupToast.style.display = DisplayStyle.None;
            _modificationPickupToast.style.opacity = 0f;
            _modificationPickupToast.style.right = ModificationToastHiddenRight;
        }

        private void ApplyModificationToastProgress(float progress)
        {
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            float right = Mathf.Lerp(ModificationToastHiddenRight, ModificationToastVisibleRight, eased);

            _modificationPickupToast.style.right = right;
            _modificationPickupToast.style.opacity = Mathf.Lerp(0.2f, 1f, eased);
        }

        private void ApplyModificationToastCard(ModificationCardDefinition definition)
        {
            if (definition == null)
                return;

            Color accent = GetModificationAccentColor(definition.Rarity);
            Color surface = GetModificationSurfaceColor(definition.Rarity);

            if (_modificationPickupBadge != null)
                _modificationPickupBadge.text = $"{definition.Rarity.ToString().ToUpperInvariant()} DROP";

            if (_modificationPickupName != null)
                _modificationPickupName.text = definition.DisplayName;

            if (_modificationPickupMeta != null)
                _modificationPickupMeta.text = $"{definition.CardType}  •  Rank {definition.Rank}  •  Cost {definition.InstallCost}";

            if (_modificationPickupDescription != null)
                _modificationPickupDescription.text = string.IsNullOrWhiteSpace(definition.Description)
                    ? "No description available."
                    : definition.Description;

            if (_modificationPickupCard != null)
            {
                _modificationPickupCard.style.borderTopColor = accent;
                _modificationPickupCard.style.borderRightColor = accent;
                _modificationPickupCard.style.borderBottomColor = accent;
                _modificationPickupCard.style.borderLeftColor = accent;
                _modificationPickupCard.style.backgroundColor = surface;
            }

            if (_modificationPickupBadge != null)
            {
                _modificationPickupBadge.style.backgroundColor = new Color(accent.r, accent.g, accent.b, 0.16f);
                _modificationPickupBadge.style.color = accent;
            }
        }

        private static Color GetModificationAccentColor(ModificationCardRarity rarity)
        {
            return rarity switch
            {
                ModificationCardRarity.Uncommon => new Color32(116, 201, 123, 255),
                ModificationCardRarity.Rare => new Color32(87, 164, 255, 255),
                ModificationCardRarity.Epic => new Color32(232, 126, 255, 255),
                ModificationCardRarity.Legendary => new Color32(255, 177, 66, 255),
                _ => new Color32(212, 168, 67, 255)
            };
        }

        private static Color GetModificationSurfaceColor(ModificationCardRarity rarity)
        {
            Color accent = GetModificationAccentColor(rarity);
            return new Color(accent.r * 0.12f, accent.g * 0.12f, accent.b * 0.12f, 0.94f);
        }
    }
}
