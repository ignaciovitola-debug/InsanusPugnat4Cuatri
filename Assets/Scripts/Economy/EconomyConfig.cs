using UnityEngine;

namespace GladiusAI
{
    // Numeros de la economia, editables desde el Inspector (Assets/Resources/EconomyConfig).
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "Insanus Pugnat/Economy Config")]
    public class EconomyConfig : ScriptableObject
    {
        private const string ResourcePath = "EconomyConfig";

        public int maxEnergy = 5;
        [Tooltip("Minutos que tarda en volver 1 punto de energia.")]
        public float minutesPerEnergy = 15f;
        [Tooltip("Energia que cuesta entrar a un combate.")]
        public int energyPerCombat = 1;

        private static EconomyConfig instance;

        // Si el asset no existe se usan los valores de arriba, asi el juego nunca se rompe por falta de config.
        public static EconomyConfig Instance
        {
            get
            {
                if (instance == null)
                    instance = Resources.Load<EconomyConfig>(ResourcePath) ?? CreateInstance<EconomyConfig>();
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;
    }
}
