using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GladiusAI
{
    /// Punto único para reproducir sonido en todo el juego. Uso:
    ///   SoundManager.Play(SoundId.SwordHit);                              // efecto corto
    ///   SoundManager.PlayAmbience(SoundId.Rain, SoundId.DistantStadium);  // fondo en loop, una o varias capas
    ///   SoundManager.StopAmbience();
    ///
    /// Patrón Singleton "perezoso": el objeto se crea solo la primera vez que alguien lo usa (aunque se
    /// arranque el juego directo desde una arena) y sobrevive a los cambios de escena (DontDestroyOnLoad).
    /// Los clips se configuran en Assets/Resources/SoundLibrary.
    /// El volumen general lo maneja el menú de Opciones (AudioListener.volume), así que acá no se toca.
    public class SoundManager : MonoBehaviour
    {
        private const string LibraryResourcePath = "SoundLibrary";
        // Cantidad de efectos que pueden sonar a la vez. Si se piden más, se reutiliza la fuente del
        // más viejo (lo corta). Las fuentes se crean una sola vez y se reciclan: no se crean en cada golpe.
        private const int EffectVoices = 8;
        private const float DefaultFadeSeconds = 1.5f;

        private static SoundManager instance;
        private static bool isQuitting;

        private SoundLibrary library;
        private AudioSource[] effectSources;
        private int nextEffectSource; // índice circular: 0, 1, ..., 7, 0, 1...

        // Ambiente: cada capa del fondo usa su propia AudioSource. Al cambiar de fondo, las capas viejas
        // bajan de volumen mientras las nuevas suben (crossfade). Las fuentes que terminan de apagarse
        // quedan libres para el próximo fondo.
        private readonly List<AudioSource> ambienceLayers = new List<AudioSource>();
        private readonly List<AudioSource> fadingOutLayers = new List<AudioSource>();
        private readonly Stack<AudioSource> freeAmbienceSources = new Stack<AudioSource>();
        // Lista de fundidos reutilizada en cada cambio de fondo (no se crea una lista nueva cada vez).
        private readonly List<VolumeFade> fades = new List<VolumeFade>();
        private SoundId[] currentAmbience = System.Array.Empty<SoundId>();
        private Coroutine ambienceFade;

        /// Una fuente que va de un volumen a otro durante el fundido.
        private struct VolumeFade
        {
            public AudioSource Source;
            public float From;
            public float To;
        }

        //  API pública (estática, para llamarla desde cualquier lado sin buscar el objeto)

        public static void Play(SoundId id)
        {
            var manager = Instance;
            if (manager != null)
                manager.PlayEffect(id);
        }

        /// Cambia el fondo por estas capas (sonando juntas, en loop). Si ya es el mismo fondo, no hace nada.
        public static void PlayAmbience(params SoundId[] layers)
            => PlayAmbience(DefaultFadeSeconds, layers);

        public static void PlayAmbience(float fadeSeconds, params SoundId[] layers)
        {
            var manager = Instance;
            if (manager != null)
                manager.CrossfadeAmbience(layers ?? System.Array.Empty<SoundId>(), fadeSeconds);
        }

        public static void StopAmbience(float fadeSeconds = DefaultFadeSeconds)
            => PlayAmbience(fadeSeconds);

        //  Singleton

        private static SoundManager Instance
        {
            get
            {
                // isQuitting evita crear un SoundManager nuevo mientras el juego se está cerrando
                // (algún objeto podría pedir un sonido en su OnDestroy).
                if (instance == null && !isQuitting)
                {
                    var go = new GameObject(nameof(SoundManager));
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<SoundManager>();
                }
                return instance;
            }
        }

        // Si "Enter Play Mode" no recarga el dominio, los static sobreviven entre sesiones de Play:
        // los reiniciamos al arrancar.
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
            source.spatialBlend = 0f; // 2D: se escucha igual sin importar dónde esté la cámara
            return source;
        }

        //  Efectos

        private void PlayEffect(SoundId id)
        {
            if (!TryGetEntry(id, out var entry)) return;

            var clip = entry.PickClip();
            if (clip == null) return;

            // Tomamos la siguiente fuente en ronda: si estaba sonando algo viejo, se reemplaza.
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

            // Si había un fundido a medias, lo cortamos: el nuevo se encarga de todo.
            if (ambienceFade != null)
                StopCoroutine(ambienceFade);
            fades.Clear();

            // Todo lo que suena se apaga, incluido lo que quedó a medio apagar de un cambio anterior.
            fadingOutLayers.AddRange(ambienceLayers);
            ambienceLayers.Clear();
            foreach (var source in fadingOutLayers)
                fades.Add(new VolumeFade { Source = source, From = source.volume, To = 0f });

            // Las capas nuevas arrancan en volumen 0 y suben hasta el de la SoundLibrary.
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

            ambienceFade = StartCoroutine(RunFades(fadeSeconds));
        }

        private IEnumerator RunFades(float seconds)
        {
            // Tiempo real (unscaled): el fundido sigue aunque el juego esté en pausa (timeScale = 0),
            // por ejemplo mientras habla el instructor del tutorial.
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float progress = t / seconds;
                foreach (var fade in fades)
                    fade.Source.volume = Mathf.Lerp(fade.From, fade.To, progress);
                yield return null;
            }

            foreach (var fade in fades)
                fade.Source.volume = fade.To;

            // Las fuentes apagadas quedan libres para reutilizarse en el próximo fondo.
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
