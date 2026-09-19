using UnityEngine;

namespace GladiusAI
{
    /// <summary>
    /// Abstracción de la Factory: quien pide gladiadores (CombatStarter,
    /// GladiatorPool) depende de esto, no de GladiatorFactory concreto —
    /// Dependency Inversion, tal como lo muestra Modelos y Algoritmos (Clase 2).
    /// </summary>
    public interface IGladiatorFactory
    {
        GladiatorNPC CreatePlayer(Vector3 position, Quaternion rotation);
        GladiatorNPC CreateEnemy(Vector3 position, Quaternion rotation);
    }
}
