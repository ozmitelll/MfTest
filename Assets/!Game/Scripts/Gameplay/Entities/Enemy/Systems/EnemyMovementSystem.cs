using _Game.Scripts.Gameplay.Systems.StatusEffects;
using _Game.Scripts.Gameplay.Systems.Stats;
using UnityEngine;
using UnityEngine.AI;

namespace _Game.Scripts.Gameplay.Entities.Enemy.Systems
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMovementSystem : MonoBehaviour
    {
        [SerializeField, Min(90f)] private float _rotationSpeed = 540f;
        [SerializeField, Min(0.5f)] private float _navMeshSnapDistance = 2f;

        private Enemy _owner;
        private NavMeshAgent _agent;
        private StatsSystem _stats;
        private StatusEffectSystem _statusEffects;
        private bool _initialized;

        public bool IsMoving
        {
            get
            {
                if (_agent == null || !_agent.isOnNavMesh)
                    return false;

                return _agent.velocity.sqrMagnitude > 0.01f ||
                       (!_agent.isStopped && _agent.desiredVelocity.sqrMagnitude > 0.01f);
            }
        }

        public void Initialize(Enemy owner)
        {
            _owner = owner;
            _agent = GetComponent<NavMeshAgent>() ?? gameObject.AddComponent<NavMeshAgent>();
            _stats = owner != null ? owner.StatsSystem : null;
            _statusEffects = owner != null ? owner.StatusEffectSystem : null;
            _initialized = owner != null;

            if (_agent != null)
            {
                _agent.updateRotation = false;
                _agent.updateUpAxis = true;
            }

            EnsureAgentOnNavMesh();
            SyncMovementStats();
        }

        private void Update()
        {
            if (!_initialized || _owner == null || _agent == null)
                return;

            EnsureAgentOnNavMesh();
            SyncMovementStats();

            if (ShouldBlockMovement())
            {
                StopMovement();
                return;
            }

            if (_agent.isOnNavMesh && _agent.desiredVelocity.sqrMagnitude > 0.01f)
                RotateTowards(_agent.desiredVelocity);
        }

        public void MoveTo(Vector3 destination, float stoppingDistance)
        {
            if (!CanUseAgent())
                return;

            _agent.stoppingDistance = Mathf.Max(0f, stoppingDistance);
            _agent.isStopped = false;
            _agent.SetDestination(destination);
        }

        public void MoveAwayFrom(Vector3 threatPosition, float retreatDistance)
        {
            if (_owner == null)
                return;

            Vector3 awayDirection = _owner.transform.position - threatPosition;
            awayDirection.y = 0f;

            if (awayDirection.sqrMagnitude < 0.001f)
                awayDirection = -_owner.transform.forward;

            awayDirection.Normalize();

            float desiredDistance = Mathf.Max(0.25f, retreatDistance);
            Vector3 desiredPosition = _owner.transform.position + awayDirection * desiredDistance;
            if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, desiredDistance, NavMesh.AllAreas))
                desiredPosition = hit.position;

            MoveTo(desiredPosition, 0.05f);
            FaceTowards(threatPosition);
        }

        public void StopMovement()
        {
            if (_agent == null || !_agent.isOnNavMesh)
                return;

            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
            if (_agent.hasPath)
                _agent.ResetPath();
        }

        public void FaceTowards(Vector3 worldPosition)
        {
            if (_owner == null)
                return;

            RotateTowards(worldPosition - _owner.transform.position);
        }

        private bool CanUseAgent()
        {
            if (!_initialized || _owner == null || _agent == null)
                return false;

            EnsureAgentOnNavMesh();
            SyncMovementStats();
            return _agent.isOnNavMesh && !ShouldBlockMovement();
        }

        private bool ShouldBlockMovement() =>
            _owner == null ||
            _owner.HealthSystem.IsDead ||
            _statusEffects?.BlocksMovement == true;

        private void EnsureAgentOnNavMesh()
        {
            if (_agent == null || _agent.isOnNavMesh)
                return;

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, _navMeshSnapDistance, NavMesh.AllAreas))
                _agent.Warp(hit.position);
        }

        private void SyncMovementStats()
        {
            if (_agent == null || _stats == null)
                return;

            float moveSpeedMultiplier = _statusEffects?.GetMoveSpeedMultiplier() ?? 1f;
            _agent.speed = Mathf.Max(0f, _stats.MoveSpeed.Value * moveSpeedMultiplier);
        }

        private void RotateTowards(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime);
        }
    }
}