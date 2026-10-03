using UnityEngine;

namespace GladiusAI
{
    /// Traduce lo que hace el gladiador a su Animator (Idle, Walk, Attack, Hurt, Block, Dead) y orienta el sprite.
    ///
    /// Sigue el mismo diseño que GladiatorMovement y GladiatorCombat: es una clase común (no un MonoBehaviour)
    /// que arma GladiatorNPC. El NPC decide qué hacer y esta clase solo lo muestra. Si el prefab no tiene
    /// Animator, todos los métodos simplemente no hacen nada (así el NPC nunca tiene que preguntar).
    public class GladiatorAnimator
    {
        // Optimización: Animator.StringToHash convierte el nombre del parámetro a un número una sola vez.
        // Pasar el string en cada SetBool/SetTrigger obligaría a Unity a buscarlo por texto cada vez.
        private static readonly int MovingParam = Animator.StringToHash("Moving");
        private static readonly int AttackParam = Animator.StringToHash("Attack");
        private static readonly int HurtParam = Animator.StringToHash("Hurt");
        private static readonly int DeadParam = Animator.StringToHash("Dead");
        private static readonly int GuardingParam = Animator.StringToHash("Guarding");

        // Distancia horizontal mínima con el rival para darse vuelta. Sin este margen, cuando los dos
        // quedan casi alineados el sprite "parpadea" de un lado al otro.
        private const float FacingDeadZone = 0.05f;

        private readonly Animator animator;
        private readonly SpriteRenderer sprite;
        private readonly float walkSpeedSqr; // umbral de velocidad al cuadrado (ver UpdateLocomotion)

        // Último valor enviado al Animator. Solo le avisamos cuando cambia, no en cada frame.
        private bool isMoving;
        private bool isGuarding;

        public GladiatorAnimator(Animator animator, SpriteRenderer sprite, float walkSpeedThreshold)
        {
            this.animator = animator;
            this.sprite = sprite;
            walkSpeedSqr = walkSpeedThreshold * walkSpeedThreshold;
        }

        /// Idle o Walk según la velocidad horizontal real del cuerpo.
        public void UpdateLocomotion(Vector3 velocity)
        {
            if (animator == null) return;

            // Comparamos velocidades al cuadrado para evitar la raíz cuadrada (más barato, mismo resultado).
            // Ignoramos Y: caer o rebotar no cuenta como caminar.
            bool moving = velocity.x * velocity.x + velocity.z * velocity.z > walkSpeedSqr;
            if (moving == isMoving) return;

            isMoving = moving;
            animator.SetBool(MovingParam, moving);
        }

        /// Levanta el escudo (Block) mientras está en guardia y lo baja al terminar.
        public void SetGuarding(bool guarding)
        {
            if (animator == null || guarding == isGuarding) return;

            isGuarding = guarding;
            animator.SetBool(GuardingParam, guarding);
        }

        /// Los sprites están dibujados mirando a la derecha: se espejan cuando el rival queda a la izquierda.
        public void FaceTowards(Vector3 selfPosition, Vector3 targetPosition)
        {
            if (sprite == null) return;

            float dx = targetPosition.x - selfPosition.x;
            if (Mathf.Abs(dx) > FacingDeadZone)
                sprite.flipX = dx < 0f;
        }

        // Ataque y daño son instantáneos: se disparan con un Trigger, que el Animator consume solo.
        public void PlayAttack() => SetTrigger(AttackParam);

        public void PlayHurt() => SetTrigger(HurtParam);

        public void PlayDeath()
        {
            if (animator == null) return;

            // Al morir se apagan los demás estados, para que ninguna transición le gane a Dead.
            SetLocomotionStopped();
            SetGuarding(false);
            animator.SetBool(DeadParam, true);
        }

        /// Vuelve a Idle desde cualquier estado (por ejemplo, al reusar un enemigo del pool).
        public void ResetToIdle()
        {
            if (animator == null) return;

            animator.Rebind(); // reinicia el Animator como recién creado
            animator.SetBool(DeadParam, false);
            isMoving = false;
            isGuarding = false;
        }

        private void SetLocomotionStopped()
        {
            isMoving = false;
            animator.SetBool(MovingParam, false);
        }

        private void SetTrigger(int param)
        {
            if (animator != null)
                animator.SetTrigger(param);
        }
    }
}
