using UnityEngine;

namespace GladiusAI
{
    // Numeros del juego que se editan desde el Inspector (Assets/Resources/EconomyConfig).
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "Insanus Pugnat/Economy Config")]
    public class EconomyConfig : ScriptableObject
    {
        private const string ResourcePath = "EconomyConfig";

        [System.Serializable]
        public struct Zone
        {
            public string scene;
            public string displayName;
        }

        [System.Serializable]
        public struct TrainingTier
        {
            public int minutes;
            public float bonusHP;
            public float bonusDamage;
        }

        [Header("Energia")]
        public int maxEnergy = 5;
        [Tooltip("Minutos que tarda en volver 1 punto.")]
        public float minutesPerEnergy = 15f;
        public int energyPerCombat = 1;

        [Header("Combates")]
        public DifficultyStrategy[] difficulties = new DifficultyStrategy[0];
        public EnemyArchetype[] archetypes = new EnemyArchetype[0];
        public Zone[] zones =
        {
            new Zone { scene = "ArenaDeCombate2", displayName = "Coliseo" },
            new Zone { scene = "ArenaZona_Catacumbas", displayName = "Catacumbas" },
            new Zone { scene = "ArenaZona_Patio", displayName = "Patio" },
        };

        [Header("Entrenamiento")]
        public TrainingTier[] trainingTiers =
        {
            new TrainingTier { minutes = 5, bonusHP = 6f, bonusDamage = 1f },
            new TrainingTier { minutes = 15, bonusHP = 20f, bonusDamage = 2.5f },
            new TrainingTier { minutes = 30, bonusHP = 50f, bonusDamage = 5f },
        };
        [Tooltip("Denarios por cada minuto que falta para terminar al instante.")]
        public int rushCostPerMinute = 4;

        [Header("Curvas por nivel de gladiador (nivel 0 a 20)")]
        [Tooltip("Multiplica la fuerza de los rivales.")]
        public AnimationCurve enemyScaleByLevel = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(20f, 2f));
        [Tooltip("Multiplica el bonus del entrenamiento (baja con los niveles).")]
        public AnimationCurve trainingBonusByLevel = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(20f, 0.4f));

        private static EconomyConfig instance;

        // Si falta el asset se usan estos valores por defecto.
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
