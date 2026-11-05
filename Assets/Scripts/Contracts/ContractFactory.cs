using System.Collections.Generic;
using UnityEngine;

namespace GladiusAI
{
    // Arma los combates que se ofrecen en la Ludus: uno por cada dificultad.
    public static class ContractFactory
    {
        // playerLevel es el nivel del gladiador elegido; sube la fuerza de los rivales.
        public static List<CombatContract> CreateAll(int playerLevel)
        {
            var config = EconomyConfig.Instance;
            var freeZones = new List<EconomyConfig.Zone>(config.zones);
            float levelMultiplier = ProgressionTables.EnemyScaleForLevel(playerLevel);
            var contracts = new List<CombatContract>();

            foreach (var difficulty in config.difficulties)
            {
                var zone = TakeRandomZone(freeZones, config.zones);
                contracts.Add(new CombatContract
                {
                    difficulty = difficulty,
                    zoneScene = zone.scene,
                    zoneName = zone.displayName,
                    enemyCount = difficulty.EnemyCount,
                    reward = difficulty.Reward(),
                    levelMultiplier = levelMultiplier,
                });
            }
            return contracts;
        }

        // Saca una zona al azar sin repetir; si ya se usaron todas, vuelve a empezar.
        private static EconomyConfig.Zone TakeRandomZone(List<EconomyConfig.Zone> remaining, EconomyConfig.Zone[] all)
        {
            if (remaining.Count == 0)
                remaining.AddRange(all);

            int index = Random.Range(0, remaining.Count);
            var zone = remaining[index];
            remaining.RemoveAt(index);
            return zone;
        }
    }
}
