using _Game.Scripts.Gameplay.Systems.Stats;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Entities.Player.Systems
{
    public class PlayerMovementSystem : MonoBehaviour
    {
        private InputSystem_Actions.PlayerActions _actions;
        private CharacterController               _controller;
        private StatsSystem                       _stats;

        public void Initialize(InputSystem_Actions.PlayerActions actions, StatsSystem stats)
        {
            _actions    = actions;
            _controller = GetComponent<CharacterController>();
            _stats      = stats;
        }

        private void Update()
        {
            Vector2 input = _actions.Move.ReadValue<Vector2>();
            Vector3 move  = new Vector3(input.x, 0f, input.y) * (_stats.MoveSpeed.Value * Time.deltaTime);
            _controller.Move(move);
        }
    }
}
