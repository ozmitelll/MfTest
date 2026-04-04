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
            if (_isOpened)
                return;

            _isOpened = true;

            if (ServiceLocator.Instance != null && ServiceLocator.Instance.Has<ModificationCardDatabaseService>())
            {
                ModificationCardDatabaseService cardDatabaseService =
                    ServiceLocator.Instance.Get<ModificationCardDatabaseService>();

                if (cardDatabaseService.TryGetRandomCard(out var rewardCard))
                {
                    player.ModificationInventory.AddCard(rewardCard);
                    Debug.Log($"Chest rewarded '{rewardCard.DisplayName}' ({rewardCard.Rarity}, {rewardCard.CardType}).", this);
                }
                else
                {
                    Debug.LogWarning("[Chest] No modifier cards are configured in the global card database.", this);
                }
            }
            else
            {
                Debug.LogWarning("[Chest] ModificationCardDatabaseService is not registered.", this);
            }

            EventBus.Publish(new OnChestOpenedEvent { Chest = this });
        }
    }
}
