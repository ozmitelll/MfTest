using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Entities.Player;
using _Game.Scripts.Services;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Interactables
{
    public class Chest : MonoBehaviour, IInteractable
    {
        [SerializeField] private int _cost = 50;

        private bool _isOpened;

        public string InteractionPrompt => _isOpened ? "" : $"Open ({_cost} coins)  [F]";
        public bool CanInteract(Player player) => !_isOpened;

        private void Start()     => ServiceLocator.Instance.Get<InteractionService>().Register(this);
        private void OnDestroy()
        {
            if (ServiceLocator.Instance != null && ServiceLocator.Instance.Has<InteractionService>())
                ServiceLocator.Instance.Get<InteractionService>().Unregister(this);
        }

        public void Interact(Player player)
        {
            _isOpened = true;
            EventBus.Publish(new OnChestOpenedEvent { Chest = this });
            Debug.Log("Chest interacted");
            // TODO: deduct coins from PlayerWallet, drop ModificationCard
        }
    }
}