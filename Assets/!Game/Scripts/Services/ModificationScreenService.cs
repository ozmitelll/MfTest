using System;
using System.Collections.Generic;
using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Entities;
using _Game.Scripts.Gameplay.Entities.Player;
using _Game.Scripts.Gameplay.Interactables;
using _Game.Scripts.Gameplay.Systems.Modifications;

namespace _Game.Scripts.Services
{
    public readonly struct ModificationStatViewData
    {
        public ModificationStatViewData(string label, string value)
        {
            Label = label;
            Value = value;
        }

        public string Label { get; }
        public string Value { get; }
    }

    public sealed class ModificationScreenSnapshot
    {
        public ModificationScreenSnapshot(
            string title,
            int usedCapacity,
            int capacity,
            ModificationCardInstance[] slots,
            ModificationCardInstance[] inventoryCards,
            ModificationStatViewData[] stats)
        {
            Title = title;
            UsedCapacity = usedCapacity;
            Capacity = capacity;
            Slots = slots;
            InventoryCards = inventoryCards;
            Stats = stats;
        }

        public string Title { get; }
        public int UsedCapacity { get; }
        public int Capacity { get; }
        public ModificationCardInstance[] Slots { get; }
        public ModificationCardInstance[] InventoryCards { get; }
        public ModificationStatViewData[] Stats { get; }
    }

    public class ModificationScreenService : IService
    {
        public event Action<ModificationStationNpc> OpenRequested;
        public event Action Closed;

        public bool IsOpen { get; private set; }
        public ModificationStationNpc CurrentStation { get; private set; }

        private Player _currentPlayer;

        public void Open(ModificationStationNpc station, Player player)
        {
            if (station == null || player == null)
                return;

            CurrentStation = station;
            _currentPlayer = player;
            IsOpen = true;
            OpenRequested?.Invoke(station);
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            CurrentStation = null;
            _currentPlayer = null;
            Closed?.Invoke();
        }

        public ModificationScreenSnapshot GetSnapshot()
        {
            if (!IsOpen || _currentPlayer == null)
                return null;

            var slots = new ModificationCardInstance[ModificationLoadoutSystem.MaxSlots];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = _currentPlayer.ModificationLoadoutSystem.GetCardInSlot(i);

            return new ModificationScreenSnapshot(
                $"{_currentPlayer.name} Modifications",
                _currentPlayer.ModificationLoadoutSystem.UsedCapacity,
                _currentPlayer.ModificationLoadoutSystem.Capacity,
                slots,
                BuildInventoryCards(),
                BuildStats());
        }

        public ModificationCardInstance GetCardInSlot(int slotIndex) =>
            _currentPlayer != null ? _currentPlayer.ModificationLoadoutSystem.GetCardInSlot(slotIndex) : null;

        public bool TryInstall(ModificationCardInstance card, int slotIndex, out string error)
        {
            if (_currentPlayer == null)
            {
                error = "Modification screen player context is missing.";
                return false;
            }

            return _currentPlayer.ModificationLoadoutSystem.TryInstall(card, slotIndex, out error);
        }

        public bool TryRemove(int slotIndex, out string error)
        {
            if (_currentPlayer == null)
            {
                error = "Modification screen player context is missing.";
                return false;
            }

            return _currentPlayer.ModificationLoadoutSystem.TryRemove(slotIndex, out error);
        }

        public bool TryMove(int fromSlotIndex, int toSlotIndex, out string error)
        {
            if (_currentPlayer == null)
            {
                error = "Modification screen player context is missing.";
                return false;
            }

            return _currentPlayer.ModificationLoadoutSystem.TryMove(fromSlotIndex, toSlotIndex, out error);
        }

        public bool IsCurrentEntity(Entity entity) => _currentPlayer != null && ReferenceEquals(_currentPlayer, entity);

        private ModificationCardInstance[] BuildInventoryCards()
        {
            var cards = new List<ModificationCardInstance>();
            foreach (ModificationCardInstance card in _currentPlayer.ModificationInventory.Cards)
            {
                if (_currentPlayer.ModificationLoadoutSystem.TryGetSlotIndex(card, out _))
                    continue;

                cards.Add(card);
            }

            return cards.ToArray();
        }

        private ModificationStatViewData[] BuildStats()
        {
            return new[]
            {
                new ModificationStatViewData("Max Health", _currentPlayer.StatsSystem.MaxHealth.Value.ToString("0.##")),
                new ModificationStatViewData("Move Speed", _currentPlayer.StatsSystem.MoveSpeed.Value.ToString("0.##")),
                new ModificationStatViewData("Armor", _currentPlayer.StatsSystem.Armor.Value.ToString("0.##")),
                new ModificationStatViewData("Magic Resist", _currentPlayer.StatsSystem.MagicResistance.Value.ToString("0.##")),
                new ModificationStatViewData("Elemental Resist", _currentPlayer.StatsSystem.ElementalResistance.Value.ToString("0.##")),
                new ModificationStatViewData("Attack Damage", _currentPlayer.StatsSystem.AttackDamage.Value.ToString("0.##")),
                new ModificationStatViewData("Attack Range", _currentPlayer.StatsSystem.AttackRange.Value.ToString("0.##")),
                new ModificationStatViewData("Attack Rate", _currentPlayer.StatsSystem.AttackRate.Value.ToString("0.##")),
                new ModificationStatViewData("Attack Type", _currentPlayer.StatsSystem.AttackDamageType.ToString())
            };
        }
    }
}
