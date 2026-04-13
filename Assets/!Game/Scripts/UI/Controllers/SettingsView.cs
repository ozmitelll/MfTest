using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Game.Scripts.Views
{
    public enum SettingsSection
    {
        Game,
        Graphics,
        Sound,
        Input,
        Language,
        Controller
    }

    public class SettingsView : IDisposable
    {
        private const int SectionTransitionDurationMs = 170;
        private const string HoverTargetClass = "settings-hover-target";
        private const string HoverTargetNavClass = "settings-hover-target--nav";
        private const string HoverIndicatorClass = "settings-hover-indicator";
        private const string HoverIndicatorLineClass = "settings-hover-indicator__line";
        private const string HoverIndicatorCaretClass = "settings-hover-indicator__caret";
        private const string ScrollTransitionOutClass = "settings-scroll--transition-out";

        private readonly Dictionary<SettingsSection, VisualElement> _pages = new();
        private readonly Dictionary<SettingsSection, Button> _navButtons = new();
        private readonly List<(Button button, Action handler)> _subscriptions = new();
        private readonly ScrollView _scrollView;
        private readonly Button _backButton;
        private readonly Action _backHandler;
        private IVisualElementScheduledItem _pendingTransitionItem;
        private SettingsSection _currentSection;
        private SettingsSection _transitionTargetSection;
        private SettingsSection? _queuedSection;
        private bool _hasCurrentSection;
        private bool _isTransitioning;

        public event Action OnBackClicked;

        public SettingsView(VisualElement root)
        {
            _scrollView = root.Q<ScrollView>("settings-scroll");
            ConfigureScrollView();

            AttachHoverIndicators(root);

            RegisterSection(root, SettingsSection.Game, "page-game", "nav-game");
            RegisterSection(root, SettingsSection.Graphics, "page-graphics", "nav-graphics");
            RegisterSection(root, SettingsSection.Sound, "page-sound", "nav-sound");
            RegisterSection(root, SettingsSection.Input, "page-input", "nav-input");
            RegisterSection(root, SettingsSection.Language, "page-language", "nav-language");
            RegisterSection(root, SettingsSection.Controller, "page-controller", "nav-controller", isInteractive: false);

            _backButton = root.Q<Button>("nav-back");
            if (_backButton != null)
            {
                _backHandler = () => OnBackClicked?.Invoke();
                _backButton.clicked += _backHandler;
            }

            ShowSection(SettingsSection.Game);
        }

        public void ShowSection(SettingsSection section)
        {
            if (!_hasCurrentSection)
            {
                ShowSectionImmediate(section);
                _currentSection = section;
                _hasCurrentSection = true;
                return;
            }

            if (section == _currentSection && !_isTransitioning)
                return;

            if (_isTransitioning)
            {
                _queuedSection = section;
                return;
            }

            if (_scrollView == null)
            {
                ShowSectionImmediate(section);
                _currentSection = section;
                return;
            }

            BeginSectionTransition(section);
        }

        private void ShowSectionImmediate(SettingsSection section)
        {
            foreach (KeyValuePair<SettingsSection, VisualElement> pair in _pages)
                pair.Value.EnableInClassList("settings-page--active", pair.Key == section);

            foreach (KeyValuePair<SettingsSection, Button> pair in _navButtons)
                pair.Value.EnableInClassList("settings-nav__item--active", pair.Key == section);
        }

        public void Dispose()
        {
            _pendingTransitionItem?.Pause();

            for (int i = 0; i < _subscriptions.Count; i++)
            {
                (Button button, Action handler) = _subscriptions[i];
                button.clicked -= handler;
            }

            _subscriptions.Clear();

            if (_backButton != null && _backHandler != null)
                _backButton.clicked -= _backHandler;
        }

        private void BeginSectionTransition(SettingsSection section)
        {
            _isTransitioning = true;
            _transitionTargetSection = section;
            _scrollView.AddToClassList(ScrollTransitionOutClass);

            _pendingTransitionItem?.Pause();
            _pendingTransitionItem = _scrollView.schedule.Execute(CompleteSectionTransition).StartingIn(SectionTransitionDurationMs);
        }

        private void CompleteSectionTransition()
        {
            SettingsSection targetSection = _transitionTargetSection;

            ShowSectionImmediate(targetSection);
            _currentSection = targetSection;
            _scrollView.scrollOffset = Vector2.zero;
            _scrollView.RemoveFromClassList(ScrollTransitionOutClass);
            _isTransitioning = false;

            if (_queuedSection.HasValue && _queuedSection.Value != _currentSection)
            {
                SettingsSection queuedSection = _queuedSection.Value;
                _queuedSection = null;
                BeginSectionTransition(queuedSection);
            }
        }

        private void RegisterSection(
            VisualElement root,
            SettingsSection section,
            string pageName,
            string buttonName,
            bool isInteractive = true)
        {
            VisualElement page = root.Q(pageName);
            Button button = root.Q<Button>(buttonName);

            if (page == null || button == null)
                return;

            _pages[section] = page;
            _navButtons[section] = button;

            if (!isInteractive)
            {
                button.SetEnabled(false);
                return;
            }

            Action handler = () => ShowSection(section);
            button.clicked += handler;
            _subscriptions.Add((button, handler));
        }

        private static void AttachHoverIndicators(VisualElement root)
        {
            foreach (VisualElement row in root.Query<VisualElement>(className: "settings-row").ToList())
                EnsureHoverIndicator(row, isNavigation: false);

            foreach (Button button in root.Query<Button>(className: "settings-nav__item").ToList())
                EnsureHoverIndicator(button, isNavigation: true);
        }

        private static void EnsureHoverIndicator(VisualElement target, bool isNavigation)
        {
            if (target == null || target.Q(className: HoverIndicatorClass) != null)
                return;

            target.AddToClassList(HoverTargetClass);
            if (isNavigation)
                target.AddToClassList(HoverTargetNavClass);

            var indicator = new VisualElement();
            indicator.AddToClassList(HoverIndicatorClass);

            var line = new VisualElement();
            line.AddToClassList(HoverIndicatorLineClass);
            indicator.Add(line);

            var caret = new VisualElement();
            caret.AddToClassList(HoverIndicatorCaretClass);
            indicator.Add(caret);

            target.Add(indicator);
        }

        private void ConfigureScrollView()
        {
            if (_scrollView == null)
                return;

            _scrollView.mode = ScrollViewMode.Vertical;
            _scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        }
    }
}