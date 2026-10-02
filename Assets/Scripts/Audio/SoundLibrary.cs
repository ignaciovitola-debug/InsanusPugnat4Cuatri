using System.Collections.Generic;
using UnityEngine;

namespace GladiusAI
{
    /// <summary>
    /// Catálogo de sonidos: a cada SoundId le asigna uno o varios clips y cómo reproducirlos.
    /// Se edita desde el Inspector (Assets/Resources/SoundLibrary). Si un sonido tiene varios clips,
    /// cada vez se elige uno al azar, así los golpes repetidos no suenan siempre igual.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Insanus Pugnat/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public SoundId id;
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume = 1f;
            [Tooltip("Variación aleatoria de tono (1 = original). Un rango chico evita que los sonidos repetidos se noten.")]
            public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

            public AudioClip PickClip()
                => clips == null || clips.Length == 0 ? null : clips[Random.Range(0, clips.Length)];

            public float PickPitch() => Random.Range(pitchRange.x, pitchRange.y);
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<SoundId, Entry> lookup;

        public bool TryGet(SoundId id, out Entry entry)
        {
            if (lookup == null)
                BuildLookup();
            return lookup.TryGetValue(id, out entry);
        }

        private void BuildLookup()
        {
            lookup = new Dictionary<SoundId, Entry>(entries.Count);
            foreach (var entry in entries)
                lookup[entry.id] = entry;
        }

        // Si se edita la librería en Play desde el Inspector, se reconstruye el índice.
        private void OnValidate() => lookup = null;
    }
}
