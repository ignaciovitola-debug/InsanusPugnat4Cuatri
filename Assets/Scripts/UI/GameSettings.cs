using UnityEngine;

namespace GladiusAI
{
    // Ajustes del jugador (volumenes, brillo, vibracion, cuadros por segundo, calidad). Se guardan en PlayerPrefs,
    // aparte del progreso: "Borrar datos" no los toca. Cada setter aplica el cambio al instante.
    public static class GameSettings
    {
        private const string MasterKey = "MasterVolume";
        private const string MusicKey = "MusicVolume";
        private const string SfxKey = "SfxVolume";
        private const string BrightnessKey = "Brightness";
        private const string VibrationKey = "Vibration";
        private const string Fps60Key = "Fps60";
        private const string QualityKey = "QualityLevel";

        public const float MinBrightness = 0.3f;

        public static float Master
        {
            get => PlayerPrefs.GetFloat(MasterKey, 1f);
            set
            {
                Store(MasterKey, value);
                AudioListener.volume = value;
            }
        }

        public static float Music
        {
            get => PlayerPrefs.GetFloat(MusicKey, 1f);
            set
            {
                Store(MusicKey, value);
                SoundManager.RefreshMusicVolume();
            }
        }

        public static float Sfx
        {
            get => PlayerPrefs.GetFloat(SfxKey, 1f);
            set => Store(SfxKey, value);
        }

        public static float Brightness
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(BrightnessKey, 1f), MinBrightness, 1f);
            set
            {
                Store(BrightnessKey, Mathf.Clamp(value, MinBrightness, 1f));
                BrightnessOverlay.Apply();
            }
        }

        public static bool Vibration
        {
            get => PlayerPrefs.GetInt(VibrationKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(VibrationKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool Fps60
        {
            get => PlayerPrefs.GetInt(Fps60Key, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(Fps60Key, value ? 1 : 0);
                PlayerPrefs.Save();
                ApplyFrameRate();
            }
        }

        public static int QualityIndex
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
            set
            {
                int index = Mathf.Clamp(value, 0, QualitySettings.names.Length - 1);
                PlayerPrefs.SetInt(QualityKey, index);
                PlayerPrefs.Save();
                QualitySettings.SetQualityLevel(index);
            }
        }

        // Se llama una sola vez al arrancar el juego (AppStartup).
        public static void ApplyAll()
        {
            AudioListener.volume = Master;
            QualitySettings.SetQualityLevel(QualityIndex);
            ApplyFrameRate();
        }

        // En Android/iOS Unity limita el juego a 30 FPS por defecto: se pide explicitamente lo que eligio el jugador.
        private static void ApplyFrameRate() => Application.targetFrameRate = Fps60 ? 60 : 30;

        // Una vibracion corta al terminar un combate. Solo en el celular y si esta activada.
        public static void Vibrate()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Vibration)
                Handheld.Vibrate();
#endif
        }

        private static void Store(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
        }
    }
}
