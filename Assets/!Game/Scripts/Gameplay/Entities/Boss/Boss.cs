using _Game.Scripts.Core;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Entities.Bosses
{
    [RequireComponent(typeof(BossSkillSystem))]
    public class Boss : _Game.Scripts.Gameplay.Entities.Enemy.Enemy
    {
        protected override void OnDied()
        {
            EventBus.Publish(new OnBossDiedEvent { Boss = this, BossInstanceId = GetInstanceID() });
            Destroy(gameObject);
        }
    }
}