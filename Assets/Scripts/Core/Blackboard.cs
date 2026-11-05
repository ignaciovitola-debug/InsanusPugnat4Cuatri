using System.Collections.Generic;

namespace GladiusAI
{
    // Memoria del NPC: guarda lo que el árbol necesita leer o escribir (objetivo, vida...).
    // Cada gladiador tiene el suyo.
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
