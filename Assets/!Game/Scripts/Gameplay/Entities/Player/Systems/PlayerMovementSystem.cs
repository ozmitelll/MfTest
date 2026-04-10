using _Game.Scripts.Configs;
using _Game.Scripts.Gameplay.Systems.Health;
using _Game.Scripts.Gameplay.Systems.Stats;
using _Game.Scripts.Gameplay.Systems.StatusEffects;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Entities.Player.Systems
{
    public class PlayerMovementSystem : MonoBehaviour
    {
        [Header("Gravity")]
        [SerializeField] private float _gravity = 30f;
        [SerializeField] private float _groundedVerticalVelocity = -2f;
        [SerializeField] private float _maxFallSpeed = 50f;

        [Header("Fall Recovery")]
        [SerializeField] private float _fallRespawnDistance = 8f;
        [SerializeField] private float _respawnVerticalOffset = 0.25f;

        private InputSystem_Actions.PlayerActions _actions;
        private CharacterController               _controller;
        private HealthSystem                      _health;
        private StatsSystem                       _stats;
        private StatusEffectSystem                _statusEffects;
        private float                             _verticalVelocity;
        private Vector3                           _lastGroundedPosition;
        private Vector3                           _fallStartPosition;
        private bool                              _isRecoveringFromFall;

        public void Initialize(InputSystem_Actions.PlayerActions actions, StatsSystem stats, PlayerConfig config)
        {
            _actions    = actions;
            _controller = GetComponent<CharacterController>();
            _health     = GetComponent<HealthSystem>();
            _stats      = stats;
            _statusEffects = GetComponent<StatusEffectSystem>();
            ApplyConfig(config);
            ResetMotionState(transform.position);
        }

        private void Update()
        {
            if (_health?.IsDead == true)
                return;

            bool wasGrounded = _controller.isGrounded;
            if (wasGrounded)
                _lastGroundedPosition = transform.position;

            Vector3 move = CalculateHorizontalMovement();
            move.y = CalculateVerticalMovement();

            _controller.Move(move);
            UpdateFallRecovery(wasGrounded);
        }

        public void TeleportTo(Vector3 position) => TeleportTo(position, transform.rotation);

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            _controller.enabled = true;

            ResetMotionState(position);
        }

        private Vector3 CalculateHorizontalMovement()
        {
            if (_statusEffects?.BlocksMovement == true)
                return Vector3.zero;

            Vector2 input = _actions.Move.ReadValue<Vector2>();
            float moveSpeed = _stats.MoveSpeed.Value * (_statusEffects?.GetMoveSpeedMultiplier() ?? 1f);
            return new Vector3(input.x, 0f, input.y) * (moveSpeed * Time.deltaTime);
        }

        private float CalculateVerticalMovement()
        {
            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = _groundedVerticalVelocity;
            else
                _verticalVelocity = Mathf.Max(_verticalVelocity - _gravity * Time.deltaTime, -_maxFallSpeed);

            return _verticalVelocity * Time.deltaTime;
        }

        private void UpdateFallRecovery(bool wasGrounded)
        {
            if (_controller.isGrounded)
            {
                _lastGroundedPosition = transform.position;
                _isRecoveringFromFall = false;
                return;
            }

            if (wasGrounded)
            {
                _fallStartPosition = _lastGroundedPosition;
                _isRecoveringFromFall = true;
            }

            if (_isRecoveringFromFall && transform.position.y <= _fallStartPosition.y - _fallRespawnDistance)
                RestoreToFallStart();
        }

        private void RestoreToFallStart()
        {
            Vector3 restorePosition = _fallStartPosition + Vector3.up * _respawnVerticalOffset;
            TeleportTo(restorePosition, transform.rotation);
        }

        private void ApplyConfig(PlayerConfig config)
        {
            if (config == null)
                return;

            _gravity = config.Gravity;
            _groundedVerticalVelocity = config.GroundedVerticalVelocity;
            _maxFallSpeed = config.MaxFallSpeed;
            _fallRespawnDistance = config.FallRespawnDistance;
            _respawnVerticalOffset = config.RespawnVerticalOffset;
        }

        private void ResetMotionState(Vector3 position)
        {
            _verticalVelocity = _groundedVerticalVelocity;
            _lastGroundedPosition = position;
            _fallStartPosition = position;
            _isRecoveringFromFall = false;
        }
    }
}
