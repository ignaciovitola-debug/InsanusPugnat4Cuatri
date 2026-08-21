using System.Collections.Generic;

namespace GladiusAI
{
    /// <summary>
    /// El Blackboard es la "memoria" del NPC: ahí se guarda todo lo que
    /// el árbol necesita leer o escribir (a quién está persiguiendo,
    /// cuánta vida le queda, etc). Cada gladiador tiene el suyo propio.
    /// </summary>
    public class Blackboard
    {
        private readonly Dictionary<string, object> data = new Dictionary<string, object>(16);

        public void Set<T>(string key, T value) => data[key] = value;

        public T Get<T>(string key, T defaultValue = default)
        {
            if (data.TryGetValue(key, out var value) && value is T typed)
                return typed;
            return defaultValue;
        }

        public bool Has(string key) => data.ContainsKey(key);
    }
}
