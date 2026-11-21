using System.Collections;
using System.Collections.Generic;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace GladiusAI
{
    // Maneja los anuncios con recompensa (LevelPlay): los inicia al abrir el juego, los deja cargados
    // y, cuando terminan, da el premio que corresponde. En el Editor LevelPlay muestra anuncios de mentira.
    public class AdsService : MonoBehaviour
    {
        // Los eventos de "premio" y "cerrado" pueden llegar en cualquier orden: se espera un poco antes de decidir.
        private const float FinishDelaySeconds = 0.7f;
        private const float RetryLoadSeconds = 20f;

        private static AdsService instance;

        private sealed class Entry
        {
            public AdSlot slot;
            public LevelPlayRewardedAd ad;
            public bool rewarded;
            public bool showing;
        }

        private readonly Dictionary<AdSlot, Entry> entries = new Dictionary<AdSlot, Entry>();
        private bool initialized;

        // En el Editor el simulador de LevelPlay falla, asi que se usa un anuncio de mentira de pocos segundos.
        private bool simulated;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) return;

            var go = new GameObject("AdsService");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AdsService>();
        }

        // true si el anuncio ya esta cargado y se puede mostrar.
        public static bool IsReady(AdSlot slot)
        {
            if (instance == null || !instance.initialized) return false;
            if (instance.simulated) return true;
            return instance.entries.TryGetValue(slot, out var entry) && entry.ad.IsAdReady();
        }

        // Muestra el anuncio. Al terminar se da el premio y se avisa con AdFinishedEvent.
        public static bool Show(AdSlot slot)
        {
            if (!IsReady(slot))
            {
                EventManager.Raise(new AdFinishedEvent(slot, AdOutcome.Failed, AdRewards.Grant(slot, AdOutcome.Failed, AdsConfig.Instance)));
                return false;
            }

            if (instance.simulated)
            {
                instance.StartCoroutine(instance.SimulateAd(slot));
                return true;
            }

            var entry = instance.entries[slot];
            if (entry.showing) return false;

            entry.rewarded = false;
            entry.showing = true;
            entry.ad.ShowAd();
            return true;
        }

        private void Start()
        {
#if UNITY_EDITOR
            simulated = true;
            initialized = true;
            Debug.Log("[Ads] Editor: los anuncios son simulados (en el celular se usa LevelPlay).");
            return;
#else
            StartLevelPlay();
#endif
        }

        private void StartLevelPlay()
        {
            var config = AdsConfig.Instance;
            if (!config.IsConfigured)
            {
                Debug.LogWarning("[Ads] Falta el App Key de LevelPlay (Assets/Resources/AdsConfig): no hay anuncios.");
                return;
            }

            LevelPlay.OnInitSuccess += OnInitSuccess;
            LevelPlay.OnInitFailed += OnInitFailed;
            LevelPlay.Init(config.appKey);
        }

        private void OnDestroy()
        {
            LevelPlay.OnInitSuccess -= OnInitSuccess;
            LevelPlay.OnInitFailed -= OnInitFailed;
        }

        private void OnInitSuccess(LevelPlayConfiguration configuration)
        {
            var config = AdsConfig.Instance;
            CreateEntry(AdSlot.Energy, config.energyAdUnitId);
            CreateEntry(AdSlot.Coins, config.coinsAdUnitId);
            initialized = true;
            Debug.Log("[Ads] LevelPlay listo.");
        }

        private void OnInitFailed(LevelPlayInitError error)
            => Debug.LogWarning($"[Ads] No se pudo iniciar LevelPlay: {error}");

        private void CreateEntry(AdSlot slot, string adUnitId)
        {
            var entry = new Entry { slot = slot, ad = new LevelPlayRewardedAd(adUnitId) };
            entry.ad.OnAdRewarded += (info, reward) => entry.rewarded = true;
            entry.ad.OnAdClosed += info => StartCoroutine(Finish(entry));
            entry.ad.OnAdDisplayFailed += (info, error) =>
            {
                Debug.LogWarning($"[Ads] No se pudo mostrar el anuncio: {error}");
                StartCoroutine(Finish(entry, forceFailed: true));
            };
            entry.ad.OnAdLoadFailed += error =>
            {
                Debug.LogWarning($"[Ads] No se pudo cargar el anuncio: {error}");
                StartCoroutine(RetryLoad(entry));
            };

            entries[slot] = entry;
            entry.ad.LoadAd();
        }

        // Anuncio de mentira para probar en el Editor: "dura" un par de segundos y siempre se ve entero.
        private IEnumerator SimulateAd(AdSlot slot)
        {
            yield return new WaitForSecondsRealtime(1.5f);
            string message = AdRewards.Grant(slot, AdOutcome.Completed, AdsConfig.Instance);
            EventManager.Raise(new AdFinishedEvent(slot, AdOutcome.Completed, message));
        }

        private IEnumerator Finish(Entry entry, bool forceFailed = false)
        {
            yield return new WaitForSecondsRealtime(FinishDelaySeconds);
            if (!entry.showing) yield break;

            entry.showing = false;
            var outcome = forceFailed ? AdOutcome.Failed : entry.rewarded ? AdOutcome.Completed : AdOutcome.Skipped;
            string message = AdRewards.Grant(entry.slot, outcome, AdsConfig.Instance);
            EventManager.Raise(new AdFinishedEvent(entry.slot, outcome, message));

            entry.ad.LoadAd();
        }

        private IEnumerator RetryLoad(Entry entry)
        {
            yield return new WaitForSecondsRealtime(RetryLoadSeconds);
            entry.ad.LoadAd();
        }
    }
}
