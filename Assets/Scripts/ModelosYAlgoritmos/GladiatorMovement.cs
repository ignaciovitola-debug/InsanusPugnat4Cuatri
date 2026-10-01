using UnityEngine;

namespace GladiusAI
{
    public class GladiatorMovement
    {
        // Buffer compartido para el SphereCast: SphereCastAll creaba un array nuevo cada frame
        // por gladiador, basura que despues frenaba el juego cuando pasaba el GC.
        private static readonly RaycastHit[] HitBuffer = new RaycastHit[8];

        private readonly Transform self;
        private readonly Rigidbody rb;
        private readonly float moveSpeed;
        private readonly float avoidCastDistance;
        private readonly float avoidRadius;
        private readonly LayerMask obstacleLayer;
        private readonly float knockbackForce;

        public GladiatorMovement(Transform self, Rigidbody rb, float moveSpeed,
            float avoidCastDistance, float avoidRadius, LayerMask obstacleLayer,
            float knockbackForce)
        {
            this.self = self;
            this.rb = rb;
            this.moveSpeed = moveSpeed;
            this.avoidCastDistance = avoidCastDistance;
            this.avoidRadius = avoidRadius;
            this.obstacleLayer = obstacleLayer;
            this.knockbackForce = knockbackForce;
        }

        public void MoveToward(Vector3 targetPos, Transform ignoreObstacle = null)
        {
            Vector3 dir = targetPos - self.position;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;

            Vector3 avoid = GetAvoidanceDir(dir, ignoreObstacle);
            Vector3 finalDir = (dir + avoid * 1.5f).normalized;

            SetVelocity(finalDir);
        }

        public void Stop()
        {
            Vector3 vel = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, vel.y, 0f);
        }

        public void ApplyKnockback(Vector3 attackerPos)
        {
            Vector3 knockDir = (self.position - attackerPos).normalized;
            knockDir.y = 0f;
            rb.AddForce(knockDir * knockbackForce, ForceMode.Impulse);
        }

        private void SetVelocity(Vector3 dir)
        {
            Vector3 vel = dir * moveSpeed;
            vel.y = rb.linearVelocity.y;
            rb.linearVelocity = vel;
        }

        private Vector3 GetAvoidanceDir(Vector3 desiredDir, Transform ignoreObstacle)
        {
            int hitCount = Physics.SphereCastNonAlloc(self.position, avoidRadius, desiredDir, HitBuffer, avoidCastDistance, obstacleLayer);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = HitBuffer[i];
                // No esquivo a mi propio objetivo — a ESE justamente quiero llegar.
                if (ignoreObstacle != null &&
                    (hit.collider.transform == ignoreObstacle || hit.collider.transform.IsChildOf(ignoreObstacle)))
                    continue;

                Vector3 avoidDir = Vector3.Cross(Vector3.up, hit.normal).normalized;
                if (Vector3.Dot(avoidDir, self.right) < 0f)
                    avoidDir = -avoidDir;
                return avoidDir;
            }

            return Vector3.zero;
        }
    }
}
