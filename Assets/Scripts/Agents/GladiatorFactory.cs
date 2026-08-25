using UnityEngine;

namespace GladiusAI
{
    public class GladiatorFactory : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GladiatorNPC playerPrefab;
        [SerializeField] private GladiatorNPC enemyPrefab;

        private GladiatorNPC CreateFrom(GladiatorNPC prefab, GladiatorStats stats, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogError("[GladiatorFactory] Falta asignar un prefab en el Inspector.");
                return null;
            }

            GladiatorNPC instance = Instantiate(prefab, position, rotation);
            instance.ApplyStats(stats);
            return instance;
        }

        public GladiatorNPC CreatePlayer(GladiatorStats stats, Vector3 position, Quaternion rotation)
            => CreateFrom(playerPrefab, stats, position, rotation);

        public GladiatorNPC CreateEnemy(GladiatorStats stats, Vector3 position, Quaternion rotation)
            => CreateFrom(enemyPrefab, stats, position, rotation);

        public GladiatorNPC CreateFromPlayerPrefs(Vector3 position, Quaternion rotation)
        {
            var stats = new GladiatorStats(
                name: "Jugador",
                hp: PlayerPrefs.GetInt("PlayerHP", 100),
                minDmg: 10f,
                maxDmg: PlayerPrefs.GetInt("PlayerDamage", 20),
                cooldown: 1.2f
            );

            return CreatePlayer(stats, position, rotation);
        }
    }
}