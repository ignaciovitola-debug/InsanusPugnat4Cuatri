using UnityEngine;

namespace GladiusAI
{
    // Tabla de progresion por nivel de gladiador, armada a partir de la curva de EconomyConfig.
    public static class ProgressionTables
    {
        private const int MaxLevel = 20;
        private const int SampleCount = 41;

        private static LookUpTable enemyScale;

        // Cuanto mas fuertes son los rivales segun el nivel del gladiador elegido.
        public static float EnemyScaleForLevel(int level)
        {
            enemyScale ??= Build(EconomyConfig.Instance.enemyScaleByLevel);
            return enemyScale.ReturnValue(level);
        }

        private static LookUpTable Build(AnimationCurve curve)
            => new LookUpTable(curve.Evaluate, 0f, MaxLevel, SampleCount);

        // Si el Editor no recarga el dominio, la tabla vieja se descarta al arrancar.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => enemyScale = null;
    }
}
