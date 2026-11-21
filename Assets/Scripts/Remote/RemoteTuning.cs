using UnityEngine;

namespace GladiusAI
{
    // Valores que se pueden cambiar desde Remote Config. Si no llega nada, se usan los de EconomyConfig.
    // El resto del juego lee de acá y no sabe de dónde vino el valor.
    public static class RemoteTuning
    {
        private static int? maxEnergy;
        private static float? minutesPerEnergy;
        private static int? rushCostPerMinute;
        private static float? rewardMultiplier;
        private static bool outlineEnabled = true;

        private static EconomyConfig Config => EconomyConfig.Instance;

        public static int MaxEnergy => maxEnergy ?? Config.maxEnergy;
        public static float MinutesPerEnergy => minutesPerEnergy ?? Config.minutesPerEnergy;
        public static int RushCostPerMinute => rushCostPerMinute ?? Config.rushCostPerMinute;
        public static float RewardMultiplier => rewardMultiplier ?? 1f;
        public static bool OutlineEnabled => outlineEnabled;

        // Cada setter limita el valor a un rango sano, por si el panel manda algo raro.
        public static void SetMaxEnergy(int value) => maxEnergy = Mathf.Clamp(value, 1, 20);
        public static void SetMinutesPerEnergy(float value) => minutesPerEnergy = Mathf.Clamp(value, 0.1f, 120f);
        public static void SetRushCostPerMinute(int value) => rushCostPerMinute = Mathf.Clamp(value, 0, 100);
        public static void SetRewardMultiplier(float value) => rewardMultiplier = Mathf.Clamp(value, 0f, 5f);

        public static void SetOutlineEnabled(bool value)
        {
            outlineEnabled = value;
            OutlineSwitch.Apply(value);
        }

        // Vuelve a los valores locales (se usa al arrancar y en las pruebas).
        public static void ResetToDefaults()
        {
            maxEnergy = null;
            minutesPerEnergy = null;
            rushCostPerMinute = null;
            rewardMultiplier = null;
            outlineEnabled = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ResetToDefaults();
    }
}
