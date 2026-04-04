using System;
using UnityEngine;

namespace _Game.Scripts.Core
{
    // Пробрасывает OnTriggerEnter/Exit наружу через события.
    // Используется там, где нужен отдельный GameObject с триггером.
    public class TriggerProxy : MonoBehaviour
    {
        public event Action<Collider> OnEntered;
        public event Action<Collider> OnExited;

        private void OnTriggerEnter(Collider other) => OnEntered?.Invoke(other);
        private void OnTriggerExit(Collider other)  => OnExited?.Invoke(other);
    }
}
