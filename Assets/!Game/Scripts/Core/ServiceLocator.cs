using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Core
{
    public class ServiceLocator : MonoBehaviour
    {
        public static ServiceLocator Instance { get; private set; }
        private readonly Dictionary<Type, IService> _services = new();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Register<T>(T service) where T : IService
        {
            _services[typeof(T)] = service;
        }

        public T Get<T>() where T : IService
        {
            if (_services.TryGetValue(typeof(T), out var service)) return (T)service;

            Debug.LogError($"Service of type {typeof(T)} not found.");
            return default;
        }
    
        public bool Has<T>() where T : IService => _services.ContainsKey(typeof(T));
    }
}
