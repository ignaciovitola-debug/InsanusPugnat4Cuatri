using UnityEngine;

namespace GladiusAI
{
    
    /// Define el área jugable de la escena "AI": contener a los boids, generar comida y elegir puntos de wander.
    
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
       
        public Vector3 GetContainmentForce(Vector3 position, Vector3 velocity, float maxSpeed)
        {
            Vector3 local = position - Center;
            float halfX = size.x / 2f - margin;
            float halfZ = size.z / 2f - margin;

            bool nearingEdge = Mathf.Abs(local.x) > halfX || Mathf.Abs(local.z) > halfZ;
            return nearingEdge ? SteeringBehaviors.Seek(position, velocity, Center, maxSpeed) : Vector3.zero;
        }

       
        // bodyRadius descuenta el radio del propio agente para que sea su borde (no su
        // centro) el que no cruce el límite — si no, medio cuerpo queda afuera de la arena.
        public Vector3 ClampPosition(Vector3 position, float bodyRadius = 0f)
        {
            Vector3 local = position - Center;
            float halfX = size.x / 2f - bodyRadius;
            float halfZ = size.z / 2f - bodyRadius;
            local.x = Mathf.Clamp(local.x, -halfX, halfX);
            local.z = Mathf.Clamp(local.z, -halfZ, halfZ);
            return Center + local;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, 0.1f, size.z));
        }
    }
}
