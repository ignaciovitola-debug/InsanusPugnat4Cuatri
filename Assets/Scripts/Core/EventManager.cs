using System;
using System.Collections.Generic;

namespace GladiusAI
{
    /// <summary>
    /// Event Manager estático y desacoplado: cualquier sistema puede publicar
    /// (Raise) o suscribirse (Subscribe) a un tipo de evento sin conocerse
    /// entre sí — ni el que publica sabe quién escucha, ni el que escucha
    /// sabe quién publicó. Mismo patrón visto en Modelos y Algoritmos
    /// (Clase 3, "Struct EventManager"): eventos como struct, diccionario
    /// de delegados por tipo.
    /// </summary>
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
