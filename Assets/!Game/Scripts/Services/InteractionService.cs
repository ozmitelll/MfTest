using System.Collections.Generic;
using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Interactables;
using UnityEngine;

namespace _Game.Scripts.Services
{
    public class InteractionService : IService
    {
        private readonly List<IInteractable> _interactables = new();

        public void Register(IInteractable interactable)   => _interactables.Add(interactable);
        public void Unregister(IInteractable interactable) => _interactables.Remove(interactable);

        public IInteractable GetNearest(Vector3 position, float range)
        {
            IInteractable best     = null;
            float         bestDist = range;

            foreach (var interactable in _interactables)
            {
                if (interactable is not MonoBehaviour mb) continue;
                float dist = Vector3.Distance(position, mb.transform.position);
                if (dist < bestDist) { bestDist = dist; best = interactable; }
            }

            return best;
        }
    }
}
