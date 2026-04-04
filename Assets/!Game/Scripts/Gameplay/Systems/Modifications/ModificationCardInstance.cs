using System;

namespace _Game.Scripts.Gameplay.Systems.Modifications
{
    [Serializable]
    public class ModificationCardInstance
    {
        public string InstanceId;
        public ModificationCardDefinition Definition;

        public ModificationCardInstance(ModificationCardDefinition definition)
        {
            InstanceId = Guid.NewGuid().ToString("N");
            Definition = definition;
        }
    }
}
