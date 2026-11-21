using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GladiusAI
{
    // Da sonido y un pequeño "rebote" a todos los botones del juego, sin tocar las escenas.
    // Busca los botones nuevos cada tanto (los de los menus, la tienda y los que se crean por codigo).
    public class ButtonFeedback : MonoBehaviour
    {
        private const float ScanSeconds = 0.25f;
        private const float PunchScale = 0.9f;
        private const float PunchDown = 0.06f;
        private const float PunchUp = 0.1f;
        private const int SampleRate = 44100;

        private static ButtonFeedback instance;

        private sealed class PunchState
        {
            public Vector3 original;
            public Coroutine routine;
        }

        private readonly HashSet<int> hooked = new HashSet<int>();
        private readonly Dictionary<int, PunchState> running = new Dictionary<int, PunchState>();
        private AudioSource source;
        private AudioClip clip;
        private float nextScan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) return;

            var go = new GameObject("ButtonFeedback");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<ButtonFeedback>();
        }

        private void Awake()
        {
            clip = BuildClickClip();
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void Start() => Scan();
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Scan();

        private void Update()
        {
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + ScanSeconds;
            Scan();
        }

        // Los ids de botones que ya no existen se descartan solos al cambiar de escena.
        private void Scan()
        {
            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var button in buttons)
            {
                if (hooked.Add(button.GetInstanceID()))
                    button.onClick.AddListener(() => OnClicked(button));
            }

            if (hooked.Count > 2000)
                hooked.Clear();
        }

        private void OnClicked(Button button)
        {
            source.pitch = Random.Range(0.96f, 1.06f);
            source.PlayOneShot(clip, 0.7f * GameSettings.Sfx);

            if (button == null || !button.isActiveAndEnabled) return;

            // Si el boton todavia esta "apretandose" por un toque anterior, se lo devuelve a su tamaño
            // de verdad antes de empezar de nuevo (si no, achicaria cada vez mas).
            var target = button.transform;
            int id = target.GetInstanceID();
            if (running.TryGetValue(id, out var state))
            {
                StopCoroutine(state.routine);
                target.localScale = state.original;
            }
            else
            {
                state = new PunchState { original = target.localScale };
            }

            state.routine = StartCoroutine(Punch(target, state, id));
            running[id] = state;
        }

        // Achica un instante el boton y lo devuelve a su tamaño: se siente que "se aprieta".
        private IEnumerator Punch(Transform target, PunchState state, int id)
        {
            yield return Scale(target, state.original, state.original * PunchScale, PunchDown);
            yield return Scale(target, state.original * PunchScale, state.original, PunchUp);

            if (target != null) target.localScale = state.original;
            running.Remove(id);
        }

        private static IEnumerator Scale(Transform target, Vector3 from, Vector3 to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                if (target == null) yield break;
                target.localScale = Vector3.Lerp(from, to, t / seconds);
                yield return null;
            }
        }

        // Un "tic" corto hecho por codigo: dos tonos que se apagan rapido y un poco de ruido al principio.
        private static AudioClip BuildClickClip()
        {
            int length = (int)(SampleRate * 0.07f);
            var data = new float[length];
            var random = new System.Random(7);
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Exp(-t * 55f);
                float tone = Mathf.Sin(2f * Mathf.PI * 620f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 1240f * t) * 0.25f;
                float noise = ((float)random.NextDouble() * 2f - 1f) * 0.15f * Mathf.Exp(-t * 120f);
                data[i] = (tone + noise) * envelope;
            }

            var created = AudioClip.Create("UiClick", length, 1, SampleRate, false);
            created.SetData(data, 0);
            return created;
        }
    }
}
