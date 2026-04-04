using UnityEngine;

namespace _Game.Scripts.Gameplay.Skills
{
    [CreateAssetMenu(fileName = "DashSkill", menuName = "Modfall/Skills/Dash Skill")]
    public class DashSkill : ActiveSkill
    {
        [Header("Dash")]
        public float MaxDistance = 6f;
        public LayerMask GroundMask = ~0; // все слои по умолчанию

        // Проверяем до активации — нет земли = отменяем
        public override bool CanActivate(SkillContext ctx) => HasGroundAtTarget(GetTarget(ctx));

        public override void Activate(SkillContext ctx)
        {
            var target = GetTarget(ctx);
            var cc = ctx.Owner.GetComponent<CharacterController>();
            cc.enabled = false;
            ctx.Owner.transform.position = target;
            cc.enabled = true;
        }

        private Vector3 GetTarget(SkillContext ctx)
        {
            var owner  = ctx.Owner.transform;
            var toAim  = ctx.AimPosition - owner.position;
            toAim.y    = 0f;
            return owner.position + toAim.normalized * Mathf.Min(toAim.magnitude, MaxDistance);
        }

        private bool HasGroundAtTarget(Vector3 target) =>
            Physics.Raycast(target + Vector3.up * 2f, Vector3.down, 4f, GroundMask);
    }
}
