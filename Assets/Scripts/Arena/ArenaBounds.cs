using UnityEngine;

namespace GladiusAI
{
    
    /// Define el área jugable de la escena "AI": contener a los boids, generar comida y elegir puntos de wander.
    
    public class ArenaBounds : MonoBehaviour
    {
        public static ArenaBounds Instance { get; private set; }

        [SerializeField] private Vector3 size = new Vector3(30f, 0f, 30f);
        [SerializeField] private float margin = 3f;

        [Tooltip("Si esta activo el limite es una elipse inscripta en el rectangulo (arena ovalada).")]
        [SerializeField] private bool elliptical;

        private void Awake() => Instance = this;

        public Vector3 Center => transform.position;
        public Vector3 Size => size;
        public bool Elliptical => elliptical;

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

            if (elliptical)
            {
                // Se lleva el punto al circulo unitario: si cae afuera, se lo devuelve al borde de la elipse.
                float nx = local.x / halfX;
                float nz = local.z / halfZ;
                float sqr = nx * nx + nz * nz;
                if (sqr > 1f)
                {
                    float inv = 1f / Mathf.Sqrt(sqr);
                    local.x = nx * inv * halfX;
                    local.z = nz * inv * halfZ;
                }
                return Center + local;
            }

            local.x = Mathf.Clamp(local.x, -halfX, halfX);
            local.z = Mathf.Clamp(local.z, -halfZ, halfZ);
            return Center + local;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            if (!elliptical)
            {
                Gizmos.DrawWireCube(transform.position, new Vector3(size.x, 0.1f, size.z));
                return;
            }

            Vector3 prev = transform.position + new Vector3(size.x / 2f, 0f, 0f);
            for (int i = 1; i <= 48; i++)
            {
                float a = i * Mathf.PI * 2f / 48f;
                Vector3 next = transform.position + new Vector3(Mathf.Cos(a) * size.x / 2f, 0f, Mathf.Sin(a) * size.z / 2f);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
