using _Game.Scripts.Services;
using _Game.Scripts.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Game.Scripts.Core
{
    public class MenuBootstrap : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        
        private MenuView _menuView;
        // private SettingsView _settingsView;
        // private CharacterSelectView _characterSelectView;

        private void Start()
        {
            var root = _document.rootVisualElement;
            _menuView = new MenuView(root.Q("main-menu-panel"));

            _menuView.OnPlayClicked += OnPlayClicked;
            _menuView.OnQuitClicked += OnQuitClicked;
        }

        void OnPlayClicked()
        {
            ServiceLocator.Instance.Get<SceneService>().LoadScene(SceneNames.Game,0);
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