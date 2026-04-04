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

        public IInteractable CurrentInteractable { get; private set; }

        public void Initialize(InputSystem_Actions.PlayerActions actions, Player player)
        {
            _actions     = actions;
            _player      = player;
            _service     = ServiceLocator.Instance.Get<InteractionService>();
            _modificationScreenService = ServiceLocator.Instance.Get<ModificationScreenService>();
            _initialized = true;

            _actions.Interact.performed += OnInteract;
        }

        private void OnEnable()
        {
            if (!_initialized) return;
            _actions.Interact.performed += OnInteract;
        }

        private void OnDisable()
        {
            if (!_initialized) return;
            _actions.Interact.performed -= OnInteract;
        }

        private void Update()
        {
            if (_modificationScreenService?.IsOpen == true)
            {
                CurrentInteractable = null;
                return;
            }

            CurrentInteractable = _service.GetNearest(transform.position, _range);
        }

        private void OnInteract(InputAction.CallbackContext _)
        {
            if (_modificationScreenService?.IsOpen == true)
                return;

            if (CurrentInteractable?.CanInteract(_player) == true)
                CurrentInteractable.Interact(_player);
        }
    }
}
