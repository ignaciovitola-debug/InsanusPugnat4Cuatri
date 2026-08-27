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

        [Header("Evasión de obstáculos")]
        [SerializeField] private float avoidCastDistance = 1.5f;
        [SerializeField] private float avoidRadius = 0.4f;
        [SerializeField] private LayerMask obstacleLayer;

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

        [Header("Consigna del jugador (solo en el gladiador del jugador)")]
        [SerializeField] private PlayerIntentController intentController;

        [Header("Debug visual")]
        [SerializeField] private Renderer bodyRenderer;

        public Blackboard Blackboard { get; private set; }
        public float CurrentHP { get; private set; }
        public bool IsDead => CurrentHP <= 0f;
        public bool HasSurrendered { get; private set; }

        private Node behaviorTreeRoot;
        private Rigidbody rb;

        private GladiatorMovement movement;
        private GladiatorCombat combat;
        private GladiatorIntentHandler intentHandler;

        private float stunTimer;
        private float defendTimer;
        private bool combatEnabled = true;
        private string lastAction;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            if (bodyRenderer == null)
                bodyRenderer = GetComponent<Renderer>();

            CurrentHP = maxHP;

            BuildComponents();
            Blackboard = new Blackboard();
            behaviorTreeRoot = BuildTree();

            Debug.Log($"[{gladiatorName}] Listo para combate. HP: {CurrentHP}/{maxHP}");
        }

        private void BuildComponents()
        {
            movement = new GladiatorMovement(transform, rb, moveSpeed,
                avoidCastDistance, avoidRadius, obstacleLayer);

            combat = new GladiatorCombat(minDamage, maxDamage, attackCooldown,
                knockbackForce, staggerDuration);

            intentHandler = new GladiatorIntentHandler(intentController);
        }

        // ============ Para la Factory / wiring externo ============
        public void ApplyStats(GladiatorStats stats)
        {
            gladiatorName = stats.gladiatorName;
            maxHP = stats.maxHP;
            minDamage = stats.minDamage;
            maxDamage = stats.maxDamage;
            attackCooldown = stats.attackCooldown;

            CurrentHP = maxHP;
            BuildComponents(); // recreamos combat con los stats nuevos
        }

        public void SetTarget(Transform newTarget) => target = newTarget;
        public void SetCombatEnabled(bool enabled) => combatEnabled = enabled;

        public void SetIntentController(PlayerIntentController controller)
        {
            intentController = controller;
            intentHandler = new GladiatorIntentHandler(controller);
        }
        // ============================================================

        private void Update()
        {
            if (IsDead) return;
            if (!combatEnabled) return;

            combat.Tick(Time.deltaTime);

            if (defendTimer > 0f)
                defendTimer -= Time.deltaTime;

            if (stunTimer > 0f)
            {
                stunTimer -= Time.deltaTime;
                SetColor(Color.white);
                movement.Stop();
                return;
            }

            Blackboard.Set("target", target);
            Blackboard.Set("self", this);
            behaviorTreeRoot.Tick(Blackboard);
        }

        private Node BuildTree()
        {
            return new Selector("Root",
                new QuestionNode("¿Estoy muerto?", AmIDead,
                    onTrue: new ActionNode("Morir", ActionDie)),
                new QuestionNode("¿Consigna de Rendirse?", WantsToSurrender,
                    onTrue: new ActionNode("Rendirse", ActionSurrender)),
                new QuestionNode("¿Enemigo muerto?", IsTargetDead,
                    onTrue: new ActionNode("Victoria", ActionVictory)),
                new ActionNode("Aplicar consigna Atacar", ApplyAttackIntent),
                new ActionNode("Aplicar consigna Defender", ApplyDefendIntent),
                new QuestionNode("¿En guardia?", IsDefending,
                    onTrue: new ActionNode("Retroceder", ActionRetreat)),
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

        private bool WantsToSurrender(Blackboard bb)
            => intentHandler.HasController && intentHandler.TryConsume(PlayerIntent.Surrender);

        private bool IsTargetDead(Blackboard bb)
        {
            var t = GetTargetNPC(bb);
            return t != null && t.IsDead;
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

        private bool IsDefending(Blackboard bb) => defendTimer > 0f;

        // ==================== Acciones ====================
        private NodeState ActionAttack(Blackboard bb)
        {
            SetColor(Color.red);
            movement.Stop();

            if (combat.IsOnCooldown) return NodeState.Running;

            var targetNPC = GetTargetNPC(bb);
            if (targetNPC == null) return NodeState.Failure;

            float damage = combat.RollDamage();
            targetNPC.TakeDamage(damage, gladiatorName, transform.position);
            combat.RegisterAttack();

            LogAction("Attack", $">>> GOLPE a {targetNPC.gladiatorName}! Daño: {damage} | HP enemigo: {targetNPC.CurrentHP}/{targetNPC.maxHP}");
            return NodeState.Success;
        }

        private NodeState ActionChase(Blackboard bb)
        {
            SetColor(new Color(1f, 0.5f, 0f));
            var t = bb.Get<Transform>("target");
            if (t == null) return NodeState.Failure;

            LogAction("Chase", $"Persiguiendo enemigo... distancia: {Vector3.Distance(transform.position, t.position):F1}m");
            movement.MoveToward(t.position);
            return NodeState.Running;
        }

        private NodeState ActionPatrol(Blackboard bb)
        {
            SetColor(Color.green);
            var t = bb.Get<Transform>("target");
            if (t == null)
            {
                movement.Stop();
                LogAction("Patrol", "Patrullando, sin objetivo asignado...");
                return NodeState.Running;
            }

            LogAction("Patrol", $"Buscando enemigo... distancia: {Vector3.Distance(transform.position, t.position):F1}m");
            movement.MoveToward(t.position);
            return NodeState.Running;
        }

        private NodeState ActionDie(Blackboard bb)
        {
            SetColor(Color.gray);
            movement.Stop();
            LogAction("Dead", "MUERTO.");
            return NodeState.Success;
        }

        private NodeState ActionVictory(Blackboard bb)
        {
            SetColor(Color.yellow);
            movement.Stop();
            LogAction("Victory", "VICTORIA! Enemigo derrotado.");
            return NodeState.Success;
        }

        private NodeState ActionSurrender(Blackboard bb)
        {
            SetColor(Color.blue);
            movement.Stop();
            HasSurrendered = true;

            var targetNPC = GetTargetNPC(bb);
            LogAction("Surrender", $"{gladiatorName} se rinde! {targetNPC?.gladiatorName ?? "El rival"} gana el combate.");

            SetCombatEnabled(false);
            targetNPC?.SetCombatEnabled(false);

            return NodeState.Success;
        }

        private NodeState ApplyAttackIntent(Blackboard bb)
        {
            if (intentHandler.TryConsume(PlayerIntent.Attack))
            {
                combat.ResetCooldown();
                LogAction("Intent", $"{gladiatorName} redobla el ataque por orden del jugador!");
            }
            return NodeState.Failure;
        }

        private NodeState ApplyDefendIntent(Blackboard bb)
        {
            if (intentHandler.TryConsume(PlayerIntent.Defend))
            {
                defendTimer = 2f;
                LogAction("Intent", $"{gladiatorName} se pone en guardia por orden del jugador!");
            }
            return NodeState.Failure;
        }

        private NodeState ActionRetreat(Blackboard bb)
        {
            SetColor(Color.cyan);
            var t = bb.Get<Transform>("target");
            if (t == null) { movement.Stop(); return NodeState.Running; }

            movement.MoveAway(t.position);
            return NodeState.Running;
        }

        // ==================== Daño ====================
        public void TakeDamage(float damage, string attackerName, Vector3 attackerPosition)
        {
            if (IsDead) return;

            CurrentHP = Mathf.Max(0f, CurrentHP - damage);
            Debug.Log($"[{gladiatorName}] Recibió {damage} daño de {attackerName}. HP: {CurrentHP}/{maxHP}");

            combat.ApplyKnockback(rb, transform.position, attackerPosition, out float stagger);
            stunTimer = stagger;

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