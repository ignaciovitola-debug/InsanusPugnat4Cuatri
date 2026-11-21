using UnityEngine;

namespace GladiusAI
{
    // Datos de los anuncios (LevelPlay) y cuanto premian. Se edita desde el Inspector (Assets/Resources/AdsConfig).
    // Si falta el asset se usan estos valores por defecto.
    [CreateAssetMenu(fileName = "AdsConfig", menuName = "Insanus Pugnat/Ads Config")]
    public class AdsConfig : ScriptableObject
    {
        private const string ResourcePath = "AdsConfig";

        [Header("LevelPlay (Android)")]
        [Tooltip("App Key de la app en el panel de LevelPlay.")]
        public string appKey = string.Empty;
        [Tooltip("Ad Unit ID del anuncio Rewarded que da energia.")]
        public string energyAdUnitId = "yc8fsi24kqjiqwty";
        [Tooltip("Ad Unit ID del anuncio Rewarded que da monedas despues de un combate.")]
        public string coinsAdUnitId = "nxjds8paq3tgv8rb";

        [Header("Premios del anuncio de energia")]
        [Tooltip("Energia que se gana si se cierra el anuncio antes de terminar (viendolo entero se llena).")]
        public int energyIfSkipped = 1;

        [Header("Premios del anuncio de monedas")]
        [Tooltip("Denarios que se ganan viendolo entero.")]
        public int coinsIfCompleted = 10;
        [Tooltip("Denarios que se ganan si se cierra antes de terminar.")]
        public int coinsIfSkipped = 2;

        // Sin App Key no se puede iniciar LevelPlay.
        public bool IsConfigured => !string.IsNullOrWhiteSpace(appKey);

        private static AdsConfig instance;

        public static AdsConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<AdsConfig>(ResourcePath);
                    if (instance == null)
                        instance = CreateInstance<AdsConfig>();
                }
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;
    }
}
