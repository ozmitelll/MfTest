using _Game.Scripts.Gameplay.Entities;
using _Game.Scripts.Gameplay.Skills;
using _Game.Scripts.Gameplay.Systems.StatusEffects;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Entities.Enemy.Systems
{
    public class EnemySkillSystem : MonoBehaviour
    {
        [Header("Active Skills")]
        [SerializeField] private ActiveSkill _skill1;
        [SerializeField] private ActiveSkill _skill2;
        [SerializeField] private ActiveSkill _skill3;
        [SerializeField] private ActiveSkill _skill4;

        [Header("Passive")]
        [SerializeField] private PassiveSkill _passive;

        private readonly float[] _cooldowns = new float[4];
        private readonly float[] _maxCooldowns = new float[4];

        private Entity _owner;
        private StatusEffectSystem _statusEffects;
        private bool _initialized;

        public void Initialize(Entity owner)
        {
            if (_initialized && _owner != null && _owner != owner)
                _passive?.OnRemove(_owner);

            _owner = owner;
            _statusEffects = owner != null ? owner.StatusEffectSystem : null;
            _initialized = owner != null;

            for (int i = 0; i < _cooldowns.Length; i++)
            {
                _cooldowns[i] = 0f;
                _maxCooldowns[i] = 0f;
            }

            if (_initialized)
                _passive?.OnApply(owner);
        }

        private void Update()
        {
            if (!_initialized)
                return;

            for (int i = 0; i < _cooldowns.Length; i++)
            {
                if (_cooldowns[i] > 0f)
                    _cooldowns[i] = Mathf.Max(0f, _cooldowns[i] - Time.deltaTime);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_owner != null)
                _passive?.OnRemove(_owner);
        }

        public bool HasSkill(int slot) => GetConfiguredSkill(slot) != null;

        public bool IsReady(int slot)
        {
            int index = slot - 1;
            if (!_initialized || index < 0 || index >= _cooldowns.Length)
                return false;

            if (_statusEffects?.BlocksSkills == true)
                return false;

            return GetConfiguredSkill(slot) != null && _cooldowns[index] <= 0f;
        }

        public bool TryActivate(int slot, Vector3 aimPosition)
        {
            ActiveSkill skill = GetConfiguredSkill(slot);
            if (skill == null)
                return false;

            return TryActivate(slot, skill, ToSkillSlot(slot), aimPosition);
        }

        public bool TryActivateAtTarget(int slot, Transform target)
        {
            Vector3 aimPosition = target != null
                ? target.position
                : _owner != null ? _owner.transform.position + _owner.transform.forward : transform.position + transform.forward;

            return TryActivate(slot, aimPosition);
        }

        public float GetCooldownRatio(int slot)
        {
            int index = slot - 1;
            if (index < 0 || index >= _maxCooldowns.Length)
                return 0f;

            return _maxCooldowns[index] > 0f ? Mathf.Clamp01(_cooldowns[index] / _maxCooldowns[index]) : 0f;
        }

        public float GetRemainingCooldown(int slot)
        {
            int index = slot - 1;
            if (index < 0 || index >= _cooldowns.Length)
                return 0f;

            return Mathf.Max(0f, _cooldowns[index]);
        }

        public ActiveSkill GetConfiguredSkill(int slot) => slot switch
        {
            1 => _skill1,
            2 => _skill2,
            3 => _skill3,
            4 => _skill4,
            _ => null
        };

        public PassiveSkill GetConfiguredPassive() => _passive;

        private bool TryActivate(int slot, ActiveSkill skill, SkillSlot skillSlot, Vector3 aimPosition)
        {
            int index = slot - 1;
            if (!_initialized || _owner == null || index < 0 || index >= _cooldowns.Length)
                return false;

            if (_cooldowns[index] > 0f || _statusEffects?.BlocksSkills == true)
                return false;

            SkillContext context = BuildContext(skill, skillSlot, aimPosition);
            if (!skill.CanActivate(context))
                return false;

            skill.Activate(context);
            float cooldown = skill.GetCooldown(context);
            _cooldowns[index] = cooldown;
            _maxCooldowns[index] = cooldown;
            return true;
        }

        private SkillContext BuildContext(ActiveSkill skill, SkillSlot skillSlot, Vector3 aimPosition)
        {
            Vector3 origin = _owner.transform.position;
            Vector3 direction = aimPosition - origin;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                direction = _owner.transform.forward;

            SkillContext context = new SkillContext
            {
                Owner = _owner,
                SkillSlot = skillSlot,
                DamageType = skill != null ? skill.ResolveDamageType(_owner) : _owner.StatsSystem.AttackDamageType,
                AimPosition = aimPosition,
                AimDirection = direction.normalized,
                DamageMultiplier = 1f,
                CooldownMultiplier = 1f,
                ProjectileSpeedMultiplier = 1f
            };

            _owner.ModificationLoadoutSystem.ApplySkillModifiers(ref context);
            return context;
        }

        private static SkillSlot ToSkillSlot(int slot) => slot switch
        {
            1 => SkillSlot.Skill1,
            2 => SkillSlot.Skill2,
            3 => SkillSlot.Skill3,
            4 => SkillSlot.Skill4,
            _ => SkillSlot.None
        };
    }
}