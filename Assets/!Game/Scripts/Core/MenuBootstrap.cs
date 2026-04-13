using System;
using System.Collections.Generic;
using _Game.Scripts.Services;
using _Game.Scripts.Gameplay.Entities.Player;
using _Game.Scripts.Gameplay.Entities.Player.Systems;
using _Game.Scripts.Gameplay.Skills;
using _Game.Scripts.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Game.Scripts.Core
{
    public class MenuBootstrap : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        [SerializeField] private VisualTreeAsset _characterSelectPanelAsset;
        [SerializeField] private VisualTreeAsset _menuPanelAsset;
        [SerializeField] private Transform _characterPreviewSpawnPoint;
        [SerializeField] private Player[] _availablePlayers;
        
        private MenuView _menuView;
        private Button _startGameButton;
        private Button _exitButton;
        private Button _wakeUpButton;
        private Button _backButton;
        private VisualElement _charactersGrid;
        private VisualElement _characterSkills;
        private VisualElement _characterStats;
        private Label _characterNameLabel;
        private readonly List<VisualElement> _standaloneCharacterSlots = new();
        private CharacterSelectionViewData[] _standaloneCharacters = Array.Empty<CharacterSelectionViewData>();
        private Player _previewCharacterInstance;
        private int _selectedStandaloneCharacterIndex;
        private int _previewCharacterIndex = -1;
        // private SettingsView _settingsView;
        // private CharacterSelectView _characterSelectView;

        private void Awake()
        {
            EnsureMenuServicesRegistered();
        }

        private void Start()
        {
            var root = _document.rootVisualElement;

            if (IsCombinedMenuDocument(root))
            {
                InitializeCombinedMenu(root);
                return;
            }

            InitializeStandaloneMenu(root);
        }

        private void InitializeCombinedMenu(VisualElement root)
        {
            _menuView = new MenuView(root);
            _menuView.BindCharacters(BuildCharacterViewData());

            _menuView.OnPlayClicked += OnPlayClicked;
            _menuView.OnCharacterConfirmed += OnCharacterConfirmed;
            _menuView.OnQuitClicked += OnQuitClicked;
        }

        private void InitializeStandaloneMenu(VisualElement root)
        {
            if (root.Q("charactersGrid") != null)
            {
                InitializeStandaloneCharacterSelection(root);
                return;
            }

            _startGameButton = root.Q<Button>("btn-start-game") ?? root.Q<Button>("menuButton");
            _exitButton = root.Q<Button>("btn-exit");

            if (_startGameButton == null)
            {
                Debug.LogWarning($"[{nameof(MenuBootstrap)}] Start button was not found in the current menu document.", this);
                return;
            }

            _startGameButton.clicked += ShowCharacterSelectionPanel;

            if (_exitButton != null)
                _exitButton.clicked += OnQuitClicked;
        }

        private void InitializeStandaloneCharacterSelection(VisualElement root)
        {
            _standaloneCharacters = BuildCharacterViewData();
            _selectedStandaloneCharacterIndex = Mathf.Clamp(_selectedStandaloneCharacterIndex, 0, Mathf.Max(0, _standaloneCharacters.Length - 1));

            _charactersGrid = root.Q("charactersGrid");
            _characterSkills = root.Q("character_skills");
            _characterStats = root.Q("character_stats");
            _characterNameLabel = root.Q<Label>("characterNameLabel");
            _wakeUpButton = root.Q<Button>("startButton");
            _backButton = root.Q<Button>("backButton");

            if (_wakeUpButton != null)
                _wakeUpButton.clicked += StartSelectedCharacter;

            if (_backButton != null)
                _backButton.clicked += ShowMainMenuPanel;

            RebuildStandaloneCharacterSlots();
            RefreshStandaloneCharacterSelection();
        }

        private static bool IsCombinedMenuDocument(VisualElement root)
        {
            return root.Q("main-menu-panel") != null && root.Q("character-select-panel") != null;
        }

        private void ShowCharacterSelectionPanel()
        {
            if (_characterSelectPanelAsset == null)
            {
                Debug.LogWarning($"[{nameof(MenuBootstrap)}] Character select panel asset is not assigned.", this);
                return;
            }

            VisualElement root = _document.rootVisualElement;
            root.Clear();
            _characterSelectPanelAsset.CloneTree(root);
            InitializeStandaloneCharacterSelection(root);
        }

        private void ShowMainMenuPanel()
        {
            DestroyPreviewCharacter();

            if (_menuPanelAsset == null)
            {
                Debug.LogWarning($"[{nameof(MenuBootstrap)}] Menu panel asset is not assigned.", this);
                return;
            }

            VisualElement root = _document.rootVisualElement;
            root.Clear();
            _menuPanelAsset.CloneTree(root);
            InitializeStandaloneMenu(root);
        }

        private void StartSelectedCharacter()
        {
            if (_standaloneCharacters == null || _standaloneCharacters.Length == 0)
                return;

            OnCharacterConfirmed(_selectedStandaloneCharacterIndex);
        }

        private void RebuildStandaloneCharacterSlots()
        {
            _standaloneCharacterSlots.Clear();
            if (_charactersGrid == null)
                return;

            while (_charactersGrid.childCount < _standaloneCharacters.Length)
            {
                var extraSlot = new VisualElement();
                extraSlot.AddToClassList("character__slot");
                _charactersGrid.Add(extraSlot);
            }

            for (int i = 0; i < _charactersGrid.childCount; i++)
            {
                VisualElement slot = _charactersGrid.ElementAt(i);
                bool hasCharacter = i < _standaloneCharacters.Length;
                slot.style.display = hasCharacter ? DisplayStyle.Flex : DisplayStyle.None;

                if (!hasCharacter)
                    continue;

                int slotIndex = i;
                ConfigureStandaloneCharacterSlot(slot, _standaloneCharacters[i]);
                slot.AddManipulator(new Clickable(() => SelectStandaloneCharacter(slotIndex)));
                _standaloneCharacterSlots.Add(slot);
            }
        }

        private void ConfigureStandaloneCharacterSlot(VisualElement slot, CharacterSelectionViewData character)
        {
            slot.Clear();
            slot.tooltip = character.DisplayName;

            var badge = new Label(character.BadgeText);
            badge.AddToClassList("character__slot-label");
            slot.Add(badge);
        }

        private void SelectStandaloneCharacter(int characterIndex)
        {
            if (_standaloneCharacters == null || characterIndex < 0 || characterIndex >= _standaloneCharacters.Length)
                return;

            _selectedStandaloneCharacterIndex = characterIndex;
            RefreshStandaloneCharacterSelection();
        }

        private void RefreshStandaloneCharacterSelection()
        {
            bool hasSelection = _standaloneCharacters != null && _standaloneCharacters.Length > 0;
            _wakeUpButton?.SetEnabled(hasSelection);

            for (int i = 0; i < _standaloneCharacterSlots.Count; i++)
                _standaloneCharacterSlots[i].EnableInClassList("character__slot--selected", i == _selectedStandaloneCharacterIndex);

            if (!hasSelection)
            {
                if (_characterNameLabel != null)
                    _characterNameLabel.text = "NO OPERATIVE";

                _characterStats?.Clear();
                ClearStandaloneSkillTiles();
                DestroyPreviewCharacter();
                return;
            }

            CharacterSelectionViewData character = _standaloneCharacters[_selectedStandaloneCharacterIndex];

            if (_characterNameLabel != null)
                _characterNameLabel.text = character.DisplayName;

            RebuildStandaloneStats(character);
            RefreshStandaloneSkillTiles(character);
            SpawnPreviewCharacter(character.CharacterIndex);
        }

        private void RebuildStandaloneStats(CharacterSelectionViewData character)
        {
            if (_characterStats == null)
                return;

            _characterStats.Clear();
            AddStatRow("ROLE", character.Role);
            AddStatRow("HP", character.Health);
            AddStatRow("DMG", character.Damage);
            AddStatRow("MOVE", character.Speed);
            AddStatRow("RATE", character.Rate);
        }

        private void AddStatRow(string label, string value)
        {
            if (_characterStats == null)
                return;

            var row = new VisualElement();
            row.AddToClassList("character__stat-row");

            var labelElement = new Label(label);
            labelElement.AddToClassList("character__stat-label");
            row.Add(labelElement);

            var valueElement = new Label(value);
            valueElement.AddToClassList("character__stat-value");
            row.Add(valueElement);

            _characterStats.Add(row);
        }

        private void RefreshStandaloneSkillTiles(CharacterSelectionViewData character)
        {
            if (_characterSkills == null)
                return;

            for (int i = 0; i < _characterSkills.childCount; i++)
            {
                VisualElement slot = _characterSkills.ElementAt(i);
                bool hasSkill = character.Skills != null && i < character.Skills.Length;
                slot.Clear();
                slot.style.opacity = hasSkill ? 1f : 0.35f;

                if (!hasSkill)
                    continue;

                CharacterSkillViewData skill = character.Skills[i];
                slot.tooltip = skill.Name;

                var label = new Label(skill.Badge);
                label.AddToClassList("character__skill-label");
                slot.Add(label);
            }
        }

        private void ClearStandaloneSkillTiles()
        {
            if (_characterSkills == null)
                return;

            for (int i = 0; i < _characterSkills.childCount; i++)
            {
                VisualElement slot = _characterSkills.ElementAt(i);
                slot.Clear();
                slot.style.opacity = 0.35f;
            }
        }

        private void SpawnPreviewCharacter(int characterIndex)
        {
            if (_previewCharacterInstance != null && _previewCharacterIndex == characterIndex)
                return;

            DestroyPreviewCharacter();

            if (_availablePlayers == null || characterIndex < 0 || characterIndex >= _availablePlayers.Length)
                return;

            Player playerPrefab = _availablePlayers[characterIndex];
            if (playerPrefab == null)
                return;

            Vector3 spawnPosition = _characterPreviewSpawnPoint != null
                ? _characterPreviewSpawnPoint.position
                : GetFallbackPreviewPosition();
            Quaternion spawnRotation = _characterPreviewSpawnPoint != null
                ? _characterPreviewSpawnPoint.rotation
                : Quaternion.identity;

            _previewCharacterInstance = Instantiate(playerPrefab, spawnPosition, spawnRotation);
            _previewCharacterIndex = characterIndex;
            PreparePreviewCharacter(_previewCharacterInstance);
        }

        private static void PreparePreviewCharacter(Player previewCharacter)
        {
            if (previewCharacter == null)
                return;

            previewCharacter.enabled = false;

            if (previewCharacter.MovementSystem != null)
                previewCharacter.MovementSystem.enabled = false;

            if (previewCharacter.SkillSystem != null)
                previewCharacter.SkillSystem.enabled = false;

            if (previewCharacter.InteractionSystem != null)
                previewCharacter.InteractionSystem.enabled = false;

            if (previewCharacter.Wallet != null)
                previewCharacter.Wallet.enabled = false;

            if (previewCharacter.TryGetComponent<CharacterController>(out CharacterController controller))
                controller.enabled = false;
        }

        private Vector3 GetFallbackPreviewPosition()
        {
            Camera sceneCamera = Camera.main;
            if (sceneCamera == null)
                return Vector3.zero;

            Vector3 position = sceneCamera.transform.position + sceneCamera.transform.forward * 6f;
            position.y = 0f;
            return position;
        }

        private void DestroyPreviewCharacter()
        {
            if (_previewCharacterInstance != null)
                Destroy(_previewCharacterInstance.gameObject);

            _previewCharacterInstance = null;
            _previewCharacterIndex = -1;
        }

        private void EnsureMenuServicesRegistered()
        {
            if (ServiceLocator.Instance == null)
                return;

            if (!ServiceLocator.Instance.Has<InteractionService>())
                ServiceLocator.Instance.Register(new InteractionService());

            if (!ServiceLocator.Instance.Has<ModificationScreenService>())
                ServiceLocator.Instance.Register(new ModificationScreenService());
        }

        private void OnDestroy()
        {
            DestroyPreviewCharacter();
        }

        void OnPlayClicked()
        {
            if (_menuView.HasCharacters)
            {
                _menuView.ShowCharacterSelection();
                return;
            }

            ServiceLocator.Instance.Get<SessionService>().StartSession();
        }

        void OnCharacterConfirmed(int characterIndex)
        {
            if (_availablePlayers == null || characterIndex < 0 || characterIndex >= _availablePlayers.Length)
                return;

            SessionService sessionService = ServiceLocator.Instance.Get<SessionService>();
            sessionService.SetSelectedPlayerPrefab(_availablePlayers[characterIndex]);
            sessionService.StartSession();
        }

        void OnQuitClicked()
        {
            Debug.Log("[MainMenu] Quit clicked.");
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private CharacterSelectionViewData[] BuildCharacterViewData()
        {
            if (_availablePlayers == null || _availablePlayers.Length == 0)
                return Array.Empty<CharacterSelectionViewData>();

            var data = new CharacterSelectionViewData[_availablePlayers.Length];
            for (int i = 0; i < _availablePlayers.Length; i++)
                data[i] = BuildCharacterViewData(i, _availablePlayers[i]);

            return data;
        }

        private static CharacterSelectionViewData BuildCharacterViewData(int characterIndex, Player playerPrefab)
        {
            if (playerPrefab == null)
            {
                return new CharacterSelectionViewData(
                    characterIndex,
                    "?",
                    "Unknown",
                    "Operative",
                    "Character prefab is missing.",
                    "--",
                    "--",
                    "--",
                    "--",
                    "Unavailable",
                    Array.Empty<CharacterSkillViewData>());
            }

            string displayName = GetDisplayName(playerPrefab);
            var config = playerPrefab.config;

            return new CharacterSelectionViewData(
                characterIndex,
                GetBadgeText(displayName),
                displayName,
                config != null && !string.IsNullOrWhiteSpace(config.Archetype) ? config.Archetype : "Operative",
                config != null && !string.IsNullOrWhiteSpace(config.Summary)
                    ? config.Summary
                    : "Deploy into the next run with a custom loadout and combat profile.",
                config != null ? Mathf.RoundToInt(config.MaxHealth).ToString() : "--",
                config != null ? Mathf.RoundToInt(config.AttackDamage).ToString() : "--",
                config != null ? config.MoveSpeed.ToString("0.0") : "--",
                config != null ? config.AttackRate.ToString("0.0") : "--",
                "Emporium",
                BuildSkillViewData(playerPrefab.SkillSystem));
        }

        private static CharacterSkillViewData[] BuildSkillViewData(PlayerSkillSystem skillSystem)
        {
            if (skillSystem == null)
                return Array.Empty<CharacterSkillViewData>();

            return new[]
            {
                CreateSkillViewData(skillSystem.GetConfiguredSkill(0), "LMB"),
                CreateSkillViewData(skillSystem.GetConfiguredSkill(1), "RMB"),
                CreateSkillViewData(skillSystem.GetConfiguredSkill(2), "SPACE"),
                CreateSkillViewData(skillSystem.GetConfiguredSkill(3), "G"),
                CreateSkillViewData(skillSystem.GetConfiguredPassive(), "PASSIVE")
            };
        }

        private static CharacterSkillViewData CreateSkillViewData(Skill skill, string slotLabel)
        {
            string skillName = skill != null && !string.IsNullOrWhiteSpace(skill.SkillName) ? skill.SkillName : "Empty";
            string badge = !string.IsNullOrWhiteSpace(skillName)
                ? skillName.Substring(0, 1).ToUpperInvariant()
                : "-";

            return new CharacterSkillViewData(slotLabel, badge, skillName);
        }

        private static string GetDisplayName(Player playerPrefab)
        {
            if (playerPrefab == null)
                return "Unknown";

            return playerPrefab.config != null && !string.IsNullOrWhiteSpace(playerPrefab.config.DisplayName)
                ? playerPrefab.config.DisplayName
                : playerPrefab.name;
        }

        private static string GetBadgeText(string displayName)
        {
            return string.IsNullOrWhiteSpace(displayName)
                ? "?"
                : displayName.Substring(0, 1).ToUpperInvariant();
        }
    }
}