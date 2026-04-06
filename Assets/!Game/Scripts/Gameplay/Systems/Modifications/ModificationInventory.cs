using System.Collections.Generic;
using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Entities;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Systems.Modifications
{
    public enum ModificationCardAddReason
    {
        Unknown = 0,
        Initialization = 1,
        LootPickup = 2
    }

    public class ModificationInventory : MonoBehaviour
    {
        private readonly List<ModificationCardInstance> _cards = new();
        private Entity _owner;

        public IReadOnlyList<ModificationCardInstance> Cards => _cards;

        private void Awake() => _owner = GetComponent<Entity>();

        public ModificationCardInstance AddCard(ModificationCardDefinition definition, ModificationCardAddReason reason = ModificationCardAddReason.Unknown)
        {
            if (definition == null)
            {
                Debug.LogWarning($"[{nameof(ModificationInventory)}] Tried to add a null card definition.", this);
                return null;
            }

            var instance = new ModificationCardInstance(definition);
            _cards.Add(instance);
            _owner ??= GetComponent<Entity>();

            EventBus.Publish(new OnModificationCardAddedEvent
            {
                Entity = _owner,
                Card = instance,
                TotalCards = _cards.Count,
                Reason = reason
            });

            return instance;
        }

        public bool Contains(ModificationCardInstance card) => card != null && _cards.Contains(card);

        public void Clear() => _cards.Clear();
    }
}
