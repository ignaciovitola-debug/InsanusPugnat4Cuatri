using UnityEngine;

namespace GladiusAI
{
    /// <summary>
    /// Define el área jugable de la escena "AI": límites para contener a los
    /// boids, generar comida y elegir puntos de wander. Poné un GameObject
    /// vacío centrado en la arena y ajustá el tamaño en el Inspector.
    /// Contención en dos capas: un empuje suave que empieza antes del borde
    /// (margin) para que giren con naturalidad, y un clamp duro de posición
    /// como garantía de que nunca salgan del área.
    /// </summary>
    public class ArenaBounds : MonoBehaviour
    {
        public static ArenaBounds Instance { get; private set; }

        [SerializeField] private Vector3 size = new Vector3(30f, 0f, 30f);
        [SerializeField] private float margin = 3f;

        private void Awake() => Instance = this;

        public Vector3 Center => transform.position;

        public Vector3 RandomPointInside()
        {
            float halfX = size.x / 2f - margin;
            float halfZ = size.z / 2f - margin;
            float x = Random.Range(-halfX, halfX);
            float z = Random.Range(-halfZ, halfZ);
            return Center + new Vector3(x, 0f, z);
        }

        /// <summary>Empuje suave hacia el centro apenas entra en la franja "margin" antes del borde.</summary>
        public Vector3 GetContainmentForce(Vector3 position, Vector3 velocity, float maxSpeed)
        {
            Vector3 local = position - Center;
            float halfX = size.x / 2f - margin;
            float halfZ = size.z / 2f - margin;

            bool nearingEdge = Mathf.Abs(local.x) > halfX || Mathf.Abs(local.z) > halfZ;
            return nearingEdge ? SteeringBehaviors.Seek(position, velocity, Center, maxSpeed) : Vector3.zero;
        }

        /// <summary>Garantía dura: nunca deja que la posición salga del rectángulo de la arena.</summary>
        public Vector3 ClampPosition(Vector3 position)
        {
            Vector3 local = position - Center;
            local.x = Mathf.Clamp(local.x, -size.x / 2f, size.x / 2f);
            local.z = Mathf.Clamp(local.z, -size.z / 2f, size.z / 2f);
            return Center + local;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, 0.1f, size.z));
        }
    }
}
