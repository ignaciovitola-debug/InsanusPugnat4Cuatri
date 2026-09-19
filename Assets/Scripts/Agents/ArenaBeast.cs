using UnityEngine;

namespace GladiusAI
{
    /// <summary>
    /// Amenaza ambiental de la arena: patrulla, y si un gladiador se acerca
    /// demasiado lo caza y lo aparta de un golpe (sin quitarle vida) antes de
    /// volver a patrullar. Es el segundo elemento con comportamiento complejo
    /// pedido por Aplicación de Motores 2 — sistema propio y separado del
    /// Hunter/Boid de Inteligencia Artificial 1 (no lo toca ni depende de él),
    /// aunque reutiliza la misma librería de steering (SteeringBehaviors).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ArenaBeast : MonoBehaviour
    {
        private enum BeastState { Patrol, Chase, Recover }

        [Header("Movimiento")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float maxForce = 14f;
        [SerializeField] private float turnSpeed = 400f;
        [SerializeField] private float bodyRadius = 0.6f;

        [Header("Evasión de obstáculos (Columnas)")]
        [SerializeField] private float avoidCastDistance = 2f;
        [SerializeField] private float avoidProbeRadius = 0.6f;
        [SerializeField] private LayerMask obstacleLayer;

        [Header("Patrulla (ida y vuelta)")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float waypointArriveDistance = 0.5f;

        [Header("Detección y carga")]
        [SerializeField] private float detectionRange = 5f;
        [SerializeField] private float chargeDistance = 1.2f;
        [SerializeField] private float recoverDuration = 2f;
        [Tooltip("Después de embestir, cuánto tiempo sigue patrullando antes de volver a buscar objetivo — evita que se quede trabada re-cazando al mismo gladiador si sigue cerca.")]
        [SerializeField] private float reEngageCooldown = 3f;

        [Header("Debug visual")]
        [SerializeField] private Renderer bodyRenderer;

        private BeastState state = BeastState.Patrol;
        private Vector3 velocity;
        private int waypointIndex;
        private int waypointDirection = 1;
        private float recoverTimer;
        private float cooldownTimer;
        private GladiatorNPC target;
        private bool combatStarted;

        private void Awake()
        {
            if (bodyRenderer == null)
                bodyRenderer = GetComponentInChildren<Renderer>();
        }

        private void OnEnable() => EventManager.Subscribe<CombatStartedEvent>(OnCombatStarted);
        private void OnDisable() => EventManager.Unsubscribe<CombatStartedEvent>(OnCombatStarted);

        private void OnCombatStarted(CombatStartedEvent e) => combatStarted = true;

        private void Update()
        {
            if (!combatStarted) return;

            switch (state)
            {
                case BeastState.Patrol: TickPatrol(); break;
                case BeastState.Chase: TickChase(); break;
                case BeastState.Recover: TickRecover(); break;
            }

            Vector3 delta = velocity * Time.deltaTime;
            transform.position = SteeringBehaviors.MoveAndCollide(transform.position, delta, bodyRadius, obstacleLayer);
            SteeringBehaviors.FaceDirection(transform, velocity, turnSpeed, Time.deltaTime);
        }

        private void TickPatrol()
        {
            SetColor(Color.green);
            MoveAlongWaypoints();

            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
                return;
            }

            GladiatorNPC found = FindNearbyGladiator();
            if (found != null)
            {
                target = found;
                state = BeastState.Chase;
            }
        }

        private void TickChase()
        {
            SetColor(Color.red);

            if (target == null || target.IsDead)
            {
                target = null;
                state = BeastState.Patrol;
                return;
            }

            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance > detectionRange * 1.5f)
            {
                target = null;
                state = BeastState.Patrol;
                return;
            }

            if (distance <= chargeDistance)
            {
                target.Startle(transform.position);
                velocity = Vector3.zero;
                recoverTimer = recoverDuration;
                cooldownTimer = reEngageCooldown;
                state = BeastState.Recover;
                return;
            }

            Vector3 steer = SteeringBehaviors.Seek(transform.position, velocity, target.transform.position, moveSpeed);
            steer += SteeringBehaviors.ObstacleAvoidance(transform.position, velocity, moveSpeed, avoidCastDistance, avoidProbeRadius, obstacleLayer);
            velocity = SteeringBehaviors.Integrate(velocity, steer, maxForce, moveSpeed, Time.deltaTime);
        }

        private void TickRecover()
        {
            SetColor(Color.yellow);
            recoverTimer -= Time.deltaTime;
            if (recoverTimer <= 0f)
            {
                target = null;
                state = BeastState.Patrol;
            }
        }

        private void MoveAlongWaypoints()
        {
            if (waypoints == null || waypoints.Length == 0) { velocity = Vector3.zero; return; }

            Transform wp = waypoints[waypointIndex];
            Vector3 steer = SteeringBehaviors.Arrive(transform.position, velocity, wp.position, moveSpeed, 1.5f);
            steer += SteeringBehaviors.ObstacleAvoidance(transform.position, velocity, moveSpeed, avoidCastDistance, avoidProbeRadius, obstacleLayer);
            velocity = SteeringBehaviors.Integrate(velocity, steer, maxForce, moveSpeed, Time.deltaTime);

            if (Vector3.Distance(transform.position, wp.position) <= waypointArriveDistance)
                AdvanceWaypoint();
        }

        private void AdvanceWaypoint()
        {
            waypointIndex += waypointDirection;
            if (waypointIndex >= waypoints.Length)
            {
                waypointIndex = waypoints.Length - 1;
                waypointDirection = -1;
            }
            else if (waypointIndex < 0)
            {
                waypointIndex = 0;
                waypointDirection = 1;
            }
        }

        private GladiatorNPC FindNearbyGladiator()
        {
            GladiatorNPC nearest = null;
            float nearestSqrDist = detectionRange * detectionRange;

            foreach (var gladiator in FindObjectsByType<GladiatorNPC>(FindObjectsSortMode.None))
            {
                if (gladiator.IsDead) continue;

                float sqrDist = (gladiator.transform.position - transform.position).sqrMagnitude;
                if (sqrDist <= nearestSqrDist)
                {
                    nearestSqrDist = sqrDist;
                    nearest = gladiator;
                }
            }
            return nearest;
        }

        private void SetColor(Color c)
        {
            if (bodyRenderer != null)
                bodyRenderer.material.color = c;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            if (waypoints == null) return;
            Gizmos.color = Color.blue;
            foreach (var wp in waypoints)
                if (wp != null) Gizmos.DrawSphere(wp.position, 0.2f);
        }
    }
}
