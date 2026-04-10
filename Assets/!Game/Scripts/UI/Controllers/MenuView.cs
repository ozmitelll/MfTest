using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace _Game.Scripts.Views
{
    public readonly struct CharacterSkillViewData
    {
        public CharacterSkillViewData(string slotLabel, string badge, string name)
        {
            SlotLabel = slotLabel;
            Badge = badge;
            Name = name;
        }

        public string SlotLabel { get; }
        public string Badge { get; }
        public string Name { get; }
    }

    public readonly struct CharacterSelectionViewData
    {
        public CharacterSelectionViewData(
            int characterIndex,
            string badgeText,
            string displayName,
            string role,
            string summary,
            string health,
            string damage,
            string speed,
            string rate,
            string launchLocation,
            CharacterSkillViewData[] skills)
        {
            CharacterIndex = characterIndex;
            BadgeText = badgeText;
            DisplayName = displayName;
            Role = role;
            Summary = summary;
            Health = health;
            Damage = damage;
            Speed = speed;
            Rate = rate;
            LaunchLocation = launchLocation;
            Skills = skills ?? Array.Empty<CharacterSkillViewData>();
        }

        public int CharacterIndex { get; }
        public string BadgeText { get; }
        public string DisplayName { get; }
        public string Role { get; }
        public string Summary { get; }
        public string Health { get; }
        public string Damage { get; }
        public string Speed { get; }
        public string Rate { get; }
        public string LaunchLocation { get; }
        public CharacterSkillViewData[] Skills { get; }
    }

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
        private CharacterSelectionViewData[] _characters = Array.Empty<CharacterSelectionViewData>();
        private int _selectedCharacterIndex;
        
        public event Action OnPlayClicked;
        public event Action OnSettingsClicked;
        public event Action OnQuitClicked;
        public event Action<int> OnCharacterConfirmed;

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

        public void BindCharacters(CharacterSelectionViewData[] characters)
        {
            _characters = characters ?? Array.Empty<CharacterSelectionViewData>();
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
                CharacterSelectionViewData character = _characters[index];
                Button button = new Button(() => SelectCharacter(characterIndex))
                {
                    name = $"character-chip-{characterIndex}"
                };

                button.AddToClassList("character-chip");

                var badge = new Label(character.BadgeText);
                badge.AddToClassList("character-chip__badge");
                button.Add(badge);

                var name = new Label(character.DisplayName);
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
            CharacterSelectionViewData? selectedCharacter = GetSelectedCharacter();

            for (int i = 0; i < _characterButtons.Count; i++)
                _characterButtons[i].EnableInClassList("character-chip--selected", i == _selectedCharacterIndex);

            bool hasSelection = selectedCharacter.HasValue;
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

            CharacterSelectionViewData character = selectedCharacter.Value;
            _previewBadge.text = character.BadgeText;
            _characterName.text = character.DisplayName;
            _characterRole.text = character.Role;
            _characterSummary.text = character.Summary;
            _statHealth.text = character.Health;
            _statDamage.text = character.Damage;
            _statSpeed.text = character.Speed;
            _statRate.text = character.Rate;
            _launchLocation.text = character.LaunchLocation;

            RebuildSkillList(character);
        }

        private void RebuildSkillList(CharacterSelectionViewData character)
        {
            _skillList.Clear();

            foreach (CharacterSkillViewData skill in character.Skills)
                AddSkillTile(skill);
        }

        private void AddSkillTile(CharacterSkillViewData skill)
        {
            var tile = new VisualElement();
            tile.AddToClassList("character-skill");

            var slot = new Label(skill.SlotLabel);
            slot.AddToClassList("character-skill__slot");
            tile.Add(slot);

            var badge = new Label(skill.Badge);
            badge.AddToClassList("character-skill__badge");
            tile.Add(badge);

            var name = new Label(skill.Name);
            name.AddToClassList("character-skill__name");
            tile.Add(name);

            _skillList.Add(tile);
        }

        private void OnStartRunClicked()
        {
            CharacterSelectionViewData? selectedCharacter = GetSelectedCharacter();
            if (selectedCharacter.HasValue)
                OnCharacterConfirmed?.Invoke(selectedCharacter.Value.CharacterIndex);
        }

        private CharacterSelectionViewData? GetSelectedCharacter() =>
            _characters != null && _selectedCharacterIndex >= 0 && _selectedCharacterIndex < _characters.Length
                ? _characters[_selectedCharacterIndex]
                : null;
    }
}