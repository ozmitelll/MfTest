using System;
using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Interactables;
using _Game.Scripts.Services;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Game.Scripts.Gameplay.Entities.Player.Systems
{
    public class InteractionSystem : MonoBehaviour
    {
        [SerializeField] private float _range = 2f;

        private InputSystem_Actions.PlayerActions _actions;
        private Player             _player;
        private InteractionService _service;
        private ModificationScreenService _modificationScreenService;
        private bool               _initialized;
        private bool               _inputSubscribed;
        private string             _lastPublishedPrompt = string.Empty;

        public IInteractable CurrentInteractable { get; private set; }

        public void Initialize(InputSystem_Actions.PlayerActions actions, Player player)
        {
            _actions     = actions;
            _player      = player;
            _service     = ServiceLocator.Instance.Get<InteractionService>();
            _modificationScreenService = ServiceLocator.Instance.Get<ModificationScreenService>();
            _initialized = true;

            if (isActiveAndEnabled)
                SubscribeInput();
        }

        private void OnEnable()
        {
            if (!_initialized) return;
            SubscribeInput();
        }

        private void OnDisable()
        {
            if (!_initialized) return;
            UnsubscribeInput();
        }

        private void Update()
        {
            if (_player == null || _player.HealthSystem.IsDead)
            {
                CurrentInteractable = null;
                PublishPromptIfChanged(string.Empty);
                return;
            }

            if (_modificationScreenService?.IsOpen == true)
            {
                CurrentInteractable = null;
                PublishPromptIfChanged(string.Empty);
                return;
            }

            CurrentInteractable = _service.GetNearest(transform.position, _range);
            string prompt = CurrentInteractable != null ? CurrentInteractable.InteractionPrompt : string.Empty;
            PublishPromptIfChanged(prompt);
        }

        private void OnInteract(InputAction.CallbackContext _)
        {
            if (_player == null || _player.HealthSystem.IsDead)
                return;

            if (_modificationScreenService?.IsOpen == true)
                return;

            if (CurrentInteractable?.CanInteract(_player) == true)
                CurrentInteractable.Interact(_player);
        }

        private void SubscribeInput()
        {
            if (_inputSubscribed)
                return;

            _actions.Interact.performed += OnInteract;
            _inputSubscribed = true;
        }

        private void UnsubscribeInput()
        {
            if (!_inputSubscribed)
                return;

            _actions.Interact.performed -= OnInteract;
            _inputSubscribed = false;
        }

        private void PublishPromptIfChanged(string prompt)
        {
            prompt ??= string.Empty;
            if (string.Equals(_lastPublishedPrompt, prompt, StringComparison.Ordinal))
                return;

            _lastPublishedPrompt = prompt;
            EventBus.Publish(new OnInteractionPromptChangedEvent { Prompt = prompt });
        }
    }
}
