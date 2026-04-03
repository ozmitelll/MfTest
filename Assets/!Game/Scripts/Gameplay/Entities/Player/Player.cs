using _Game.Scripts.Gameplay.Entities.Player.Systems;
using UnityEngine;

namespace _Game.Scripts.Gameplay.Entities.Player
{
    public class Player : Entity
    {
        private InputSystem_Actions _input;
        [SerializeField] private PlayerMovementSystem _movementSystem;

        private void Awake()
        {
            _input = new InputSystem_Actions();
            _movementSystem.Initialize(_input.Player);
        }

        private void OnEnable()
        {
            _input.Player.Enable();
        }

        private void OnDisable()
        {
            _input.Player.Disable();
        }
    }
}