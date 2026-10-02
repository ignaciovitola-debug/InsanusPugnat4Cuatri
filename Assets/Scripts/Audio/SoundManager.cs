using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GladiusAI
{
    /// <summary>
    /// Punto único para reproducir sonido. Uso:
    ///   SoundManager.Play(SoundId.SwordHit);                          // efecto corto
    ///   SoundManager.PlayAmbience(SoundId.Rain, SoundId.DistantStadium); // fondo en loop, una o varias capas
    ///   SoundManager.StopAmbience();
    /// Se crea solo la primera vez que se lo usa (aunque se arranque directo desde una arena) y
    /// sobrevive a los cambios de escena. Los clips se configuran en Assets/Resources/SoundLibrary.
    /// El volumen general lo controla Opciones (AudioListener.volume), así que acá no se toca.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        private const string LibraryResourcePath = "SoundLibrary";
        // Efectos que pueden sonar a la vez; si se piden más, se corta el más viejo.
        private const int EffectVoices = 8;
        private const float DefaultFadeSeconds = 1.5f;

        private static SoundManager instance;
        private static bool isQuitting;

        private SoundLibrary library;
        private AudioSource[] effectSources;
        private int nextEffectSource;

        // Ambiente: cada capa usa su propia fuente. Al cambiar de ambiente, las capas viejas
        // se apagan mientras las nuevas se prenden (crossfade).
        private readonly List<AudioSource> ambienceLayers = new List<AudioSource>();
        private readonly List<AudioSource> fadingOutLayers = new List<AudioSource>();
        private readonly Stack<AudioSource> freeAmbienceSources = new Stack<AudioSource>();
        private SoundId[] currentAmbience = new SoundId[0];
        private Coroutine ambienceFade;

        private struct VolumeFade
        {
            public AudioSource Source;
            public float From;
            public float To;
        }

        public static void Play(SoundId id)
        {
            var manager = Instance;
            if (manager != null)
                manager.PlayEffect(id);
        }

        /// <summary>Cambia el fondo por estas capas (sonando juntas, en loop). Si ya es el mismo fondo, no hace nada.</summary>
        public static void PlayAmbience(params SoundId[] layers)
            => PlayAmbience(DefaultFadeSeconds, layers);

        public static void PlayAmbience(float fadeSeconds, params SoundId[] layers)
        {
            var manager = Instance;
            if (manager != null)
                manager.CrossfadeAmbience(layers ?? new SoundId[0], fadeSeconds);
        }

        public static void StopAmbience(float fadeSeconds = DefaultFadeSeconds)
            => PlayAmbience(fadeSeconds);

        private static SoundManager Instance
        {
            get
            {
                if (instance == null && !isQuitting)
                {
                    var go = new GameObject(nameof(SoundManager));
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<SoundManager>();
                }
                return instance;
            }
        }

        // Con "Enter Play Mode" sin recarga de dominio, los static sobreviven entre sesiones de Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            isQuitting = false;
        }

        private void Awake()
        {
            library = Resources.Load<SoundLibrary>(LibraryResourcePath);
            if (library == null)
                Debug.LogWarning($"[SoundManager] No se encontró Resources/{LibraryResourcePath}: el juego va a estar en silencio.");

            effectSources = new AudioSource[EffectVoices];
            for (int i = 0; i < effectSources.Length; i++)
                effectSources[i] = CreateSource(loop: false);
        }

        private void OnApplicationQuit() => isQuitting = true;

        private AudioSource CreateSource(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f; // 2D: en mobile se escucha igual desde cualquier lado de la arena
            return source;
        }

        //  Efectos

        private void PlayEffect(SoundId id)
        {
            if (!TryGetEntry(id, out var entry)) return;

            var clip = entry.PickClip();
            if (clip == null) return;

            var source = effectSources[nextEffectSource];
            nextEffectSource = (nextEffectSource + 1) % effectSources.Length;

            source.clip = clip;
            source.volume = entry.volume;
            source.pitch = entry.PickPitch();
            source.Play();
        }

        //  Ambiente

        private void CrossfadeAmbience(SoundId[] layers, float fadeSeconds)
        {
            if (IsCurrentAmbience(layers)) return;
            currentAmbience = (SoundId[])layers.Clone();

            var fades = new List<VolumeFade>();

            // Todo lo que suena (incluido lo que quedó a medio apagar de un cambio anterior) se apaga.
            fadingOutLayers.AddRange(ambienceLayers);
            ambienceLayers.Clear();
            foreach (var source in fadingOutLayers)
                fades.Add(new VolumeFade { Source = source, From = source.volume, To = 0f });

            foreach (var id in layers)
            {
                if (!TryGetEntry(id, out var entry)) continue;

                var clip = entry.PickClip();
                if (clip == null) continue;

                var source = freeAmbienceSources.Count > 0 ? freeAmbienceSources.Pop() : CreateSource(loop: true);
                source.clip = clip;
                source.pitch = 1f;
                source.volume = 0f;
                source.Play();

                ambienceLayers.Add(source);
                fades.Add(new VolumeFade { Source = source, From = 0f, To = entry.volume });
            }

            if (ambienceFade != null)
                StopCoroutine(ambienceFade);
            ambienceFade = StartCoroutine(RunFades(fades, fadeSeconds));
        }

        private IEnumerator RunFades(List<VolumeFade> fades, float seconds)
        {
            // Tiempo real: el fundido no se frena si el juego está en pausa (timeScale = 0).
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = t / seconds;
                foreach (var fade in fades)
                    fade.Source.volume = Mathf.Lerp(fade.From, fade.To, k);
                yield return null;
            }

            foreach (var fade in fades)
                fade.Source.volume = fade.To;

            foreach (var source in fadingOutLayers)
            {
                source.Stop();
                source.clip = null;
                freeAmbienceSources.Push(source);
            }
            fadingOutLayers.Clear();
            ambienceFade = null;
        }

        private bool IsCurrentAmbience(SoundId[] layers)
        {
            if (layers.Length != currentAmbience.Length) return false;
            for (int i = 0; i < layers.Length; i++)
                if (layers[i] != currentAmbience[i]) return false;
            return true;
        }

        private bool TryGetEntry(SoundId id, out SoundLibrary.Entry entry)
        {
            entry = null;
            if (id == SoundId.None || library == null) return false;

            if (library.TryGet(id, out entry)) return true;

            Debug.LogWarning($"[SoundManager] '{id}' no tiene clips asignados en la SoundLibrary.");
            return false;
        }
    }
}
