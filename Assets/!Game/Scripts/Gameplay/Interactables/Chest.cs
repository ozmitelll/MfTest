using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Entities.Player;
using _Game.Scripts.Gameplay.Systems.Modifications;
using _Game.Scripts.Services;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Interactables
{
    public class Chest : MonoBehaviour, IInteractable
    {
        [SerializeField] private int _cost = 50;

        private bool _isOpened;

        public string InteractionPrompt => _isOpened ? "" : $"Open ({_cost} coins)  [F]";
        public bool CanInteract(Player player) => !_isOpened && player != null && player.Wallet != null && player.Wallet.CanAfford(_cost);

        private void Start() => ServiceLocator.Instance.Get<InteractionService>().Register(this);
        private void OnDestroy()
        {
            if (ServiceLocator.Instance != null && ServiceLocator.Instance.Has<InteractionService>())
                ServiceLocator.Instance.Get<InteractionService>().Unregister(this);
        }

        public void Interact(Player player)
        {
            if (_isOpened || player == null)
                return;

            if (player.Wallet == null || !player.Wallet.CanAfford(_cost))
                return;

            if (!TryGetRewardCard(out ModificationCardDefinition rewardCard))
                return;

            if (!player.Wallet.TrySpendCoins(_cost))
                return;

            _isOpened = true;

            player.ModificationInventory.AddCard(rewardCard, ModificationCardAddReason.LootPickup);
            Debug.Log($"Chest rewarded '{rewardCard.DisplayName}' ({rewardCard.Rarity}, {rewardCard.CardType}).", this);

            EventBus.Publish(new OnChestOpenedEvent { Chest = this });
        }

        private bool TryGetRewardCard(out ModificationCardDefinition rewardCard)
        {
            rewardCard = null;

            if (ServiceLocator.Instance != null && ServiceLocator.Instance.Has<ModificationCardDatabaseService>())
            {
                ModificationCardDatabaseService cardDatabaseService =
                    ServiceLocator.Instance.Get<ModificationCardDatabaseService>();

                if (cardDatabaseService.TryGetRandomCard(out rewardCard))
                {
                    return true;
                }

                Debug.LogWarning("[Chest] No modifier cards are configured in the global card database.", this);
                return false;
            }

            Debug.LogWarning("[Chest] ModificationCardDatabaseService is not registered.", this);
            return false;
        }
    }
}
