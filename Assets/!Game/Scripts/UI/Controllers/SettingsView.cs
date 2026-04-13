using System;
using System.Collections.Generic;
using _Game.Scripts.Core;
using UnityEngine;
using UnityEngine.InputSystem;
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
        private const int RebindStartDelayMs = 75;
        private const string HoverTargetClass = "settings-hover-target";
        private const string HoverTargetNavClass = "settings-hover-target--nav";
        private const string HoverIndicatorClass = "settings-hover-indicator";
        private const string HoverIndicatorLineClass = "settings-hover-indicator__line";
        private const string HoverIndicatorCaretClass = "settings-hover-indicator__caret";
        private const string ScrollTransitionOutClass = "settings-scroll--transition-out";
        private const string ListeningKeyClass = "setting-key--listening";

        private sealed class BindingItem
        {
            public string StatusName;
            public Button Button;
            public InputAction Action;
            public int BindingIndex;
            public bool AllowMouse;
        }

        private readonly Dictionary<SettingsSection, VisualElement> _pages = new();
        private readonly Dictionary<SettingsSection, Button> _navButtons = new();
        private readonly List<(Button button, Action handler)> _subscriptions = new();
        private readonly List<BindingItem> _bindingItems = new();
        private readonly VisualElement _root;
        private readonly ScrollView _scrollView;
        private readonly Button _backButton;
        private readonly Action _backHandler;
        private readonly InputSystem_Actions _inputActions;
        private readonly Label _bindingsStatus;
        private readonly Button _bindingsDefaultButton;
        private IVisualElementScheduledItem _pendingTransitionItem;
        private IVisualElementScheduledItem _pendingRebindItem;
        private InputActionRebindingExtensions.RebindingOperation _activeRebindOperation;
        private BindingItem _activeBindingItem;
        private SettingsSection _currentSection;
        private SettingsSection _transitionTargetSection;
        private SettingsSection? _queuedSection;
        private bool _hasCurrentSection;
        private bool _isTransitioning;

        public event Action OnBackClicked;

        public SettingsView(VisualElement root)
        {
            _root = root;
            _scrollView = root.Q<ScrollView>("settings-scroll");
            _bindingsStatus = root.Q<Label>("bindings-status");
            _bindingsDefaultButton = root.Q<Button>("bindings-default");
            _inputActions = new InputSystem_Actions();
            InputBindingsPersistence.ApplySavedOverrides(_inputActions);

            ConfigureScrollView();

            AttachHoverIndicators(root);

            RegisterSection(root, SettingsSection.Game, "page-game", "nav-game");
            RegisterSection(root, SettingsSection.Graphics, "page-graphics", "nav-graphics");
            RegisterSection(root, SettingsSection.Sound, "page-sound", "nav-sound");
            RegisterSection(root, SettingsSection.Input, "page-input", "nav-input");
            RegisterSection(root, SettingsSection.Language, "page-language", "nav-language");
            RegisterSection(root, SettingsSection.Controller, "page-controller", "nav-controller", isInteractive: false);

            InitializeInputBindings(root);

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
            _pendingRebindItem?.Pause();
            CancelActiveRebind();

            for (int i = 0; i < _subscriptions.Count; i++)
            {
                (Button button, Action handler) = _subscriptions[i];
                button.clicked -= handler;
            }

            _subscriptions.Clear();

            if (_backButton != null && _backHandler != null)
                _backButton.clicked -= _backHandler;

            _inputActions.Dispose();
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

        private void InitializeInputBindings(VisualElement root)
        {
            RegisterBinding(root, "binding-move-up", _inputActions.Player.Move, "e2062cb9-1b15-46a2-838c-2f8d72a0bdd9");
            RegisterBinding(root, "binding-move-down", _inputActions.Player.Move, "320bffee-a40b-4347-ac70-c210eb8bc73a");
            RegisterBinding(root, "binding-move-left", _inputActions.Player.Move, "d2581a9b-1d11-4566-b27d-b92aff5fabbc");
            RegisterBinding(root, "binding-move-right", _inputActions.Player.Move, "fcfe95b8-67b9-4526-84b5-5d0bc98d6400");
            RegisterBinding(root, "binding-interact", _inputActions.Player.Interact, "aa3be111-2ad4-4f2b-9079-1d1c215e3d92");
            RegisterBinding(root, "binding-skill-1", _inputActions.Player.Skill1, "05f6913d-c316-48b2-a6bb-e225f14c7960", allowMouse: true);
            RegisterBinding(root, "binding-skill-2", _inputActions.Player.Skill2, "ce93755f-33b9-4d50-94ad-e60d7da7c6dc", allowMouse: true);
            RegisterBinding(root, "binding-skill-3", _inputActions.Player.Skill3, "44853405-3710-4474-a6cb-004a9a8414b3");
            RegisterBinding(root, "binding-skill-4", _inputActions.Player.Skill4, "aa177ad1-2caa-4dbf-bb1c-797a9e42a10b");
            RegisterBinding(root, "binding-sprint", _inputActions.Player.Sprint, "f2e9ba44-c423-42a7-ad56-f20975884794");
            RegisterBinding(root, "binding-crouch", _inputActions.Player.Crouch, "36e52cba-0905-478e-a818-f4bfcb9f3b9a");
            RegisterBinding(root, "binding-previous", _inputActions.Player.Previous, "1534dc16-a6aa-499d-9c3a-22b47347b52a");
            RegisterBinding(root, "binding-next", _inputActions.Player.Next, "cbac6039-9c09-46a1-b5f2-4e5124ccb5ed");

            if (_bindingsDefaultButton != null)
            {
                Action resetHandler = ResetBindingsToDefault;
                _bindingsDefaultButton.clicked += resetHandler;
                _subscriptions.Add((_bindingsDefaultButton, resetHandler));
            }

            RefreshBindingButtons();
            SetBindingsStatus("Нажмите на биндинг, чтобы переназначить его.");
        }

        private void RegisterBinding(VisualElement root, string buttonName, InputAction action, string bindingId, bool allowMouse = false)
        {
            Button button = root.Q<Button>(buttonName);
            if (button == null || action == null)
                return;

            int bindingIndex = FindBindingIndex(action, bindingId);
            if (bindingIndex < 0)
            {
                button.text = "[N/A]";
                button.SetEnabled(false);
                return;
            }

            var bindingItem = new BindingItem
            {
                StatusName = ResolveBindingStatusName(button),
                Button = button,
                Action = action,
                BindingIndex = bindingIndex,
                AllowMouse = allowMouse
            };

            Action handler = () => QueueInteractiveRebind(bindingItem);
            button.clicked += handler;
            _subscriptions.Add((button, handler));
            _bindingItems.Add(bindingItem);
        }

        private void QueueInteractiveRebind(BindingItem bindingItem)
        {
            if (bindingItem == null || _activeRebindOperation != null)
                return;

            _pendingRebindItem?.Pause();
            SetBindingsInteractable(false);
            _activeBindingItem = bindingItem;
            bindingItem.Button.AddToClassList(ListeningKeyClass);
            bindingItem.Button.text = "[...]";
            SetBindingsStatus($"Нажмите новую клавишу для {bindingItem.StatusName}. Esc - отмена.");

            _pendingRebindItem = _root.schedule.Execute(() => BeginInteractiveRebind(bindingItem)).StartingIn(RebindStartDelayMs);
        }

        private void BeginInteractiveRebind(BindingItem bindingItem)
        {
            if (bindingItem == null)
                return;

            _activeRebindOperation?.Dispose();

            _activeRebindOperation = bindingItem.Action.PerformInteractiveRebinding(bindingItem.BindingIndex)
                .WithCancelingThrough("<Keyboard>/escape")
                .WithControlsExcluding("<Pointer>/position")
                .WithControlsExcluding("<Pointer>/delta")
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Mouse>/scroll")
                .WithControlsExcluding("<Gamepad>")
                .WithControlsExcluding("<Joystick>")
                .WithControlsExcluding("<XRController>")
                .WithControlsExcluding("<Touchscreen>")
                .OnCancel(_ => FinishInteractiveRebind(bindingItem, wasCanceled: true))
                .OnComplete(_ => FinishInteractiveRebind(bindingItem, wasCanceled: false));

            if (!bindingItem.AllowMouse)
                _activeRebindOperation.WithControlsExcluding("<Mouse>");

            _activeRebindOperation.Start();
        }

        private void FinishInteractiveRebind(BindingItem bindingItem, bool wasCanceled)
        {
            if (!wasCanceled)
                InputBindingsPersistence.SaveOverrides(_inputActions);

            CleanupActiveRebind();
            RefreshBindingButtons();

            if (wasCanceled)
            {
                SetBindingsStatus("Переназначение отменено.");
                return;
            }

            SetBindingsStatus($"Биндинг для {bindingItem.StatusName} обновлен.");
        }

        private void CancelActiveRebind()
        {
            if (_activeRebindOperation == null)
                return;

            _activeRebindOperation.Cancel();
            CleanupActiveRebind();
        }

        private void CleanupActiveRebind()
        {
            _pendingRebindItem?.Pause();
            _pendingRebindItem = null;

            _activeRebindOperation?.Dispose();
            _activeRebindOperation = null;

            if (_activeBindingItem?.Button != null)
                _activeBindingItem.Button.RemoveFromClassList(ListeningKeyClass);

            _activeBindingItem = null;
            SetBindingsInteractable(true);
        }

        private void ResetBindingsToDefault()
        {
            if (_activeRebindOperation != null)
                return;

            InputBindingsPersistence.ResetOverrides(_inputActions);
            RefreshBindingButtons();
            SetBindingsStatus("Биндинги сброшены к значениям по умолчанию.");
        }

        private void RefreshBindingButtons()
        {
            for (int i = 0; i < _bindingItems.Count; i++)
            {
                BindingItem bindingItem = _bindingItems[i];
                if (bindingItem.Button == null)
                    continue;

                bindingItem.Button.RemoveFromClassList(ListeningKeyClass);
                bindingItem.Button.text = InputBindingsPersistence.FormatBindingDisplay(bindingItem.Action, bindingItem.BindingIndex);
            }
        }

        private void SetBindingsInteractable(bool isEnabled)
        {
            for (int i = 0; i < _bindingItems.Count; i++)
            {
                if (_bindingItems[i].Button != null)
                    _bindingItems[i].Button.SetEnabled(isEnabled);
            }

            _bindingsDefaultButton?.SetEnabled(isEnabled);
        }

        private void SetBindingsStatus(string text)
        {
            if (_bindingsStatus != null)
                _bindingsStatus.text = text;
        }

        private static int FindBindingIndex(InputAction action, string bindingId)
        {
            if (action == null || string.IsNullOrWhiteSpace(bindingId))
                return -1;

            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (string.Equals(action.bindings[i].id.ToString(), bindingId, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static string ResolveBindingStatusName(Button button)
        {
            VisualElement row = button?.parent?.parent;
            Label label = row?.Q<Label>(className: "settings-row__label");
            return string.IsNullOrWhiteSpace(label?.text) ? "этого действия" : label.text.ToLowerInvariant();
        }
    }
}