using _Game.Scripts.Gameplay.Entities;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Skills
{
    public struct SkillContext
    {
        public Entity  Owner;
        public Vector3 AimPosition;   // точка прицела в мировых координатах
        public Vector3 AimDirection;  // нормализованное направление от владельца к прицелу
    }
}
