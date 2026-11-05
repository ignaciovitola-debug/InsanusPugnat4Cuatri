using UnityEngine;

namespace GladiusAI
{
    // Strategy: cada dificultad decide su propia forma de armar el combate. Es un ScriptableObject,
    // asi cada una es un asset que se elige desde el Inspector.
    public abstract class DifficultyStrategy : ScriptableObject
    {
        [SerializeField] private string displayName = "Dificultad";

        public string DisplayName => displayName;

        // Cuantos rivales hay, uno despues del otro.
        public abstract int EnemyCount { get; }

        // Ajusta los stats de un arquetipo; levelMultiplier sube con el nivel del gladiador.
        public abstract CombatStats Scale(CombatStats baseStats, float levelMultiplier);

        // Denarios que paga ganar el combate.
        public abstract int Reward();
    }
}
