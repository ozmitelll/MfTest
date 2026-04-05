using System;
using System.Collections.Generic;
using _Game.Scripts.Configs;
using _Game.Scripts.Gameplay.Entities.Player;
using _Game.Scripts.Gameplay.Entities.Player.Systems;
using _Game.Scripts.Gameplay.Skills;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Game.Scripts.Views
{
    public class MenuView
    {
        private readonly VisualElement _root;
        private readonly VisualElement _mainMenuPanel;
        private readonly VisualElement _characterSelectPanel;
        private readonly VisualElement _characterStrip;
        private readonly VisualElement _skillList;
        private readonly Label _previewBadge;
        private readonly Label _characterName;
        private readonly Label _characterRole;
        private readonly Label _characterSummary;
        private readonly Label _statHealth;
        private readonly Label _statDamage;
        private readonly Label _statSpeed;
        private readonly Label _statRate;
        private readonly Label _launchLocation;
        private readonly Button _startRunButton;
        private readonly Button _backButton;
        private readonly List<Button> _characterButtons = new();
        private Player[] _characters = Array.Empty<Player>();
        private int _selectedCharacterIndex;
        
        public event Action OnPlayClicked;
        public event Action OnSettingsClicked;
        public event Action OnQuitClicked;
        public event Action<Player> OnCharacterConfirmed;

        public bool HasCharacters => _characters != null && _characters.Length > 0;
        
        public MenuView(VisualElement root)
        {
            _root = root;
            _mainMenuPanel = root.Q("main-menu-panel");
            _characterSelectPanel = root.Q("character-select-panel");
            _characterStrip = root.Q("character-strip");
            _skillList = root.Q("character-skills");
            _previewBadge = root.Q<Label>("character-preview-badge");
            _characterName = root.Q<Label>("character-name");
            _characterRole = root.Q<Label>("character-role");
            _characterSummary = root.Q<Label>("character-summary");
            _statHealth = root.Q<Label>("stat-health-value");
            _statDamage = root.Q<Label>("stat-damage-value");
            _statSpeed = root.Q<Label>("stat-speed-value");
            _statRate = root.Q<Label>("stat-rate-value");
            _launchLocation = root.Q<Label>("launch-location-value");
            _startRunButton = root.Q<Button>("btn-start-run");
            _backButton = root.Q<Button>("btn-back-menu");

            _root.Q<Button>("btn-play").clicked += () => OnPlayClicked?.Invoke();
            _root.Q<Button>("btn-settings").clicked += () => OnSettingsClicked?.Invoke();
            _root.Q<Button>("btn-quit").clicked += () => OnQuitClicked?.Invoke();
            _startRunButton.clicked += OnStartRunClicked;
            _backButton.clicked += ShowMainMenu;

            ShowMainMenu();
        }

        public void BindCharacters(Player[] characters)
        {
            _characters = characters ?? Array.Empty<Player>();
            _selectedCharacterIndex = 0;
            RebuildCharacterStrip();
            RefreshSelection();
        }

        public void ShowMainMenu()
        {
            _mainMenuPanel.style.display = DisplayStyle.Flex;
            _characterSelectPanel.style.display = DisplayStyle.None;
        }

        public void ShowCharacterSelection()
        {
            _mainMenuPanel.style.display = DisplayStyle.None;
            _characterSelectPanel.style.display = DisplayStyle.Flex;
            RefreshSelection();
        }

        public void Show() => _root.style.display = DisplayStyle.Flex;
        public void Hide() => _root.style.display = DisplayStyle.None;
        public void Dispose() { }

        private void RebuildCharacterStrip()
        {
            _characterStrip.Clear();
            _characterButtons.Clear();

            for (int index = 0; index < _characters.Length; index++)
            {
                int characterIndex = index;
                Player playerPrefab = _characters[index];
                Button button = new Button(() => SelectCharacter(characterIndex))
                {
                    name = $"character-chip-{characterIndex}"
                };

                button.AddToClassList("character-chip");

                var badge = new Label(GetBadgeText(playerPrefab));
                badge.AddToClassList("character-chip__badge");
                button.Add(badge);

                var name = new Label(GetDisplayName(playerPrefab));
                name.AddToClassList("character-chip__label");
                button.Add(name);

                _characterStrip.Add(button);
                _characterButtons.Add(button);
            }
        }

        private void SelectCharacter(int index)
        {
            if (_characters == null || index < 0 || index >= _characters.Length)
                return;

            _selectedCharacterIndex = index;
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            Player selectedPlayer = GetSelectedPlayer();

            for (int i = 0; i < _characterButtons.Count; i++)
                _characterButtons[i].EnableInClassList("character-chip--selected", i == _selectedCharacterIndex);

            bool hasSelection = selectedPlayer != null;
            _startRunButton.SetEnabled(hasSelection);

            if (!hasSelection)
            {
                _previewBadge.text = "--";
                _characterName.text = "No Operative";
                _characterRole.text = "Assign a character prefab in MenuBootstrap.";
                _characterSummary.text = "Character selection is empty.";
                _statHealth.text = "--";
                _statDamage.text = "--";
                _statSpeed.text = "--";
                _statRate.text = "--";
                _launchLocation.text = "Unavailable";
                _skillList.Clear();
                return;
            }

            PlayerConfig config = selectedPlayer.config;
            _previewBadge.text = GetBadgeText(selectedPlayer);
            _characterName.text = GetDisplayName(selectedPlayer);
            _characterRole.text = !string.IsNullOrWhiteSpace(config.Archetype) ? config.Archetype : "Operative";
            _characterSummary.text = !string.IsNullOrWhiteSpace(config.Summary)
                ? config.Summary
                : "Deploy into the next run with a custom loadout and combat profile.";
            _statHealth.text = Mathf.RoundToInt(config.MaxHealth).ToString();
            _statDamage.text = Mathf.RoundToInt(config.AttackDamage).ToString();
            _statSpeed.text = config.MoveSpeed.ToString("0.0");
            _statRate.text = config.AttackRate.ToString("0.0");
            _launchLocation.text = "Emporium";

            RebuildSkillList(selectedPlayer);
        }

        private void RebuildSkillList(Player playerPrefab)
        {
            _skillList.Clear();

            PlayerSkillSystem skillSystem = playerPrefab != null ? playerPrefab.SkillSystem : null;
            if (skillSystem == null)
                return;

            AddSkillTile(skillSystem.GetConfiguredSkill(0), "LMB");
            AddSkillTile(skillSystem.GetConfiguredSkill(1), "RMB");
            AddSkillTile(skillSystem.GetConfiguredSkill(2), "SPACE");
            AddSkillTile(skillSystem.GetConfiguredSkill(3), "G");
            AddSkillTile(skillSystem.GetConfiguredPassive(), "PASSIVE");
        }

        private void AddSkillTile(Skill skill, string slotLabel)
        {
            var tile = new VisualElement();
            tile.AddToClassList("character-skill");

            var slot = new Label(slotLabel);
            slot.AddToClassList("character-skill__slot");
            tile.Add(slot);

            var badge = new Label(GetSkillBadge(skill));
            badge.AddToClassList("character-skill__badge");
            tile.Add(badge);

            var name = new Label(skill != null && !string.IsNullOrWhiteSpace(skill.SkillName) ? skill.SkillName : "Empty");
            name.AddToClassList("character-skill__name");
            tile.Add(name);

            _skillList.Add(tile);
        }

        private void OnStartRunClicked()
        {
            Player selectedPlayer = GetSelectedPlayer();
            if (selectedPlayer != null)
                OnCharacterConfirmed?.Invoke(selectedPlayer);
        }

        private Player GetSelectedPlayer() =>
            _characters != null && _selectedCharacterIndex >= 0 && _selectedCharacterIndex < _characters.Length
                ? _characters[_selectedCharacterIndex]
                : null;

        private static string GetDisplayName(Player playerPrefab)
        {
            if (playerPrefab == null)
                return "Unknown";

            return playerPrefab.config != null && !string.IsNullOrWhiteSpace(playerPrefab.config.DisplayName)
                ? playerPrefab.config.DisplayName
                : playerPrefab.name;
        }

        private static string GetBadgeText(Player playerPrefab)
        {
            string displayName = GetDisplayName(playerPrefab);
            return string.IsNullOrWhiteSpace(displayName)
                ? "?"
                : displayName.Substring(0, 1).ToUpperInvariant();
        }

        private static string GetSkillBadge(Skill skill)
        {
            if (skill == null || string.IsNullOrWhiteSpace(skill.SkillName))
                return "-";

            return skill.SkillName.Substring(0, 1).ToUpperInvariant();
        }
    }
}