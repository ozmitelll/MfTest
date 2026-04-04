using _Game.Scripts.Gameplay.Entities.Player;

namespace _Game.Scripts.Gameplay.Interactables
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        bool   CanInteract(Player player);
        void   Interact(Player player);
    }
}
