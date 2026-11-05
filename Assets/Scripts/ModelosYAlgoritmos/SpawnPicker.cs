using UnityEngine;

namespace GladiusAI
{
    // Elige donde aparece cada gladiador: cerca del punto de spawn, dentro de la arena y lejos de las columnas.
    public class SpawnPicker
    {
        private readonly Vector2 jitter;

        // Cuanto se corre el lugar respecto del punto (X = a los costados, Z = hacia el fondo).
        public SpawnPicker(Vector2 jitter)
        {
            this.jitter = jitter;
        }

        public Vector3 Pick(Transform point)
        {
            int obstacles = LayerMask.GetMask("Obstacle");
            Vector3 origin = point.position;

            for (int attempt = 0; attempt < 10; attempt++)
            {
                var candidate = origin + new Vector3(
                    Random.Range(-jitter.x, jitter.x), 0f, Random.Range(-jitter.y, jitter.y));

                if (ArenaBounds.Instance != null)
                    candidate = ArenaBounds.Instance.ClampPosition(candidate, 0.6f);

                if (!Physics.CheckSphere(candidate, 0.7f, obstacles))
                    return candidate;
            }

            return origin;
        }
    }
}
