using System.Collections.Generic;
using UnityEngine;

namespace GladiusAI
{
    // Genera los contratos que se ofrecen en la Ludus: uno por dificultad, cada uno en una zona distinta si se puede.
    public static class ContractFactory
    {
        public static List<CombatContract> CreateAll()
        {
            var config = EconomyConfig.Instance;
            var zones = new List<EconomyConfig.Zone>(config.zones);
            var contracts = new List<CombatContract>();

            foreach (var tier in config.tiers)
            {
                var zone = TakeRandom(zones, config.zones);
                contracts.Add(new CombatContract
                {
                    title = tier.name,
                    zoneScene = zone.scene,
                    zoneName = zone.displayName,
                    enemyCount = Mathf.Max(1, tier.enemies),
                    statScale = tier.statScale,
                    reward = tier.reward,
                });
            }
            return contracts;
        }

        // Saca una zona al azar sin repetir; si ya se usaron todas, vuelve a elegir entre todas.
        private static EconomyConfig.Zone TakeRandom(List<EconomyConfig.Zone> remaining, EconomyConfig.Zone[] all)
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
