using _Game.Scripts.Gameplay.Entities;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Systems.Modifications
{
    public abstract class ModificationEffectDefinition : ScriptableObject
    {
        public abstract void Apply(Entity entity);
        public abstract void Remove(Entity entity);
    }
}
