using System;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Systems.Modifications
{
    [CreateAssetMenu(fileName = "ModificationCard", menuName = "Modfall/Modifications/Card")]
    public class ModificationCardDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] [Min(1)] private int _rank = 1;
        [SerializeField] [Min(0)] private int _installCost = 1;
        [SerializeField] [TextArea] private string _description;
        [SerializeReference] private ModificationEffectDefinition[] _effects;

        public string Id => _id;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public int Rank => _rank;
        public int InstallCost => _installCost;
        public string Description => _description;
        public ModificationEffectDefinition[] Effects => _effects ?? Array.Empty<ModificationEffectDefinition>();
    }
}
