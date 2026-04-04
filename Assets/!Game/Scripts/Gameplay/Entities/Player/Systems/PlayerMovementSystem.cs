using _Game.Scripts.Configs;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Entities.Player.Systems
{
    public class PlayerMovementSystem : MonoBehaviour
    {
        private InputSystem_Actions.PlayerActions _actions;
        private CharacterController _controller;
        private float _speed;

        public void Initialize(InputSystem_Actions.PlayerActions actions, PlayerConfig config)
        {
            _actions    = actions;
            _controller = GetComponent<CharacterController>();
            _speed      = config.MoveSpeed;
        }

        private void Update()
        {
            Vector2 input = _actions.Move.ReadValue<Vector2>();
            Vector3 move  = new Vector3(input.x, 0f, input.y) * (_speed * Time.deltaTime);
            _controller.Move(move);
        }
    }
}
