using System;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Systems.Modifications
{
    [Serializable]
    public struct StartingModificationEntry
    {
        [SerializeField] private ModificationCardDefinition _card;
        [SerializeField] private bool _installOnStart;

        public ModificationCardDefinition Card => _card;
        public bool InstallOnStart => _installOnStart;
    }
}
