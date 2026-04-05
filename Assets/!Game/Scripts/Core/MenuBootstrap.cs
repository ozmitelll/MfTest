using _Game.Scripts.Services;
using _Game.Scripts.Gameplay.Entities.Player;
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
            _menuView.BindCharacters(_availablePlayers);

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

        void OnCharacterConfirmed(Player playerPrefab)
        {
            SessionService sessionService = ServiceLocator.Instance.Get<SessionService>();
            sessionService.SetSelectedPlayerPrefab(playerPrefab);
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
    }
}