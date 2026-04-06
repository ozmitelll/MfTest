using System;
using _Game.Scripts.Configs;
using _Game.Scripts.Gameplay.Systems.Health;
using _Game.Scripts.Gameplay.Systems.Modifications;
using _Game.Scripts.Gameplay.Systems.StatusEffects;
using _Game.Scripts.Gameplay.Systems.Stats;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Entities
{
    public abstract class Entity : MonoBehaviour
    {
        [SerializeField] private HealthSystem _healthSystem;
        [SerializeField] private StatsSystem _statsSystem;
        [SerializeField] private ModificationInventory _modificationInventory;
        [SerializeField] private ModificationLoadoutSystem _modificationLoadoutSystem;
        [SerializeField] private StatusEffectSystem _statusEffectSystem;

        public HealthSystem HealthSystem =>
            _healthSystem != null ? _healthSystem : _healthSystem = GetComponent<HealthSystem>() ?? gameObject.AddComponent<HealthSystem>();

        public StatsSystem StatsSystem =>
            _statsSystem != null ? _statsSystem : _statsSystem = GetComponent<StatsSystem>() ?? gameObject.AddComponent<StatsSystem>();

        public ModificationInventory ModificationInventory =>
            _modificationInventory != null
                ? _modificationInventory
                : _modificationInventory = GetComponent<ModificationInventory>() ?? gameObject.AddComponent<ModificationInventory>();

        public ModificationLoadoutSystem ModificationLoadoutSystem =>
            _modificationLoadoutSystem != null
                ? _modificationLoadoutSystem
                : _modificationLoadoutSystem = GetComponent<ModificationLoadoutSystem>() ?? gameObject.AddComponent<ModificationLoadoutSystem>();

        public StatusEffectSystem StatusEffectSystem =>
            _statusEffectSystem != null
                ? _statusEffectSystem
                : _statusEffectSystem = GetComponent<StatusEffectSystem>() ?? gameObject.AddComponent<StatusEffectSystem>();

        protected void InitializeEntity(EntityConfig config)
        {
            if (config == null)
                throw new InvalidOperationException($"{GetType().Name} on '{name}' cannot initialize without an assigned config.");

            StatsSystem.Initialize(config);
            HealthSystem.Initialize(StatsSystem.MaxHealth.Value);
            StatusEffectSystem.Initialize(this);
            ModificationInventory.Clear();
            ModificationLoadoutSystem.Initialize(this, ModificationInventory, config.ModificationCapacity);

            if (config.StartingModifications == null)
                return;

            foreach (StartingModificationEntry entry in config.StartingModifications)
            {
                if (entry.Card == null)
                {
                    Debug.LogWarning($"[{GetType().Name}] '{name}' has an empty starting modification entry.", this);
                    continue;
                }

                ModificationCardInstance card = ModificationInventory.AddCard(entry.Card, ModificationCardAddReason.Initialization);
                if (card == null || !entry.InstallOnStart)
                    continue;

                if (ModificationLoadoutSystem.TryInstallInFirstFreeSlot(card, out int _, out string error))
                    continue;

                Debug.LogWarning(
                    $"[{GetType().Name}] Failed to auto-install '{entry.Card.DisplayName}' on '{name}': {error}",
                    this);
            }
        }
    }
}
