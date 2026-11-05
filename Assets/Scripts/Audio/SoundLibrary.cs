using System.Collections.Generic;
using UnityEngine;

namespace GladiusAI
{
    // Catálogo de sonidos: a cada SoundId le asigna clips, volumen y variación de tono.
    // Es un ScriptableObject: se edita desde el Inspector (Resources/SoundLibrary).
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Insanus Pugnat/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        // Configuración de un sonido.
        [System.Serializable]
        public class Entry
        {
            public SoundId id;
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume = 1f;
            [Tooltip("Variación aleatoria de tono (1 = original).")]
            public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

            public AudioClip PickClip()
                => clips == null || clips.Length == 0 ? null : clips[Random.Range(0, clips.Length)];

            public float PickPitch() => Random.Range(pitchRange.x, pitchRange.y);
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        // Índice por posición del array (el número del SoundId): más rápido que un Dictionary.
        private Entry[] byId;

        public bool TryGet(SoundId id, out Entry entry)
        {
            if (byId == null)
                BuildIndex();

            int index = (int)id;
            entry = index >= 0 && index < byId.Length ? byId[index] : null;
            return entry != null;
        }

        // Arma el índice una sola vez (la primera vez que se pide un sonido).
        private void BuildIndex()
        {
            int size = 0;
            foreach (var entry in entries)
                size = Mathf.Max(size, (int)entry.id + 1);

            byId = new Entry[size];
            foreach (var entry in entries)
                byId[(int)entry.id] = entry;
        }

        // OnValidate se ejecuta cada vez que se edita el asset en el Inspector: tiramos el índice para
        // que se vuelva a armar con los datos nuevos (sirve para ajustar sonidos con el juego corriendo).
        private void OnValidate() => byId = null;
    }
}
