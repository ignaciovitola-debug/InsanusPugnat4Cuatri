using System.Collections.Generic;
using UnityEngine;

namespace GladiusAI
{
    // Fabrica de gladiadores: crea el prefab que corresponde a cada tipo.
    public class GladiatorFactory : MonoBehaviour
    {
        [System.Serializable]
        public struct KindPrefab
        {
            public GladiatorKind kind;
            public GladiatorNPC prefab;
        }

        [Header("Prefab de cada tipo de gladiador")]
        [SerializeField] private KindPrefab[] prefabs = new KindPrefab[0];

        private readonly Dictionary<GladiatorKind, GladiatorNPC> prefabByKind = new Dictionary<GladiatorKind, GladiatorNPC>();

        private void Awake()
        {
            foreach (var entry in prefabs)
                prefabByKind[entry.kind] = entry.prefab;
        }

        public GladiatorNPC Create(GladiatorKind kind, Vector3 position, Quaternion rotation)
        {
            if (!prefabByKind.TryGetValue(kind, out var prefab) || prefab == null)
            {
                Debug.LogError($"[GladiatorFactory] Falta asignar un prefab para {kind} en la tabla del Inspector.");
                return null;
            }

            return Instantiate(prefab, position, rotation);
        }
    }
}
