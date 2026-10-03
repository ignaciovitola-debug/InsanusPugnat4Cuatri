using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace GladiusAI
{
    [RequireComponent(typeof(Rigidbody))]
    public class GladiatorNPC : MonoBehaviour
    {
        /// Gladiadores activos en la escena (lo usa ArenaBeast para no hacer FindObjectsByType cada frame).
        public static readonly List<GladiatorNPC> All = new List<GladiatorNPC>();

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

        [Header("Animaciones")]
        [Tooltip("Segundos que queda el cuerpo en la arena después de morir (para que se vea la animación Dead).")]
        [SerializeField] private float corpseLifetime = 2f;
        [Tooltip("Velocidad mínima para considerar que está caminando (si no, Idle).")]
        [SerializeField] private float walkSpeedThreshold = 0.2f;

        public Blackboard Blackboard { get; private set; }
        public float CurrentHP { get; private set; }
        public bool IsDead => CurrentHP <= 0f;
        public bool HasSurrendered { get; private set; }
        public float OrderCooldownRatio => combat != null ? combat.OrderCooldownRatio : 0f;

        /// Sprite animado del personaje (no incluye la sombra de los pies).
        public SpriteRenderer CharacterSprite { get; private set; }

        private Node behaviorTreeRoot;
        private Rigidbody rb;
        private Collider[] bodyColliders;

        private GladiatorMovement movement;
        private GladiatorCombat combat;
        private GladiatorIntentHandler intentHandler;
        private GladiatorAnimator animator;

        private bool combatEnabled = true;
        private string lastAction;
        private bool destroyOnDeath = true;
        private bool invulnerable;
        private bool corpseCleanupDone;
        private Color currentColor;
        private bool hasColor;

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            // La fisica corre a paso fijo (50 Hz) y la pantalla a 60+: sin interpolar,
            // el gladiador avanza "a saltos" entre pasos de fisica y se ven tirones.
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            if (bodyRenderer == null)
                bodyRenderer = GetComponent<Renderer>();

            bodyColliders = GetComponentsInChildren<Collider>();

            // El sprite del personaje es el que lleva el Animator; el gladiador tiene otros
            // SpriteRenderer (la sombra de los pies) que no se animan ni se espejan.
            var unityAnimator = GetComponentInChildren<Animator>();
            CharacterSprite = unityAnimator != null ? unityAnimator.GetComponent<SpriteRenderer>() : null;
            animator = new GladiatorAnimator(unityAnimator, CharacterSprite, walkSpeedThreshold);

            CurrentHP = maxHP;

            BuildComponents();
            if (intentController != null)
                intentController.SetOwner(this);
            Blackboard = new Blackboard();
            behaviorTreeRoot = BuildTree();

            Log($"Listo para combate. HP: {CurrentHP}/{maxHP}");
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

        public void SetCombatEnabled(bool enabled)
        {
            combatEnabled = enabled;
            // Con el combate apagado Update no corre: si no se frena aca, sigue deslizandose con la ultima velocidad.
            if (!enabled)
                movement?.Stop();
        }

        /// Mueve al gladiador al instante y sin inercia. Hay que mover el Rigidbody y no solo el transform:
        /// con interpolacion activada, la fisica pisaria el transform y lo devolveria a donde estaba.
        public void TeleportTo(Vector3 position)
        {
            rb.position = position;
            transform.position = position;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        public void SetDestroyOnDeath(bool value) => destroyOnDeath = value;

        /// Usado por CombatStarter en la ola-tutorial invulnerable: TakeDamage no le hace nada mientras esto sea true.
        public void SetInvulnerable(bool value) => invulnerable = value;

        /// Restaura la vida al maximo actual (usado al reiniciar el spawn entre olas del tutorial).
        public void FullyHeal() => CurrentHP = maxHP;

        public void ResetForReuse()
        {
            corpseCleanupDone = false;
            HasSurrendered = false;
            lastAction = null;
            SetColor(Color.white);
            SetPhysicalBodyEnabled(true);
            animator.ResetToIdle();
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
            TickBehaviour();
            UpdateAnimation();
        }

        private void TickBehaviour()
        {
            // Los tiempos de espera (entre órdenes, entre golpes, aturdimiento) corren aunque el combate
            // esté en pausa: si no, una orden dada justo antes de ganar dejaba los botones bloqueados
            // durante todo el cartel del tutorial siguiente, y no había forma de arrancar el combate.
            if (!IsDead)
                combat.Tick(Time.deltaTime);

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
            animator.PlayAttack();

            if (IsNewAction("Attack"))
                Log($">>> GOLPE a {cachedTargetNPC.gladiatorName}! Daño: {damage} | HP enemigo: {cachedTargetNPC.CurrentHP}/{cachedTargetNPC.maxHP}");
            return NodeState.Success;
        }

        private NodeState ActionChase(Blackboard bb)
        {
            SetColor(new Color(1f, 0.5f, 0f));
            if (target == null) return NodeState.Failure;

            // El texto se arma solo si de verdad se va a loguear: antes se construia un string nuevo
            // cada frame (aunque despues se descartara) y esa basura le hacia saltar al GC.
            if (IsNewAction("Chase"))
                Log($"Persiguiendo enemigo... distancia: {Vector3.Distance(transform.position, target.position):F1}m");
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

            if (IsNewAction("Patrol"))
                Log($"Buscando enemigo... distancia: {Vector3.Distance(transform.position, target.position):F1}m");
            movement.MoveToward(target.position, target);
            return NodeState.Running;
        }

        private NodeState ActionDie(Blackboard bb)
        {
            SetColor(Color.gray);
            movement.Stop();
            LogAction("Dead", "MUERTO.");

            if (!corpseCleanupDone)
            {
                corpseCleanupDone = true;
                EventManager.Raise(new GladiatorDiedEvent(gladiatorName));
                animator.PlayDeath();

                // El cuerpo deja de chocar al instante (no estorba al resto), pero se queda
                // unos segundos para que se vea la animación Dead antes de sacarlo.
                SetPhysicalBodyEnabled(false);
                if (destroyOnDeath)
                    Destroy(gameObject, corpseLifetime);
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

            if (IsNewAction("Surrender"))
                Log($"{gladiatorName} se rinde! {cachedTargetNPC?.gladiatorName ?? "El rival"} gana el combate.");
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
                if (IsNewAction("Intent"))
                    Log($"{gladiatorName} redobla el ataque por orden del jugador!");
                ShowFeedback("¡A la carga!", new Color(1f, 0.45f, 0.3f));
            }

            return NodeState.Failure;
        }

        private NodeState ApplyDefendIntent(Blackboard bb)
        {
            if (combat.CanReceiveOrder && intentHandler.TryConsume(PlayerIntent.Defend) && combat.TryDefend(2f))
            {
                if (IsNewAction("Intent"))
                    Log($"{gladiatorName} se pone en guardia por orden del jugador!");
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
                SoundManager.Play(SoundId.SwordHit);
                Flinch(attackerPosition);
                return;
            }

            if (combat.IsDefending)
            {
                // Parry: el golpe no hace daño, el ATACANTE es quien recibe el empujón.
                Log($"Paró el golpe de {attackerName}!");
                SoundManager.Play(SoundId.ShieldBlock);
                ShowFeedback("¡Bloqueó!", new Color(0.4f, 0.85f, 1f));
                attacker?.Startle(transform.position);
                return;
            }

            CurrentHP = Mathf.Max(0f, CurrentHP - damage);
            Log($"Recibió {damage} daño de {attackerName}. HP: {CurrentHP}/{maxHP}");

            if (IsDead)
            {
                Log("HA CAÍDO EN COMBATE!");
                SoundManager.Play(SoundId.KillingBlow);
                movement.ApplyKnockback(attackerPosition);
                return;
            }

            SoundManager.Play(SoundId.SwordHit);
            Flinch(attackerPosition);
        }

        /// Reacción a un golpe que no lo mata: empujón, aturdimiento y animación Hurt.
        private void Flinch(Vector3 attackerPosition)
        {
            movement.ApplyKnockback(attackerPosition);
            combat.ApplyStagger();
            animator.PlayHurt();
        }

        //  Animación

        /// Estados continuos (Idle/Walk, Block mientras dure la guardia) y orientación.
        /// Attack, Hurt y Dead son instantáneos: los disparan las acciones que los causan.
        private void UpdateAnimation()
        {
            if (IsDead) return;

            animator.UpdateLocomotion(rb.linearVelocity);
            animator.SetGuarding(combat.IsDefending);
            if (target != null)
                animator.FaceTowards(transform.position, target.position);
        }

        /// Al morir el cuerpo deja de chocar y queda clavado en el lugar; al reusarlo vuelve a la normalidad.
        private void SetPhysicalBodyEnabled(bool enabled)
        {
            foreach (var bodyCollider in bodyColliders)
                bodyCollider.enabled = enabled;

            rb.constraints = enabled ? RigidbodyConstraints.FreezeRotation : RigidbodyConstraints.FreezeAll;
        }

        //  Utilidades

        /// true solo la primera vez que se entra a esta accion (para no repetir el mismo log cada frame).
        private bool IsNewAction(string action)
        {
            if (lastAction == action) return false;
            lastAction = action;
            return true;
        }

        private void LogAction(string action, string message)
        {
            if (IsNewAction(action))
                Log(message);
        }

        // Solo existe en el Editor y en Development Builds: en el build final para el celular
        // el compilador borra estas llamadas (y el armado de sus textos), que en mobile son caras.
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private void Log(string message) => Debug.Log($"[{gladiatorName}] {message}");

        /// Color de debug del estado (rojo atacando, verde patrullando...) sobre la forma básica del gladiador.
        /// En los prefabs esa forma está apagada (la sombra de los pies es el hijo "Shadow");
        /// si se reactiva su MeshRenderer, los colores de debug vuelven a verse.
        private void SetColor(Color c)
        {
            if (!IsBodyShapeVisible || (hasColor && currentColor == c)) return;
            currentColor = c;
            hasColor = true;
            bodyRenderer.material.color = c;
        }

        private bool IsBodyShapeVisible =>
            bodyRenderer != null && bodyRenderer.enabled && bodyRenderer.shadowCastingMode != ShadowCastingMode.ShadowsOnly;

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
