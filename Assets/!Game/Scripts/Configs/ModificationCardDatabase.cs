using System;
using _Game.Scripts.Gameplay.Systems.Modifications;
using UnityEngine;

namespace _Game.Scripts.Configs
{
    [CreateAssetMenu(fileName = "ModificationCardDatabase", menuName = "Modfall/Configs/Modification Card Database")]
    public class ModificationCardDatabase : ScriptableObject
    {
        [SerializeField] private ModificationCardDefinition[] _cards;

        public ModificationCardDefinition[] Cards => _cards ?? Array.Empty<ModificationCardDefinition>();

        public bool TryGetRandomCard(out ModificationCardDefinition card)
        {
            card = null;
            if (_cards == null || _cards.Length == 0)
                return false;

            int index = UnityEngine.Random.Range(0, _cards.Length);
            card = _cards[index];
            return card != null;
        }
    }
}
