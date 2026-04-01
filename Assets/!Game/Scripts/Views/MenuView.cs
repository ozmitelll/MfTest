using System;
using UnityEngine.UIElements;

namespace _Game.Scripts.Views
{
    public class MenuView
    {
        private readonly VisualElement _root;
        
        public event Action OnPlayClicked;
        public event Action OnSettingsClicked;
        public event Action OnQuitClicked;
        
        public MenuView(VisualElement root)
        {
            _root = root;
            _root.Q<Button>("btn-play").clicked     += () => OnPlayClicked?.Invoke();
            _root.Q<Button>("btn-settings").clicked += () => OnSettingsClicked?.Invoke();
            _root.Q<Button>("btn-quit").clicked     += () => OnQuitClicked?.Invoke();
        }
        public void Show() => _root.style.display = DisplayStyle.Flex;
        public void Hide() => _root.style.display = DisplayStyle.None;
        public void Dispose() { }
    }
}