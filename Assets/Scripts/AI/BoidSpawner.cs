using UnityEngine;

namespace GladiusAI
{
    public class BoidSpawner : MonoBehaviour
    {
        [SerializeField] private Boid boidPrefab;
        [SerializeField] private int boidsPerGroup = 4;
        [SerializeField] private float spawnSpread = 2f;
        [SerializeField] private Color[] groupColors = { Color.cyan, Color.yellow, Color.magenta, Color.blue };

        private int nextGroupId;

        public void SpawnGroup()
        {
            if (boidPrefab == null || ArenaBounds.Instance == null) return;

            Vector3 center = ArenaBounds.Instance.RandomPointInside();
            int groupId = nextGroupId;
            nextGroupId++;

            for (int i = 0; i < boidsPerGroup; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-spawnSpread, spawnSpread), 0f, Random.Range(-spawnSpread, spawnSpread));
                Vector3 pos = ArenaBounds.Instance.ClampPosition(center + offset);

                Boid boid = Instantiate(boidPrefab, pos, Quaternion.identity, transform);
                boid.GroupId = groupId;
            }
        }
    }
}
