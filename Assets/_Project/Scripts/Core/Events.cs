using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Events
{
    public interface IEvent { }

    public static class EventBus<T> where T : IEvent
    {
        private static readonly Dictionary<Guid, Action<T>> _listeners = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            _listeners.Clear();
        }

        public static Guid Listen(Action<T> callback)
        {
            var guid = Guid.NewGuid();
            _listeners.Add(guid, callback);

            return guid;
        }

        public static bool TryCancel(Guid guid)
        {
            if (!_listeners.ContainsKey(guid)) return false;

            _listeners.Remove(guid);
            return true;
        }

        public static void Invoke(T data)
        {
            var garbage = new Stack<Guid>();
            foreach (var listener in _listeners)
            {
                if (listener.Value == null || listener.Value.Target.Equals(null))
                {
                    garbage.Push(listener.Key);
                    continue;
                }

                listener.Value(data);
            }

            while (garbage.Count > 0)
            {
                _listeners.Remove(garbage.Pop());
            }
        }
    }
}