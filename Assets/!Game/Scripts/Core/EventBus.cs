using System;
using System.Collections.Generic;

namespace _Game.Scripts.Core
{
    public static class EventBus
    {
        static readonly Dictionary<Type, List<Delegate>> _subscribers = new();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);

            if (!_subscribers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _subscribers[type] = list;
            }

            list.Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);

            if (_subscribers.TryGetValue(type, out var list))
                list.Remove(handler);
        }

        public static void Publish<T>(T evt) where T : struct
        {
            var type = typeof(T);

            if (!_subscribers.TryGetValue(type, out var list))
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] is Action<T> action)
                    action.Invoke(evt);
            }
        }

        public static void Clear()
        {
            _subscribers.Clear();
        }
    }
}
