using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEngine;

namespace GladiusAI
{
    // Avisa que llegaron valores nuevos de Remote Config.
    public readonly struct RemoteConfigLoadedEvent
    {
    }

    // Baja los valores de Remote Config al arrancar y los pasa a RemoteTuning.
    // Sin internet o sin proyecto vinculado, el juego sigue con sus valores locales.
    public static class RemoteConfigLoader
    {
        private const int TimeoutMilliseconds = 8000;

        public struct UserAttributes { }
        public struct AppAttributes { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static async void Start()
        {
            try
            {
                var fetch = FetchAsync();
                var finished = await Task.WhenAny(fetch, Task.Delay(TimeoutMilliseconds));
                if (finished != fetch)
                {
                    Debug.LogWarning("[RemoteConfig] Sin respuesta a tiempo: se usan los valores locales.");
                    return;
                }
                await fetch;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RemoteConfig] No se pudo cargar, se usan los valores locales: {e.Message}");
            }
        }

        private static async Task FetchAsync()
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            await RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(), new AppAttributes());
            Apply(RemoteConfigService.Instance.appConfig);
        }

        // Solo pisa los valores que existen en el panel.
        private static void Apply(RuntimeConfig config)
        {
            if (config.HasKey("maxEnergy")) RemoteTuning.SetMaxEnergy(config.GetInt("maxEnergy"));
            if (config.HasKey("minutesPerEnergy")) RemoteTuning.SetMinutesPerEnergy(config.GetFloat("minutesPerEnergy"));
            if (config.HasKey("rushCostPerMinute")) RemoteTuning.SetRushCostPerMinute(config.GetInt("rushCostPerMinute"));
            if (config.HasKey("rewardMultiplier")) RemoteTuning.SetRewardMultiplier(config.GetFloat("rewardMultiplier"));
            if (config.HasKey("outlineEnabled")) RemoteTuning.SetOutlineEnabled(config.GetBool("outlineEnabled"));

            Debug.Log($"[RemoteConfig] Valores aplicados: energia max {RemoteTuning.MaxEnergy}, " +
                      $"minutos por punto {RemoteTuning.MinutesPerEnergy}, apurar {RemoteTuning.RushCostPerMinute}/min, " +
                      $"premio x{RemoteTuning.RewardMultiplier}, contorno {RemoteTuning.OutlineEnabled}");
            EventManager.Raise(new RemoteConfigLoadedEvent());
        }
    }
}
