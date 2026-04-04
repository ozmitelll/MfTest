using System.Linq;
using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Entities;
using _Game.Scripts.Gameplay.Skills;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Systems.Modifications
{
    public class ModificationLoadoutSystem : MonoBehaviour
    {
        public const int MaxSlots = 10;

        private readonly ModificationCardInstance[] _slots = new ModificationCardInstance[MaxSlots];

        private Entity _entity;
        private ModificationInventory _inventory;
        private int _capacity;

        public int SlotCount => MaxSlots;
        public int Capacity => _capacity;
        public int UsedCapacity => _slots.Where(card => card != null).Sum(card => card.Definition.InstallCost);

        public void Initialize(Entity entity, ModificationInventory inventory, int capacity)
        {
            _entity = entity;
            _inventory = inventory;
            _capacity = Mathf.Max(0, capacity);
            ClearInstalledCards();
        }

        public ModificationCardInstance GetCardInSlot(int slotIndex) =>
            IsValidSlot(slotIndex) ? _slots[slotIndex] : null;

        public bool TryGetSlotIndex(ModificationCardInstance card, out int slotIndex)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (!ReferenceEquals(_slots[i], card))
                    continue;

                slotIndex = i;
                return true;
            }

            slotIndex = -1;
            return false;
        }

        public bool TryInstallInFirstFreeSlot(ModificationCardInstance card, out int slotIndex, out string error)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                    continue;

                if (TryInstall(card, i, out error))
                {
                    slotIndex = i;
                    return true;
                }
            }

            slotIndex = -1;
            error = "No free modifier slots available.";
            return false;
        }

        public bool TryInstall(ModificationCardInstance card, int slotIndex, out string error)
        {
            if (_entity == null || _inventory == null)
            {
                error = "Modifier loadout is not initialized.";
                return false;
            }

            if (card == null)
            {
                error = "Card instance is null.";
                return false;
            }

            if (card.Definition == null)
            {
                error = "Card definition is missing.";
                return false;
            }

            if (!IsValidSlot(slotIndex))
            {
                error = $"Slot index {slotIndex} is outside the valid range 0-{MaxSlots - 1}.";
                return false;
            }

            if (_slots[slotIndex] != null)
            {
                error = $"Slot {slotIndex} is already occupied.";
                return false;
            }

            if (!_inventory.Contains(card))
            {
                error = $"Card '{card.Definition.DisplayName}' is not present in the inventory.";
                return false;
            }

            if (_slots.Contains(card))
            {
                error = $"Card '{card.Definition.DisplayName}' is already installed.";
                return false;
            }

            int nextCapacity = UsedCapacity + card.Definition.InstallCost;
            if (nextCapacity > _capacity)
            {
                error = $"Installing '{card.Definition.DisplayName}' would exceed capacity {nextCapacity}/{_capacity}.";
                return false;
            }

            ApplyCardEffects(card);
            _slots[slotIndex] = card;
            RefreshHealthIfNeeded();

            EventBus.Publish(new OnModificationInstalledEvent
            {
                Entity = _entity,
                Card = card,
                SlotIndex = slotIndex,
                UsedCapacity = UsedCapacity,
                Capacity = _capacity
            });

            error = null;
            return true;
        }

        public bool TryMove(int fromSlotIndex, int toSlotIndex, out string error)
        {
            if (!IsValidSlot(fromSlotIndex) || !IsValidSlot(toSlotIndex))
            {
                error = $"Move from {fromSlotIndex} to {toSlotIndex} is outside the valid slot range.";
                return false;
            }

            if (fromSlotIndex == toSlotIndex)
            {
                error = "Source and target slots are the same.";
                return false;
            }

            ModificationCardInstance card = _slots[fromSlotIndex];
            if (card == null)
            {
                error = $"Slot {fromSlotIndex} is empty.";
                return false;
            }

            if (_slots[toSlotIndex] != null)
            {
                error = $"Slot {toSlotIndex} is already occupied.";
                return false;
            }

            _slots[fromSlotIndex] = null;
            _slots[toSlotIndex] = card;

            EventBus.Publish(new OnModificationRemovedEvent
            {
                Entity = _entity,
                Card = card,
                SlotIndex = fromSlotIndex,
                UsedCapacity = UsedCapacity,
                Capacity = _capacity
            });

            EventBus.Publish(new OnModificationInstalledEvent
            {
                Entity = _entity,
                Card = card,
                SlotIndex = toSlotIndex,
                UsedCapacity = UsedCapacity,
                Capacity = _capacity
            });

            error = null;
            return true;
        }

        public bool TryRemove(int slotIndex, out string error)
        {
            if (!IsValidSlot(slotIndex))
            {
                error = $"Slot index {slotIndex} is outside the valid range 0-{MaxSlots - 1}.";
                return false;
            }

            ModificationCardInstance card = _slots[slotIndex];
            if (card == null)
            {
                error = $"Slot {slotIndex} is already empty.";
                return false;
            }

            RemoveCardEffects(card);
            _slots[slotIndex] = null;
            RefreshHealthIfNeeded();

            EventBus.Publish(new OnModificationRemovedEvent
            {
                Entity = _entity,
                Card = card,
                SlotIndex = slotIndex,
                UsedCapacity = UsedCapacity,
                Capacity = _capacity
            });

            error = null;
            return true;
        }

        public void ApplySkillModifiers(ref SkillContext context)
        {
            if (_entity == null)
                return;

            foreach (ModificationCardInstance card in _slots)
            {
                if (card?.Definition == null)
                    continue;

                foreach (ModificationEffectDefinition effect in card.Definition.Effects)
                {
                    if (effect == null || !effect.CanModifySkill(context.SkillSlot))
                        continue;

                    effect.ModifySkillContext(_entity, ref context);
                }
            }
        }

        public void ClearInstalledCards()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                    continue;

                RemoveCardEffects(_slots[i]);
                _slots[i] = null;
            }

            if (_entity != null)
                RefreshHealthIfNeeded();
        }

        private void ApplyCardEffects(ModificationCardInstance card)
        {
            foreach (ModificationEffectDefinition effect in card.Definition.Effects)
            {
                if (effect == null)
                {
                    Debug.LogWarning($"[{nameof(ModificationLoadoutSystem)}] Card '{card.Definition.DisplayName}' has a null effect reference.", this);
                    continue;
                }

                if (!effect.AppliesOnInstall)
                    continue;

                effect.Apply(_entity);
            }
        }

        private void RemoveCardEffects(ModificationCardInstance card)
        {
            foreach (ModificationEffectDefinition effect in card.Definition.Effects)
            {
                if (effect == null)
                {
                    Debug.LogWarning($"[{nameof(ModificationLoadoutSystem)}] Card '{card.Definition.DisplayName}' has a null effect reference.", this);
                    continue;
                }

                if (!effect.AppliesOnInstall)
                    continue;

                effect.Remove(_entity);
            }
        }

        private void RefreshHealthIfNeeded()
        {
            _entity.HealthSystem.SetMaxHealth(_entity.StatsSystem.MaxHealth.Value, preserveRatio: true);
        }

        private static bool IsValidSlot(int slotIndex) => slotIndex >= 0 && slotIndex < MaxSlots;
    }
}
