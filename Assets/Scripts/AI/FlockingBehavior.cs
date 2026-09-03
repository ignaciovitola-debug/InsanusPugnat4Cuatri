using System.Collections.Generic;
using UnityEngine;

namespace GladiusAI
{
    /// <summary>
    /// Las 3 reglas clásicas de Flocking (Craig Reynolds, 1986): Separación,
    /// Alineación y Cohesión. Solo trabaja con posiciones/velocidades de los
    /// vecinos que le pasan — Boid decide antes cuáles son sus vecinos (mismo grupo, en rango).
    /// </summary>
    public static class FlockingBehavior
    {
        /// <summary>Evita colisiones: empuja lejos de cada vecino demasiado cercano (rango corto).</summary>
        public static Vector3 Separation(Vector3 position, List<Boid> neighbors, float separationRadius)
        {
            Vector3 steer = Vector3.zero;
            int count = 0;

            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector3 offset = position - neighbors[i].Position;
                float distance = offset.magnitude;
                if (distance > 0.0001f && distance < separationRadius)
                {
                    steer += offset.normalized / distance; // más cerca = empuja más fuerte
                    count++;
                }
            }

            return count > 0 ? steer / count : Vector3.zero;
        }

        /// <summary>Ajusta la dirección para moverse como el promedio del grupo.</summary>
        public static Vector3 Alignment(List<Boid> neighbors)
        {
            if (neighbors.Count == 0) return Vector3.zero;

            Vector3 avgVelocity = Vector3.zero;
            for (int i = 0; i < neighbors.Count; i++)
                avgVelocity += neighbors[i].Velocity;

            return avgVelocity / neighbors.Count;
        }

        /// <summary>Se acerca al centro del grupo (rango mayor que separación).</summary>
        public static Vector3 Cohesion(Vector3 position, List<Boid> neighbors)
        {
            if (neighbors.Count == 0) return Vector3.zero;

            Vector3 center = Vector3.zero;
            for (int i = 0; i < neighbors.Count; i++)
                center += neighbors[i].Position;
            center /= neighbors.Count;

            return center - position;
        }
    }
}
