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

        [System.Serializable]
        public struct ContractTier
        {
            public string name;
            [Tooltip("Cantidad de rivales, uno despues del otro.")]
            public int enemies;
            [Tooltip("Multiplica vida y dano de los rivales.")]
            public float statScale;
            public int reward;
        }

        [System.Serializable]
        public struct Zone
        {
            public string scene;
            public string displayName;
        }

        [Header("Combates elegibles")]
        public ContractTier[] tiers =
        {
            new ContractTier { name = "Fácil", enemies = 1, statScale = 0.8f, reward = 25 },
            new ContractTier { name = "Normal", enemies = 2, statScale = 1f, reward = 60 },
            new ContractTier { name = "Difícil", enemies = 3, statScale = 1.25f, reward = 120 },
        };

        public Zone[] zones =
        {
            new Zone { scene = "ArenaDeCombate2", displayName = "Coliseo" },
            new Zone { scene = "ArenaZona_Catacumbas", displayName = "Catacumbas" },
            new Zone { scene = "ArenaZona_Patio", displayName = "Patio" },
        };

        [System.Serializable]
        public struct TrainingTier
        {
            public int minutes;
            public float bonusHP;
            public float bonusDamage;
        }

        [Header("Entrenamiento")]
        public TrainingTier[] trainingTiers =
        {
            new TrainingTier { minutes = 5, bonusHP = 6f, bonusDamage = 1f },
            new TrainingTier { minutes = 15, bonusHP = 20f, bonusDamage = 2.5f },
            new TrainingTier { minutes = 30, bonusHP = 50f, bonusDamage = 5f },
        };
        [Tooltip("Denarios por cada minuto que falta, para terminar un entrenamiento al instante.")]
        public int rushCostPerMinute = 4;

        [Header("Rival base (antes de aplicar la dificultad)")]
        public float enemyHP = 90f;
        public float enemyMinDamage = 8f;
        public float enemyMaxDamage = 16f;
        public float enemyCooldown = 1.3f;

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
