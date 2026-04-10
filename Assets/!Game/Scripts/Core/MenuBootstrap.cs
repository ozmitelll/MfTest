using System;
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
        [SerializeField] private Player[] _availablePlayers;
        
        private MenuView _menuView;
        // private SettingsView _settingsView;
        // private CharacterSelectView _characterSelectView;

        private void Start()
        {
            var root = _document.rootVisualElement;
            _menuView = new MenuView(root);
            _menuView.BindCharacters(BuildCharacterViewData());

            _menuView.OnPlayClicked += OnPlayClicked;
            _menuView.OnCharacterConfirmed += OnCharacterConfirmed;
            _menuView.OnQuitClicked += OnQuitClicked;
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