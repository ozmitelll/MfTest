using _Game.Scripts.Gameplay.Entities;
using _Game.Scripts.Gameplay.Skills;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Game.Scripts.Gameplay.Entities.Player.Systems
{
    public class PlayerSkillSystem : MonoBehaviour
    {
        [Header("Active Skills")]
        [SerializeField] private ActiveSkill _skill1; // LMB
        [SerializeField] private ActiveSkill _skill2; // RMB
        [SerializeField] private ActiveSkill _skill3; // Space
        [SerializeField] private ActiveSkill _skill4; // G

        [Header("Passive")]
        [SerializeField] private PassiveSkill _passive;

        private readonly float[] _cooldowns    = new float[4];
        private readonly float[] _maxCooldowns = new float[4];
        private InputSystem_Actions.PlayerActions _actions;
        private Entity _owner;
        private Camera _camera;

        public void Initialize(InputSystem_Actions.PlayerActions actions, Entity owner)
        {
            _actions = actions;
            _owner   = owner;
            _camera  = Camera.main;
            _passive?.OnApply(owner);
        }

        private void OnEnable()
        {
            _actions.Skill1.performed += OnSkill1;
            _actions.Skill2.performed += OnSkill2;
            _actions.Skill3.performed += OnSkill3;
            _actions.Skill4.performed += OnSkill4;
        }

        private void OnDisable()
        {
            _actions.Skill1.performed -= OnSkill1;
            _actions.Skill2.performed -= OnSkill2;
            _actions.Skill3.performed -= OnSkill3;
            _actions.Skill4.performed -= OnSkill4;
        }

        private void OnDestroy() => _passive?.OnRemove(_owner);

        private void Update()
        {
            for (int i = 0; i < _cooldowns.Length; i++)
                if (_cooldowns[i] > 0f)
                    _cooldowns[i] -= Time.deltaTime;

            if (_skill1 is { HoldToRepeat: true } && _actions.Skill1.IsPressed()) TryActivate(0, _skill1);
            if (_skill2 is { HoldToRepeat: true } && _actions.Skill2.IsPressed()) TryActivate(1, _skill2);
            if (_skill3 is { HoldToRepeat: true } && _actions.Skill3.IsPressed()) TryActivate(2, _skill3);
            if (_skill4 is { HoldToRepeat: true } && _actions.Skill4.IsPressed()) TryActivate(3, _skill4);
        }

        private void OnSkill1(InputAction.CallbackContext _) => TryActivate(0, _skill1);
        private void OnSkill2(InputAction.CallbackContext _) => TryActivate(1, _skill2);
        private void OnSkill3(InputAction.CallbackContext _) => TryActivate(2, _skill3);
        private void OnSkill4(InputAction.CallbackContext _) => TryActivate(3, _skill4);

        private void TryActivate(int slot, ActiveSkill skill)
        {
            if (skill == null || _cooldowns[slot] > 0f) return;

            var ctx = BuildContext();
            if (!skill.CanActivate(ctx)) return;

            skill.Activate(ctx);
            var cd = skill.GetCooldown(ctx);
            _cooldowns[slot]    = cd;
            _maxCooldowns[slot] = cd;
        }

        // 0 = готов, 1 = только что использован
        public float GetCooldownRatio(int slot) =>
            _maxCooldowns[slot] > 0f ? Mathf.Clamp01(_cooldowns[slot] / _maxCooldowns[slot]) : 0f;

        private ActiveSkill GetSkill(int slot) => slot switch
        {
            0 => _skill1, 1 => _skill2, 2 => _skill3, 3 => _skill4, _ => null
        };

        private SkillContext BuildContext()
        {
            var aim = GetAimPosition();
            return new SkillContext
            {
                Owner        = _owner,
                AimPosition  = aim,
                AimDirection = (aim - _owner.transform.position).normalized
            };
        }

        private Vector3 GetAimPosition()
        {
            if (_camera == null) return _owner.transform.position;
            var ray   = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            var plane = new Plane(Vector3.up, _owner.transform.position);
            return plane.Raycast(ray, out float dist) ? ray.GetPoint(dist) : _owner.transform.position;
        }
    }
}
