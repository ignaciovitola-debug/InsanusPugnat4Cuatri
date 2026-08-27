using UnityEngine;

namespace GladiusAI
{
    public class GladiatorFactory : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GladiatorNPC playerPrefab;
        [SerializeField] private GladiatorNPC enemyPrefab;

        private GladiatorNPC Create(GladiatorNPC prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogError("[GladiatorFactory] Falta asignar un prefab en el Inspector.");
                return null;
            }

            return Instantiate(prefab, position, rotation);
        }

        public GladiatorNPC CreatePlayer(Vector3 position, Quaternion rotation)
            => Create(playerPrefab, position, rotation);

        public GladiatorNPC CreateEnemy(Vector3 position, Quaternion rotation)
            => Create(enemyPrefab, position, rotation);
    }
}
