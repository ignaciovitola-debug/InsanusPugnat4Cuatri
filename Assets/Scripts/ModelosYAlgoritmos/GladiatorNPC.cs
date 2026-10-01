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

        [Header("Feedback de consignas")]
        [Tooltip("Altura sobre el gladiador donde aparece el cartel (\"¡En guardia!\", etc).")]
        [SerializeField] private float feedbackHeight = 2f;

        public Blackboard Blackboard { get; private set; }
        public float CurrentHP { get; private set; }
        public bool IsDead => CurrentHP <= 0f;
        public bool HasSurrendered { get; private set; }
        public float OrderCooldownRatio => combat != null ? combat.OrderCooldownRatio : 0f;

        private Node behaviorTreeRoot;
        private Rigidbody rb;

        private GladiatorMovement movement;
        private GladiatorCombat combat;
        private GladiatorIntentHandler intentHandler;

        private bool combatEnabled = true;
        private string lastAction;
        private bool destroyOnDeath = true;
        private bool invulnerable;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            if (bodyRenderer == null)
                bodyRenderer = GetComponent<Renderer>();

            CurrentHP = maxHP;

            BuildComponents();
            if (intentController != null)
                intentController.SetOwner(this);
            Blackboard = new Blackboard();
            behaviorTreeRoot = BuildTree();

            Debug.Log($"[{gladiatorName}] Listo para combate. HP: {CurrentHP}/{maxHP}");
        }

        private void BuildComponents()
        {
            movement = new GladiatorMovement(transform, rb, moveSpeed,
                avoidCastDistance, avoidRadius, obstacleLayer, knockbackForce);

            combat = new GladiatorCombat(minDamage, maxDamage, attackCooldown,
                staggerDuration);

            intentHandler = new GladiatorIntentHandler(intentController);
        }

       
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            CacheTargetNPC();
        }

        public void SetCombatEnabled(bool enabled) => combatEnabled = enabled;

        public void SetDestroyOnDeath(bool value) => destroyOnDeath = value;

        /// <summary>Usado por CombatStarter en la ola-tutorial invulnerable: TakeDamage no le hace nada mientras esto sea true.</summary>
        public void SetInvulnerable(bool value) => invulnerable = value;

        /// <summary>Restaura la vida al maximo actual (usado al reiniciar el spawn entre olas del tutorial).</summary>
        public void FullyHeal() => CurrentHP = maxHP;

        public void ResetForReuse()
        {
            corpseCleanupDone = false;
            HasSurrendered = false;
            lastAction = null;
            SetColor(Color.white);
        }

        public void SetIntentController(PlayerIntentController controller)
        {
            intentController = controller;
            intentHandler = new GladiatorIntentHandler(controller);
            if (controller != null)
                controller.SetOwner(this);
        }

        public void ConfigureStats(float newMaxHP, float newMinDamage, float newMaxDamage, float newAttackCooldown)
        {
            maxHP = newMaxHP;
            CurrentHP = newMaxHP;
            minDamage = newMinDamage;
            maxDamage = newMaxDamage;
            attackCooldown = newAttackCooldown;
            BuildComponents();
        }
        

        private GladiatorNPC cachedTargetNPC;

        private void CacheTargetNPC()
        {
            cachedTargetNPC = null;
            if (target != null)
                cachedTargetNPC = target.GetComponent<GladiatorNPC>();
        }

        private void Update()
        {
            if (!combatEnabled) return;

            if (IsDead)
            {
                // "¿Estoy muerto?" -> ActionDie
                // limpie el cuerpo.
                Blackboard.Set("target", target);
                Blackboard.Set("self", this);
                behaviorTreeRoot.Tick(Blackboard);
                return;
            }

            combat.Tick(Time.deltaTime);

            if (combat.IsStunned)
            {
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
                    onTrue: new ActionNode("Defender", ActionHoldGuard)),
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

        //Condiciones
        private bool AmIDead(Blackboard bb) => IsDead;

        private bool WantsToSurrender(Blackboard bb)
            => intentHandler.HasController && intentHandler.TryConsume(PlayerIntent.Surrender);

        private bool IsTargetDead(Blackboard bb)
            => cachedTargetNPC != null && cachedTargetNPC.IsDead;

        private bool IsTargetInAttackRange(Blackboard bb)
        {
            if (target == null) return false;
            return Vector3.Distance(transform.position, target.position) <= attackRange;
        }

        private bool IsTargetInDetectionRange(Blackboard bb)
        {
            if (target == null) return false;
            return Vector3.Distance(transform.position, target.position) <= detectionRange;
        }

        private bool IsDefending(Blackboard bb) => combat.IsDefending;

        // Acciones
        private NodeState ActionAttack(Blackboard bb)
        {
            SetColor(Color.red);
            movement.Stop();

            if (combat.IsOnCooldown) return NodeState.Running;
            if (cachedTargetNPC == null) return NodeState.Failure;

            float damage = combat.RollDamage();
            cachedTargetNPC.TakeDamage(damage, gladiatorName, transform.position, this);
            combat.RegisterAttack();

            LogAction("Attack", $">>> GOLPE a {cachedTargetNPC.gladiatorName}! Daño: {damage} | HP enemigo: {cachedTargetNPC.CurrentHP}/{cachedTargetNPC.maxHP}");
            return NodeState.Success;
        }

        private NodeState ActionChase(Blackboard bb)
        {
            SetColor(new Color(1f, 0.5f, 0f));
            if (target == null) return NodeState.Failure;

            LogAction("Chase", $"Persiguiendo enemigo... distancia: {Vector3.Distance(transform.position, target.position):F1}m");
            movement.MoveToward(target.position, target);
            return NodeState.Running;
        }

        private NodeState ActionPatrol(Blackboard bb)
        {
            SetColor(Color.green);
            if (target == null)
            {
                movement.Stop();
                LogAction("Patrol", "Patrullando, sin objetivo asignado...");
                return NodeState.Running;
            }

            LogAction("Patrol", $"Buscando enemigo... distancia: {Vector3.Distance(transform.position, target.position):F1}m");
            movement.MoveToward(target.position, target);
            return NodeState.Running;
        }

        private bool corpseCleanupDone;

        private NodeState ActionDie(Blackboard bb)
        {
            SetColor(Color.gray);
            movement.Stop();
            LogAction("Dead", "MUERTO.");

            if (!corpseCleanupDone)
            {
                corpseCleanupDone = true;
                EventManager.Raise(new GladiatorDiedEvent(gladiatorName));

                if (destroyOnDeath)
                    Destroy(gameObject); // sin delay: saca TODO (collider, sprite, script) de encima 
            }

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

            LogAction("Surrender", $"{gladiatorName} se rinde! {cachedTargetNPC?.gladiatorName ?? "El rival"} gana el combate.");
            ShowFeedback("¡Me rindo!", Color.white);

            SetCombatEnabled(false);
            cachedTargetNPC?.SetCombatEnabled(false);

            return NodeState.Success;
        }

        // Mientras dura la espera entre ordenes no se consume la consigna: antes se tiraba
        // sin hacer nada y el jugador sentia que el gladiador lo ignoraba.
        private NodeState ApplyAttackIntent(Blackboard bb)
        {
            if (combat.CanReceiveOrder && intentHandler.TryConsume(PlayerIntent.Attack) && combat.TryRedoubleAttack())
            {
                LogAction("Intent", $"{gladiatorName} redobla el ataque por orden del jugador!");
                ShowFeedback("¡A la carga!", new Color(1f, 0.45f, 0.3f));
            }

            return NodeState.Failure;
        }

        private NodeState ApplyDefendIntent(Blackboard bb)
        {
            if (combat.CanReceiveOrder && intentHandler.TryConsume(PlayerIntent.Defend) && combat.TryDefend(2f))
            {
                LogAction("Intent", $"{gladiatorName} se pone en guardia por orden del jugador!");
                ShowFeedback("¡En guardia!", new Color(0.4f, 0.85f, 1f));
            }

            return NodeState.Failure;
        }

        private NodeState ActionHoldGuard(Blackboard bb)
        {
            SetColor(Color.cyan);
            movement.Stop();
            return NodeState.Running;
        }

        public void Startle(Vector3 sourcePosition)
        {
            if (IsDead) return;
            movement.ApplyKnockback(sourcePosition);
            combat.ApplyStagger();
        }

        //  Daño
        public void TakeDamage(float damage, string attackerName, Vector3 attackerPosition, GladiatorNPC attacker = null)
        {
            if (IsDead) return;

            if (invulnerable)
            {
                movement.ApplyKnockback(attackerPosition);
                combat.ApplyStagger();
                return;
            }

            if (combat.IsDefending)
            {
                // Parry: el golpe no hace daño, el ATACANTE es quien recibe el empujón.
                Debug.Log($"[{gladiatorName}] Paró el golpe de {attackerName}!");
                ShowFeedback("¡Bloqueó!", new Color(0.4f, 0.85f, 1f));
                attacker?.Startle(transform.position);
                return;
            }

            CurrentHP = Mathf.Max(0f, CurrentHP - damage);
            Debug.Log($"[{gladiatorName}] Recibió {damage} daño de {attackerName}. HP: {CurrentHP}/{maxHP}");

            movement.ApplyKnockback(attackerPosition);
            combat.ApplyStagger();

            if (IsDead)
                Debug.Log($"[{gladiatorName}] HA CAÍDO EN COMBATE!");
        }

        //  Utilidades 
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

        private void ShowFeedback(string message, Color color)
            => FloatingText.Spawn(transform.position + Vector3.up * feedbackHeight, message, color);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
