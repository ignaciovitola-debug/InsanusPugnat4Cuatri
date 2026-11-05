using UnityEngine;

namespace GladiusAI
{
    // Tablas de progresion por nivel de gladiador, armadas a partir de las curvas de EconomyConfig.
    public static class ProgressionTables
    {
        private const int MaxLevel = 20;
        private const int SampleCount = 41;

        private static LookUpTable enemyScale;
        private static LookUpTable trainingBonus;

        // Cuanto mas fuertes son los rivales segun el nivel del gladiador elegido.
        public static float EnemyScaleForLevel(int level)
        {
            enemyScale ??= Build(EconomyConfig.Instance.enemyScaleByLevel);
            return enemyScale.ReturnValue(level);
        }

        // Cuanto del bonus de entrenamiento se aplica segun el nivel (baja con los niveles).
        public static float TrainingBonusForLevel(int level)
        {
            trainingBonus ??= Build(EconomyConfig.Instance.trainingBonusByLevel);
            return trainingBonus.ReturnValue(level);
        }

        private static LookUpTable Build(AnimationCurve curve)
            => new LookUpTable(curve.Evaluate, 0f, MaxLevel, SampleCount);

        // Si el Editor no recarga el dominio, las tablas viejas se descartan al arrancar.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            enemyScale = null;
            trainingBonus = null;
        }
    }
}
