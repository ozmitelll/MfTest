using _Game.Scripts.Core;
using _Game.Scripts.Gameplay.Entities;
using _Game.Scripts.Gameplay.Skills;
using _Game.Scripts.Gameplay.Systems.StatusEffects;
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
        private StatusEffectSystem _statusEffects;
        private bool   _initialized;
        private bool   _inputSubscribed;

        public void Initialize(InputSystem_Actions.PlayerActions actions, Entity owner)
        {
            _actions     = actions;
            _owner       = owner;
            _camera      = Camera.main;
            _statusEffects = owner.StatusEffectSystem;
            _initialized = true;
            _passive?.OnApply(owner);

            if (isActiveAndEnabled)
                SubscribeInput();

            PublishSkillIcons();
            PublishCooldownState();
        }

        private void OnEnable()
        {
            if (!_initialized) return;
            SubscribeInput();
        }

        private void OnDisable()
        {
            if (!_initialized) return;
            UnsubscribeInput();
        }

        private void OnDestroy() => _passive?.OnRemove(_owner);

        private void Update()
        {
            if (_owner?.HealthSystem.IsDead == true)
                return;

            bool cooldownsChanged = false;
            for (int i = 0; i < _cooldowns.Length; i++)
            {
                if (_cooldowns[i] <= 0f)
                    continue;

                float updatedCooldown = Mathf.Max(0f, _cooldowns[i] - Time.deltaTime);
                if (!Mathf.Approximately(updatedCooldown, _cooldowns[i]))
                {
                    _cooldowns[i] = updatedCooldown;
                    cooldownsChanged = true;
                }
            }

            if (cooldownsChanged)
                PublishCooldownState();

            if (_skill1 is { HoldToRepeat: true } && _actions.Skill1.IsPressed()) TryActivate(0, _skill1, SkillSlot.Skill1);
            if (_skill2 is { HoldToRepeat: true } && _actions.Skill2.IsPressed()) TryActivate(1, _skill2, SkillSlot.Skill2);
            if (_skill3 is { HoldToRepeat: true } && _actions.Skill3.IsPressed()) TryActivate(2, _skill3, SkillSlot.Skill3);
            if (_skill4 is { HoldToRepeat: true } && _actions.Skill4.IsPressed()) TryActivate(3, _skill4, SkillSlot.Skill4);
        }

        private void OnSkill1(InputAction.CallbackContext _) => TryActivate(0, _skill1, SkillSlot.Skill1);
        private void OnSkill2(InputAction.CallbackContext _) => TryActivate(1, _skill2, SkillSlot.Skill2);
        private void OnSkill3(InputAction.CallbackContext _) => TryActivate(2, _skill3, SkillSlot.Skill3);
        private void OnSkill4(InputAction.CallbackContext _) => TryActivate(3, _skill4, SkillSlot.Skill4);

        private void TryActivate(int slot, ActiveSkill skill, SkillSlot skillSlot)
        {
            if (skill == null || _cooldowns[slot] > 0f) return;
            if (_owner?.HealthSystem.IsDead == true) return;
            if (_statusEffects?.BlocksSkills == true) return;

            var ctx = BuildContext(skill, skillSlot);
            if (!skill.CanActivate(ctx)) return;

            skill.Activate(ctx);
            var cd = skill.GetCooldown(ctx);
            _cooldowns[slot]    = cd;
            _maxCooldowns[slot] = cd;
            PublishCooldownState();
        }

        // 0 = готов, 1 = только что использован
        public float GetCooldownRatio(int slot) =>
            _maxCooldowns[slot] > 0f ? Mathf.Clamp01(_cooldowns[slot] / _maxCooldowns[slot]) : 0f;

        public float  GetRemainingCooldown(int slot) => Mathf.Max(0f, _cooldowns[slot]);
        public Sprite GetSkillIcon(int slot)          => GetSkill(slot)?.Icon;
        public Sprite GetPassiveIcon()                => _passive?.Icon;
        public ActiveSkill GetConfiguredSkill(int slot) => GetSkill(slot);
        public PassiveSkill GetConfiguredPassive() => _passive;

        private ActiveSkill GetSkill(int slot) => slot switch
        {
            0 => _skill1, 1 => _skill2, 2 => _skill3, 3 => _skill4, _ => null
        };

        private SkillContext BuildContext(ActiveSkill skill, SkillSlot skillSlot)
        {
            var aim = GetAimPosition();
            SkillContext context = new SkillContext
            {
                Owner        = _owner,
                SkillSlot    = skillSlot,
            DamageType   = skill != null ? skill.ResolveDamageType(_owner) : _owner.StatsSystem.AttackDamageType,
                AimPosition  = aim,
                AimDirection = (aim - _owner.transform.position).normalized,
                DamageMultiplier = 1f,
                CooldownMultiplier = 1f,
                ProjectileSpeedMultiplier = 1f
            };

            _owner.ModificationLoadoutSystem.ApplySkillModifiers(ref context);
            return context;
        }

        private Vector3 GetAimPosition()
        {
            if (_camera == null) return _owner.transform.position;
            var ray   = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            var plane = new Plane(Vector3.up, _owner.transform.position);
            return plane.Raycast(ray, out float dist) ? ray.GetPoint(dist) : _owner.transform.position;
        }

        private void SubscribeInput()
        {
            if (_inputSubscribed)
                return;

            _actions.Skill1.performed += OnSkill1;
            _actions.Skill2.performed += OnSkill2;
            _actions.Skill3.performed += OnSkill3;
            _actions.Skill4.performed += OnSkill4;
            _inputSubscribed = true;
        }

        private void UnsubscribeInput()
        {
            if (!_inputSubscribed)
                return;

            _actions.Skill1.performed -= OnSkill1;
            _actions.Skill2.performed -= OnSkill2;
            _actions.Skill3.performed -= OnSkill3;
            _actions.Skill4.performed -= OnSkill4;
            _inputSubscribed = false;
        }

        private void PublishSkillIcons()
        {
            EventBus.Publish(new OnPlayerSkillIconsChangedEvent
            {
                Skill0Icon = GetSkillIcon(0),
                Skill1Icon = GetSkillIcon(1),
                Skill2Icon = GetSkillIcon(2),
                Skill3Icon = GetSkillIcon(3),
                PassiveIcon = GetPassiveIcon()
            });
        }

        private void PublishCooldownState()
        {
            EventBus.Publish(new OnPlayerSkillCooldownsChangedEvent
            {
                Slot1Ratio = GetCooldownRatio(1),
                Slot2Ratio = GetCooldownRatio(2),
                Slot3Ratio = GetCooldownRatio(3),
                Slot1Remaining = GetRemainingCooldown(1),
                Slot2Remaining = GetRemainingCooldown(2),
                Slot3Remaining = GetRemainingCooldown(3)
            });
        }
    }
}
