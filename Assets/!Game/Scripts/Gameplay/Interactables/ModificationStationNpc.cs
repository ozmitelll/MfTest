using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Entities.Player;
using _Game.Scripts.Services;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Interactables
{
    public class ModificationStationNpc : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _interactionLabel = "Configure Modifiers";

        public string InteractionPrompt => $"{_interactionLabel}  [F]";

        public bool CanInteract(Player player)
        {
            if (player == null || ServiceLocator.Instance == null || !ServiceLocator.Instance.Has<ModificationScreenService>())
                return false;

            return !ServiceLocator.Instance.Get<ModificationScreenService>().IsOpen;
        }

        private void Start() => ServiceLocator.Instance.Get<InteractionService>().Register(this);

        private void OnDestroy()
        {
            if (ServiceLocator.Instance != null && ServiceLocator.Instance.Has<InteractionService>())
                ServiceLocator.Instance.Get<InteractionService>().Unregister(this);
        }

        public void Interact(Player player)
        {
            if (player == null || ServiceLocator.Instance == null || !ServiceLocator.Instance.Has<ModificationScreenService>())
                return;

            ServiceLocator.Instance.Get<ModificationScreenService>().Open(this, player);
        }
    }
}
