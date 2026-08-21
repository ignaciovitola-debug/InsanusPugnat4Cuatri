using UnityEngine;

namespace GladiusAI
{
    /// <summary>
    /// Versión BÁSICA del gladiador, pensada para probar el árbol de
    /// comportamiento en la escena de prueba de la clase de IA.
    ///
    /// A propósito NO tiene armas, stamina, ni managers externos: cada
    /// NPC decide y se mueve por su cuenta, y el "enemigo" se asigna a
    /// mano desde el Inspector. Esto es justo para poder mirar el árbol
    /// (Selector → QuestionNode → ActionNode) sin ruido de otras cosas.
    /// Más adelante lo vamos a ir ampliando de a poco.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class GladiatorNPC : MonoBehaviour
    {
        [Header("Objetivo (para la prueba, asignalo a mano en el Inspector)")]
        [SerializeField] private Transform target;

        [Header("Movimiento")]
        [SerializeField] private float moveSpeed = 2.5f;

        [Header("Rangos")]
        [SerializeField] private float detectionRange = 6f;
        [SerializeField] private float attackRange = 1.5f;

        [Header("Debug visual")]
        [Tooltip("Si se deja vacío, se busca automáticamente en este GameObject.")]
        [SerializeField] private Renderer bodyRenderer;

        public Blackboard Blackboard { get; private set; }

        private Node behaviorTreeRoot;
        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation; // que no ruede/vuelque

            if (bodyRenderer == null)
                bodyRenderer = GetComponent<Renderer>();

            Blackboard = new Blackboard();
            behaviorTreeRoot = BuildTree();
        }

        private void Update()
        {
            // Guardamos el objetivo en el Blackboard antes de tickear, así
            // el árbol siempre lee el dato más actualizado.
            Blackboard.Set("target", target);
            behaviorTreeRoot.Tick(Blackboard);
        }

        // ==================== Árbol de comportamiento ====================
        //
        //  Selector (Root)
        //  ├── "¿Enemigo en rango de ataque?" → Atacar
        //  ├── "¿Enemigo en rango de detección?" → Perseguir
        //  └── Patrullar   (fallback si nada de lo anterior aplicó)
        //
        private Node BuildTree()
        {
            return new Selector("Root",
                new QuestionNode("¿Enemigo en rango de ataque?", IsTargetInAttackRange,
                    onTrue: new ActionNode("Atacar", ActionAttack)),
                new QuestionNode("¿Enemigo en rango de detección?", IsTargetInDetectionRange,
                    onTrue: new ActionNode("Perseguir", ActionChase)),
                new ActionNode("Patrullar", ActionPatrol)
            );
        }

        // ==================== Condiciones (QuestionNode) ====================

        private bool IsTargetInAttackRange(Blackboard bb)
        {
            var t = bb.Get<Transform>("target");
            if (t == null) return false;
            return Vector3.Distance(transform.position, t.position) <= attackRange;
        }

        private bool IsTargetInDetectionRange(Blackboard bb)
        {
            var t = bb.Get<Transform>("target");
            if (t == null) return false;
            return Vector3.Distance(transform.position, t.position) <= detectionRange;
        }

        // ==================== Acciones (ActionNode) ====================

        private NodeState ActionAttack(Blackboard bb)
        {
            SetColor(Color.red);
            Stop();
            // TODO: acá va el daño real más adelante. Por ahora solo
            // mostramos con el color que el árbol eligió "Atacar".
            return NodeState.Success;
        }

        private NodeState ActionChase(Blackboard bb)
        {
            SetColor(new Color(1f, 0.5f, 0f)); // naranja
            var t = bb.Get<Transform>("target");
            if (t == null) return NodeState.Failure;

            MoveToward(t.position);
            return NodeState.Running;
        }

        private NodeState ActionPatrol(Blackboard bb)
        {
            SetColor(Color.green);
            // TODO: waypoints reales más adelante. Por ahora se queda quieto.
            Stop();
            return NodeState.Running;
        }

        // ==================== Utilidades de movimiento (plano XZ) ====================

        private void MoveToward(Vector3 targetPos)
        {
            Vector3 dir = targetPos - transform.position;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;

            Vector3 vel = dir * moveSpeed;
            vel.y = rb.linearVelocity.y; // no pisar la gravedad
            rb.linearVelocity = vel;
        }

        private void Stop()
        {
            Vector3 vel = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, vel.y, 0f);
        }

        private void SetColor(Color c)
        {
            if (bodyRenderer != null)
                bodyRenderer.material.color = c;
        }

        // Para ver los rangos como círculos en el Scene view mientras prueban.
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
