using UnityEngine;

namespace _Game.Scripts.Gameplay.Entities.Player.Systems
{
    public class PlayerMovementSystem : MonoBehaviour
    {
        private const float Speed = 5f;

        private InputSystem_Actions.PlayerActions _actions;
        private CharacterController _controller;

        public void Initialize(InputSystem_Actions.PlayerActions actions)
        {
            _actions = actions;
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Vector2 input = _actions.Move.ReadValue<Vector2>();
            Vector3 move = new Vector3(input.x, 0f, input.y) * (Speed * Time.deltaTime);
            _controller.Move(move);
        }
    }
}
