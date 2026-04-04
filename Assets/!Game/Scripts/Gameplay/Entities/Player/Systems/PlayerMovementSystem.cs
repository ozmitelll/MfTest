using _Game.Scripts.Gameplay.Systems.Stats;
using _Game.Scripts.Gameplay.Systems.StatusEffects;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Entities.Player.Systems
{
    public class PlayerMovementSystem : MonoBehaviour
    {
        private InputSystem_Actions.PlayerActions _actions;
        private CharacterController               _controller;
        private StatsSystem                       _stats;
        private StatusEffectSystem                _statusEffects;

        public void Initialize(InputSystem_Actions.PlayerActions actions, StatsSystem stats)
        {
            _actions    = actions;
            _controller = GetComponent<CharacterController>();
            _stats      = stats;
            _statusEffects = GetComponent<StatusEffectSystem>();
        }

        private void Update()
        {
            if (_statusEffects?.BlocksMovement == true)
                return;

            Vector2 input = _actions.Move.ReadValue<Vector2>();
            float moveSpeed = _stats.MoveSpeed.Value * (_statusEffects?.GetMoveSpeedMultiplier() ?? 1f);
            Vector3 move  = new Vector3(input.x, 0f, input.y) * (moveSpeed * Time.deltaTime);
            _controller.Move(move);
        }
    }
}
