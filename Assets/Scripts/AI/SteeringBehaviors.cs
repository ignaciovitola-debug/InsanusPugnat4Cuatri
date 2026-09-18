using UnityEngine;

namespace GladiusAI
{
    /// <summary>
    /// Comportamientos de Steering (Reynolds) usados por los Boids y por el Cazador.
    /// Todas las funciones devuelven una fuerza de dirección (aceleración deseada);
    /// quien llama la acumula sobre la velocidad actual con <see cref="Integrate"/>.
    /// Todo es cinemático (Transform), no se usa Rigidbody en ningún lado.
    /// </summary>
    public static class SteeringBehaviors
    {
        public static Vector3 Seek(Vector3 position, Vector3 velocity, Vector3 targetPos, float maxSpeed)
        {
            Vector3 desired = targetPos - position;
            desired.y = 0f;
            if (desired.sqrMagnitude < 0.0001f) return Vector3.zero;
            desired = desired.normalized * maxSpeed;
            return desired - velocity;
        }

        public static Vector3 Flee(Vector3 position, Vector3 velocity, Vector3 threatPos, float maxSpeed)
        {
            Vector3 desired = position - threatPos;
            desired.y = 0f;
            if (desired.sqrMagnitude < 0.0001f) desired = Random.insideUnitSphere;
            desired = desired.normalized * maxSpeed;
            return desired - velocity;
        }

        /// <summary>Como Seek, pero frena al entrar en slowRadius para no "vibrar" sobre el objetivo.</summary>
        public static Vector3 Arrive(Vector3 position, Vector3 velocity, Vector3 targetPos, float maxSpeed, float slowRadius)
        {
            Vector3 toTarget = targetPos - position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance < 0.0001f) return -velocity;

            float speed = distance < slowRadius ? maxSpeed * (distance / slowRadius) : maxSpeed;
            Vector3 desired = toTarget.normalized * speed;
            return desired - velocity;
        }

        /// <summary>Persigue prediciendo dónde va a estar el objetivo, no dónde está ahora.</summary>
        public static Vector3 Pursuit(Vector3 position, Vector3 velocity, Vector3 targetPos, Vector3 targetVelocity, float maxSpeed, float maxPredictionTime = 1.5f)
        {
            float distance = Vector3.Distance(position, targetPos);
            float predictionTime = Mathf.Clamp(distance / Mathf.Max(maxSpeed, 0.01f), 0f, maxPredictionTime);
            Vector3 predictedPos = targetPos + targetVelocity * predictionTime;
            return Seek(position, velocity, predictedPos, maxSpeed);
        }

        /// <summary>Huye anticipando hacia dónde viene el perseguidor (Pursuit invertido).</summary>
        public static Vector3 Evade(Vector3 position, Vector3 velocity, Vector3 threatPos, Vector3 threatVelocity, float maxSpeed, float maxPredictionTime = 1f)
        {
            float distance = Vector3.Distance(position, threatPos);
            float predictionTime = Mathf.Clamp(distance / Mathf.Max(maxSpeed, 0.01f), 0f, maxPredictionTime);
            Vector3 predictedThreatPos = threatPos + threatVelocity * predictionTime;
            return Flee(position, velocity, predictedThreatPos, maxSpeed);
        }

        /// <summary>Acumula una fuerza de steering sobre la velocidad actual, clampeada por maxForce y maxSpeed.</summary>
        public static Vector3 Integrate(Vector3 velocity, Vector3 steerForce, float maxForce, float maxSpeed, float deltaTime)
        {
            Vector3 clampedForce = Vector3.ClampMagnitude(steerForce, maxForce);
            Vector3 newVelocity = velocity + clampedForce * deltaTime;
            return Vector3.ClampMagnitude(newVelocity, maxSpeed);
        }

        /// <summary>Rota el transform para mirar hacia la dirección de movimiento (plano XZ).</summary>
        public static void FaceDirection(Transform t, Vector3 velocity, float turnSpeedDegPerSec, float deltaTime)
        {
            if (velocity.sqrMagnitude < 0.01f) return;
            Quaternion targetRot = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            t.rotation = Quaternion.RotateTowards(t.rotation, targetRot, turnSpeedDegPerSec * deltaTime);
        }

        /// <summary>
        /// Si hay un obstáculo (Columnas, paredes) adelante en la dirección de movimiento,
        /// devuelve una fuerza de steering para esquivarlo tangencialmente a su superficie.
        /// Vector3.zero si no se está moviendo o el camino está libre.
        /// </summary>
        public static Vector3 ObstacleAvoidance(Vector3 position, Vector3 velocity, float maxSpeed,
            float castDistance, float probeRadius, LayerMask obstacleLayer)
        {
            if (velocity.sqrMagnitude < 0.0001f) return Vector3.zero;

            Vector3 dir = velocity.normalized;
            Vector3 origin = position + Vector3.up * 0.5f;

            if (!Physics.SphereCast(origin, probeRadius, dir, out RaycastHit hit, castDistance, obstacleLayer))
                return Vector3.zero;

            Vector3 avoidDir = Vector3.Cross(Vector3.up, hit.normal).normalized;
            if (Vector3.Dot(avoidDir, dir) < 0f) avoidDir = -avoidDir;

            Vector3 desired = avoidDir * maxSpeed;
            return desired - velocity;
        }

        /// <summary>
        /// Aplica el desplazamiento del frame recortándolo si de lo contrario atravesaría
        /// un obstáculo sólido. El steering de <see cref="ObstacleAvoidance"/> es sólo una
        /// sugerencia de dirección (puede llegar tarde en ángulos cerrados o a alta velocidad);
        /// esto es la garantía dura de que nunca se atraviesa una Columna/pared.
        /// </summary>
        public static Vector3 MoveAndCollide(Vector3 position, Vector3 delta, float bodyRadius, LayerMask obstacleLayer)
        {
            float distance = delta.magnitude;
            if (distance < 0.0001f) return position;

            Vector3 dir = delta / distance;
            Vector3 origin = position + Vector3.up * 0.5f;

            if (!Physics.SphereCast(origin, bodyRadius, dir, out RaycastHit hit, distance, obstacleLayer))
                return position + delta;

            float safeDistance = Mathf.Max(0f, hit.distance - 0.05f);
            Vector3 afterBlock = position + dir * safeDistance;

            // Desliza lo que quedó del movimiento a lo largo de la superficie del obstáculo
            // (tangencial a su normal) en vez de perderlo — si no, queda pegado contra la columna.
            Vector3 normal = hit.normal;
            normal.y = 0f;
            if (normal.sqrMagnitude > 0.0001f)
            {
                Vector3 remaining = delta - dir * safeDistance;
                afterBlock += Vector3.ProjectOnPlane(remaining, normal.normalized);
            }

            return afterBlock;
        }
    }
}
