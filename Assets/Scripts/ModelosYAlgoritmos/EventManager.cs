using System;
using System.Collections.Generic;

namespace GladiusAI
{
    public static class EventManager
    {
        private static readonly Dictionary<Type, Delegate> listeners = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> listener) where T : struct
        {
            var type = typeof(T);
            listeners[type] = listeners.TryGetValue(type, out var existing)
                ? Delegate.Combine(existing, listener)
                : listener;
        }

        public static void Unsubscribe<T>(Action<T> listener) where T : struct
        {
            var type = typeof(T);
            if (!listeners.TryGetValue(type, out var existing)) return;

            var remaining = Delegate.Remove(existing, listener);
            if (remaining == null)
                listeners.Remove(type);
            else
                listeners[type] = remaining;
        }

        public static void Raise<T>(T eventData) where T : struct
        {
            if (listeners.TryGetValue(typeof(T), out var handler))
                ((Action<T>)handler).Invoke(eventData);
        }
    }
}
