using _Game.Scripts.Configs;
using _Game.Scripts.Core;
using _Game.Scripts.Services;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Entities.Enemy.Systems
{
    public class EnemyAIStateMachine : MonoBehaviour
    {
        private enum EnemyAIState
        {
            Idle,
            Chase,
            Attack,
            Retreat
        }

        [SerializeField, Min(0.05f)] private float _destinationRefreshInterval = 0.15f;

        private EnemyAIState _currentState;
        private Enemy _owner;
        private EnemyConfig _config;
        private EnemyMovementSystem _movementSystem;
        private EnemySkillSystem _skillSystem;
        private PlayerService _playerService;
        private Transform _target;
        private bool _initialized;
        private bool _hasAggro;
        private float _stateElapsed;
        private float _nextDestinationRefreshTime;

        public void Initialize(Enemy owner)
        {
            _owner = owner;
            _config = owner != null ? owner.Config : null;
            _movementSystem = owner != null ? owner.MovementSystem : null;
            _skillSystem = owner != null ? owner.SkillSystem : null;
            _initialized = owner != null;
            _hasAggro = false;
            _target = null;
            ResolveServices();
            ChangeState(EnemyAIState.Idle);
        }

        private void Update()
        {
            if (!_initialized || _owner == null || _config == null || _movementSystem == null || _skillSystem == null)
                return;

            if (_owner.HealthSystem.IsDead)
                return;

            ResolveServices();
            _target = _playerService?.Player != null ? _playerService.Player.transform : null;

            if (!HasValidTarget())
            {
                _hasAggro = false;
                ChangeState(EnemyAIState.Idle);
                _movementSystem.StopMovement();
                return;
            }

            float distanceToTarget = GetTargetDistance();
            UpdateAggro(distanceToTarget);

            if (!_hasAggro)
            {
                ChangeState(EnemyAIState.Idle);
                _movementSystem.StopMovement();
                return;
            }

            _stateElapsed += Time.deltaTime;

            switch (_currentState)
            {
                case EnemyAIState.Idle:
                    TickIdle(distanceToTarget);
                    break;

                case EnemyAIState.Chase:
                    TickChase(distanceToTarget);
                    break;

                case EnemyAIState.Attack:
                    TickAttack(distanceToTarget);
                    break;

                case EnemyAIState.Retreat:
                    TickRetreat(distanceToTarget);
                    break;
            }
        }

        private void TickIdle(float distanceToTarget)
        {
            if (!_hasAggro)
                return;

            ChangeState(distanceToTarget <= GetAttackRangeThreshold() && !CanUsePrimarySkillWhileMoving()
                ? EnemyAIState.Attack
                : EnemyAIState.Chase);
        }

        private void TickChase(float distanceToTarget)
        {
            if (distanceToTarget <= GetAttackRangeThreshold())
            {
                if (CanUsePrimarySkillWhileMoving())
                {
                    if (TryUsePrimarySkillWhileMoving())
                    {
                        if (ShouldRetreat())
                            ChangeState(EnemyAIState.Retreat);

                        return;
                    }

                    if (!IsPrimarySkillReady() && ShouldRetreat())
                    {
                        ChangeState(EnemyAIState.Retreat);
                        return;
                    }
                }
                else
                {
                    ChangeState(EnemyAIState.Attack);
                    return;
                }

                RefreshDestination(() => _movementSystem.MoveTo(_target.position, GetDesiredEngagementDistance()));
                _movementSystem.FaceTowards(_target.position);
                return;
            }

            RefreshDestination(() => _movementSystem.MoveTo(_target.position, GetDesiredEngagementDistance()));
            _movementSystem.FaceTowards(_target.position);
        }

        private void TickAttack(float distanceToTarget)
        {
            if (CanUsePrimarySkillWhileMoving())
            {
                ChangeState(EnemyAIState.Chase);
                return;
            }

            _movementSystem.StopMovement();
            _movementSystem.FaceTowards(_target.position);

            if (_movementSystem.IsMoving)
                return;

            if (distanceToTarget > GetAttackRangeThreshold())
            {
                ChangeState(EnemyAIState.Chase);
                return;
            }

            int primarySkillSlot = GetPrimarySkillSlot();
            if (!_skillSystem.HasSkill(primarySkillSlot))
                return;

            if (_skillSystem.IsReady(primarySkillSlot) && _skillSystem.TryActivateAtTarget(primarySkillSlot, _target))
            {
                ChangeState(ShouldRetreat() ? EnemyAIState.Retreat : EnemyAIState.Attack);
                return;
            }

            if (!_skillSystem.IsReady(primarySkillSlot))
                ChangeState(EnemyAIState.Retreat);
        }

        private void TickRetreat(float distanceToTarget)
        {
            _movementSystem.MoveAwayFrom(_target.position, GetRetreatDistance());

            int primarySkillSlot = GetPrimarySkillSlot();
            if (_skillSystem.HasSkill(primarySkillSlot) &&
                distanceToTarget <= GetAttackRangeThreshold())
            {
                if (_skillSystem.CanUseWhileMoving(primarySkillSlot))
                {
                    if (TryUsePrimarySkillWhileMoving())
                        return;
                }
                else if (_skillSystem.IsReady(primarySkillSlot))
                {
                    ChangeState(EnemyAIState.Attack);
                    return;
                }
            }

            if (_stateElapsed >= Mathf.Max(0f, _config.RetreatDuration))
            {
                ChangeState(distanceToTarget <= GetAttackRangeThreshold() && !CanUsePrimarySkillWhileMoving()
                    ? EnemyAIState.Attack
                    : EnemyAIState.Chase);
            }
        }

        private void UpdateAggro(float distanceToTarget)
        {
            float aggroRange = Mathf.Max(_config.DetectionRange, _config.AggroRange);

            if (_hasAggro)
            {
                if (distanceToTarget > aggroRange)
                    _hasAggro = false;

                return;
            }

            if (distanceToTarget <= Mathf.Max(0f, _config.DetectionRange))
                _hasAggro = true;
        }

        private void ResolveServices()
        {
            if (ServiceLocator.Instance == null || !ServiceLocator.Instance.Has<PlayerService>())
                return;

            _playerService ??= ServiceLocator.Instance.Get<PlayerService>();
        }

        private bool HasValidTarget() =>
            _playerService?.Player != null &&
            _target != null &&
            !_playerService.Player.HealthSystem.IsDead;

        private float GetTargetDistance()
        {
            if (_owner == null || _target == null)
                return float.MaxValue;

            Vector3 offset = _target.position - _owner.transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        private float GetDesiredEngagementDistance()
        {
            float attackRange = Mathf.Max(0.5f, _owner.StatsSystem.AttackRange.Value);
            return _config.CombatRole == EnemyCombatRole.Melee
                ? Mathf.Max(0.25f, attackRange - Mathf.Max(0.05f, _config.RangeTolerance * 0.5f))
                : attackRange;
        }

        private float GetAttackRangeThreshold() => Mathf.Max(0.5f, _owner.StatsSystem.AttackRange.Value) + Mathf.Max(0f, _config.RangeTolerance);

        private float GetRetreatDistance()
        {
            float attackRange = Mathf.Max(0.5f, _owner.StatsSystem.AttackRange.Value);
            float configuredDistance = Mathf.Max(0f, _config.RetreatDistance);

            return _config.CombatRole == EnemyCombatRole.Ranged
                ? Mathf.Max(configuredDistance, attackRange * 0.5f)
                : configuredDistance;
        }

        private bool ShouldRetreat() => GetRetreatDistance() > 0f && _config.RetreatDuration > 0f;

        private int GetPrimarySkillSlot() => Mathf.Clamp(_config.PrimarySkillSlot, 1, 4);

        private bool IsPrimarySkillReady()
        {
            int primarySkillSlot = GetPrimarySkillSlot();
            return _skillSystem.HasSkill(primarySkillSlot) && _skillSystem.IsReady(primarySkillSlot);
        }

        private bool CanUsePrimarySkillWhileMoving()
        {
            int primarySkillSlot = GetPrimarySkillSlot();
            return _skillSystem.HasSkill(primarySkillSlot) && _skillSystem.CanUseWhileMoving(primarySkillSlot);
        }

        private bool TryUsePrimarySkillWhileMoving()
        {
            int primarySkillSlot = GetPrimarySkillSlot();
            if (!_skillSystem.HasSkill(primarySkillSlot) || !_skillSystem.CanUseWhileMoving(primarySkillSlot))
                return false;

            if (!_skillSystem.IsReady(primarySkillSlot))
                return false;

            _movementSystem.FaceTowards(_target.position);
            return _skillSystem.TryActivateAtTarget(primarySkillSlot, _target);
        }

        private void RefreshDestination(System.Action setDestination)
        {
            if (Time.time < _nextDestinationRefreshTime)
                return;

            _nextDestinationRefreshTime = Time.time + _destinationRefreshInterval;
            setDestination?.Invoke();
        }

        private void ChangeState(EnemyAIState nextState)
        {
            if (_currentState == nextState)
                return;

            _currentState = nextState;
            _stateElapsed = 0f;
            _nextDestinationRefreshTime = 0f;

            if (nextState == EnemyAIState.Idle || nextState == EnemyAIState.Attack)
                _movementSystem?.StopMovement();
        }
    }
}