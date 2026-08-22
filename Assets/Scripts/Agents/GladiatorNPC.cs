using UnityEngine;

namespace GladiusAI
{
    [RequireComponent(typeof(Rigidbody))]
    public class GladiatorNPC : MonoBehaviour
    {
        [Header("Identidad")]
        [SerializeField] private string gladiatorName = "Gladiator";

        [Header("Objetivo (asignar en Inspector)")]
        [SerializeField] private Transform target;

        [Header("Movimiento")]
        [SerializeField] private float moveSpeed = 2.5f;

        [Header("Rangos")]
        [SerializeField] private float detectionRange = 6f;
        [SerializeField] private float attackRange = 1.5f;

        [Header("Combate")]
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private float minDamage = 10f;
        [SerializeField] private float maxDamage = 20f;
        [SerializeField] private float attackCooldown = 1.2f;

        [Header("Knockback / Stagger")]
        [SerializeField] private float knockbackForce = 5f;
        [SerializeField] private float staggerDuration = 0.4f;

        [Header("Debug visual")]
        [SerializeField] private Renderer bodyRenderer;

        public Blackboard Blackboard { get; private set; }
        public float CurrentHP { get; private set; }
        public bool IsDead => CurrentHP <= 0f;

        private Node behaviorTreeRoot;
        private Rigidbody rb;
        private float attackTimer;
        private float stunTimer;
        private string lastAction;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            if (bodyRenderer == null)
                bodyRenderer = GetComponent<Renderer>();

            CurrentHP = maxHP;
            Blackboard = new Blackboard();
            behaviorTreeRoot = BuildTree();

            Debug.Log($"[{gladiatorName}] Listo para combate. HP: {CurrentHP}/{maxHP}");
        }

        private void Update()
        {
            if (IsDead) return;

            if (attackTimer > 0f)
                attackTimer -= Time.deltaTime;

            if (stunTimer > 0f)
            {
                stunTimer -= Time.deltaTime;
                SetColor(Color.white);
                Stop();
                return;
            }

            Blackboard.Set("target", target);
            Blackboard.Set("self", this);
            behaviorTreeRoot.Tick(Blackboard);
        }

        //  Selector (Root)
        //  ├── "¿Estoy muerto?" → Morir
        //  ├── "¿Enemigo muerto?" → Victoria
        //  ├── Sequence (Atacar)
        //  │   ├── "¿En rango de ataque?"
        //  │   └── Atacar
        //  ├── "¿En rango de detección?" → Perseguir
        //  └── Patrullar
        private Node BuildTree()
        {
            return new Selector("Root",
                new QuestionNode("¿Estoy muerto?", AmIDead,
                    onTrue: new ActionNode("Morir", ActionDie)),
                new QuestionNode("¿Enemigo muerto?", IsTargetDead,
                    onTrue: new ActionNode("Victoria", ActionVictory)),
                new Sequence("Secuencia Ataque",
                    new QuestionNode("¿En rango de ataque?", IsTargetInAttackRange,
                        onTrue: new ActionNode("CheckOK", (bb) => NodeState.Success)),
                    new ActionNode("Atacar", ActionAttack)
                ),
                new QuestionNode("¿En rango de detección?", IsTargetInDetectionRange,
                    onTrue: new ActionNode("Perseguir", ActionChase)),
                new ActionNode("Patrullar", ActionPatrol)
            );
        }

        // ==================== Condiciones ====================

        private bool AmIDead(Blackboard bb) => IsDead;

        private bool IsTargetDead(Blackboard bb)
        {
            var targetNPC = GetTargetNPC(bb);
            return targetNPC != null && targetNPC.IsDead;
        }

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

        // ==================== Acciones ====================

        private NodeState ActionAttack(Blackboard bb)
        {
            SetColor(Color.red);
            Stop();

            if (attackTimer > 0f) return NodeState.Running;

            var targetNPC = GetTargetNPC(bb);
            if (targetNPC == null) return NodeState.Failure;

            float damage = Mathf.Round(Random.Range(minDamage, maxDamage));
            targetNPC.TakeDamage(damage, gladiatorName, transform.position);
            attackTimer = attackCooldown;

            LogAction("Attack", $">>> GOLPE a {targetNPC.gladiatorName}! Daño: {damage} | HP enemigo: {targetNPC.CurrentHP}/{targetNPC.maxHP}");
            return NodeState.Success;
        }

        private NodeState ActionChase(Blackboard bb)
        {
            SetColor(new Color(1f, 0.5f, 0f));
            var t = bb.Get<Transform>("target");
            if (t == null) return NodeState.Failure;

            float dist = Vector3.Distance(transform.position, t.position);
            LogAction("Chase", $"Persiguiendo enemigo... distancia: {dist:F1}m");

            MoveToward(t.position);
            return NodeState.Running;
        }

        private NodeState ActionPatrol(Blackboard bb)
        {
            SetColor(Color.green);
            var t = bb.Get<Transform>("target");
            if (t == null)
            {
                Stop();
                LogAction("Patrol", "Patrullando, sin objetivo asignado...");
                return NodeState.Running;
            }

            float dist = Vector3.Distance(transform.position, t.position);
            LogAction("Patrol", $"Buscando enemigo... distancia: {dist:F1}m");
            MoveToward(t.position);
            return NodeState.Running;
        }

        private NodeState ActionDie(Blackboard bb)
        {
            SetColor(Color.gray);
            Stop();
            LogAction("Dead", "MUERTO.");
            return NodeState.Success;
        }

        private NodeState ActionVictory(Blackboard bb)
        {
            SetColor(Color.yellow);
            Stop();
            LogAction("Victory", "VICTORIA! Enemigo derrotado.");
            return NodeState.Success;
        }

        // ==================== Daño ====================

        public void TakeDamage(float damage, string attackerName, Vector3 attackerPosition)
        {
            if (IsDead) return;

            CurrentHP = Mathf.Max(0f, CurrentHP - damage);
            Debug.Log($"[{gladiatorName}] Recibió {damage} daño de {attackerName}. HP: {CurrentHP}/{maxHP}");

            Vector3 knockDir = (transform.position - attackerPosition).normalized;
            knockDir.y = 0f;
            rb.AddForce(knockDir * knockbackForce, ForceMode.Impulse);
            stunTimer = staggerDuration;

            if (IsDead)
                Debug.Log($"[{gladiatorName}] HA CAÍDO EN COMBATE!");
        }

        // ==================== Utilidades ====================

        private GladiatorNPC GetTargetNPC(Blackboard bb)
        {
            var t = bb.Get<Transform>("target");
            if (t == null) return null;
            return t.GetComponent<GladiatorNPC>();
        }

        private void LogAction(string action, string message)
        {
            if (lastAction == action) return;
            lastAction = action;
            Debug.Log($"[{gladiatorName}] {message}");
        }

        private void MoveToward(Vector3 targetPos)
        {
            Vector3 dir = targetPos - transform.position;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;

            Vector3 vel = dir * moveSpeed;
            vel.y = rb.linearVelocity.y;
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
