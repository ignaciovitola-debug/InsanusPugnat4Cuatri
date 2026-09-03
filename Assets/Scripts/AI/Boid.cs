using System.Collections.Generic;
using UnityEngine;

namespace GladiusAI
{
    /// <summary>
    /// Gladiador-Boid: agente autónomo que aplica Flocking dentro de su propio
    /// grupo (separación/alineación/cohesión) y usa un Árbol de Decisión
    /// (reutilizando el motor de Core/) para elegir, cada frame, entre buscar
    /// comida, huir del cazador, aplicar Flocking o vagar solo.
    /// </summary>
    public class Boid : MonoBehaviour
    {
        public static readonly List<Boid> All = new List<Boid>();

        [Header("Grupo (0-3): boids del mismo grupo flockean juntos")]
        [SerializeField] private int groupId = 0;
        [SerializeField] private Renderer bodyRenderer;

        [Header("Movimiento")]
        [SerializeField] private float maxSpeed = 3.5f;
        [SerializeField] private float maxForce = 9f;
        [SerializeField] private float turnSpeed = 480f;

        [Header("Flocking (dentro del mismo grupo)")]
        [SerializeField] private float separationRadius = 1.2f;
        [SerializeField] private float neighborRadius = 4f;
        [SerializeField] private float separationWeight = 1.6f;
        [SerializeField] private float alignmentWeight = 1f;
        [SerializeField] private float cohesionWeight = 1f;

        [Header("Comida (Arrive)")]
        [SerializeField] private float foodDetectionRange = 8f;
        [SerializeField] private float eatDistance = 1.0f;
        [SerializeField] private float arriveSlowRadius = 1.5f;

        [Header("Cazador (Evade)")]
        [SerializeField] private float hunterVisionRange = 6f;

        [Header("Vagar (sin comida, sin cazador, sin grupo cerca)")]
        [SerializeField] private float wanderRetargetInterval = 2.5f;

        public int GroupId { get => groupId; set => groupId = value; }
        public Vector3 Position => transform.position;
        public Vector3 Velocity { get; private set; }

        /// <summary>El cazador lo atrapó: reaparece en otro punto de la arena (evita que el león lo atraviese sin más).</summary>
        public void GetCaught()
        {
            transform.position = ArenaBounds.Instance != null ? ArenaBounds.Instance.RandomPointInside() : Position;
            Velocity = Vector3.zero;
        }

        private Blackboard blackboard;
        private Node decisionTree;
        private Food targetFood;
        private Hunter visibleHunter;
        private Vector3 wanderTarget;
        private float wanderTimer;

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        private void Awake()
        {
            blackboard = new Blackboard();
            decisionTree = BuildDecisionTree();
            wanderTarget = transform.position;

            if (bodyRenderer == null)
                bodyRenderer = GetComponentInChildren<Renderer>();

            DirectionIndicator.Attach(transform, Color.black);
        }

        private void Update()
        {
            decisionTree.Tick(blackboard);
            ApplyGlobalSeparation();
            ApplyContainment();
            Integrate();
        }

        /// <summary>
        /// ¿Hay comida cerca? -> Arrive
        /// si no, ¿hay cazador en rango? -> Evade
        /// si no, ¿hay boids del grupo cerca? -> Flocking
        /// si está solo -> vagar
        /// </summary>
        private Node BuildDecisionTree()
        {
            return new Selector("Decisión del Boid",
                new QuestionNode("¿Hay comida cerca?", HasNearbyFood,
                    onTrue: new ActionNode("Ir a comer (Arrive)", ActionSeekFood)),
                new QuestionNode("¿Cazador en rango de visión?", HasVisibleHunter,
                    onTrue: new ActionNode("Huir (Evade)", ActionEvadeHunter)),
                new QuestionNode("¿Hay boids del grupo cerca?", HasFlockmatesNearby,
                    onTrue: new ActionNode("Flocking", ActionFlock)),
                new ActionNode("Vagar solo", ActionWander)
            );
        }

        // ==================== Condiciones ====================
        private bool HasNearbyFood(Blackboard bb)
        {
            if (FoodManager.Instance == null) return false;
            targetFood = FoodManager.Instance.GetNearestFood(Position, foodDetectionRange);
            return targetFood != null;
        }

        private bool HasVisibleHunter(Blackboard bb)
        {
            visibleHunter = FindNearestHunter();
            return visibleHunter != null;
        }

        private bool HasFlockmatesNearby(Blackboard bb) => GetFlockmates().Count > 0;

        // ==================== Acciones ====================
        private NodeState ActionSeekFood(Blackboard bb)
        {
            SetColor(Color.green);
            if (targetFood == null) return NodeState.Failure;

            if (Vector3.Distance(Position, targetFood.Position) <= eatDistance)
            {
                targetFood.Consume();
                return NodeState.Success;
            }

            ApplySteer(SteeringBehaviors.Arrive(Position, Velocity, targetFood.Position, maxSpeed, arriveSlowRadius));
            return NodeState.Running;
        }

        private NodeState ActionEvadeHunter(Blackboard bb)
        {
            SetColor(Color.red);
            if (visibleHunter == null) return NodeState.Failure;

            ApplySteer(SteeringBehaviors.Evade(Position, Velocity, visibleHunter.Position, visibleHunter.Velocity, maxSpeed));
            return NodeState.Running;
        }

        private NodeState ActionFlock(Blackboard bb)
        {
            SetColor(Color.cyan);
            var flockmates = GetFlockmates();

            Vector3 separation = FlockingBehavior.Separation(Position, flockmates, separationRadius) * separationWeight;
            Vector3 alignment = FlockingBehavior.Alignment(flockmates) * alignmentWeight;
            Vector3 cohesion = FlockingBehavior.Cohesion(Position, flockmates).normalized * maxSpeed * cohesionWeight;

            Vector3 flockSteer = separation + alignment + cohesion;

            if (flockSteer.sqrMagnitude < 0.01f && Velocity.sqrMagnitude < 0.25f)
            {
                Vector3 jitter = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
                flockSteer += jitter * maxSpeed * 0.5f;
            }

            ApplySteer(flockSteer);
            return NodeState.Running;
        }

        private NodeState ActionWander(Blackboard bb)
        {
            SetColor(Color.white);

            wanderTimer -= Time.deltaTime;
            bool reachedTarget = Vector3.Distance(Position, wanderTarget) < 0.5f;
            if (wanderTimer <= 0f || reachedTarget)
            {
                wanderTarget = ArenaBounds.Instance != null
                    ? ArenaBounds.Instance.RandomPointInside()
                    : Position + Random.insideUnitSphere * 5f;
                wanderTimer = wanderRetargetInterval;
            }

            ApplySteer(SteeringBehaviors.Arrive(Position, Velocity, wanderTarget, maxSpeed * 0.5f, arriveSlowRadius));
            return NodeState.Running;
        }

        // ==================== Auxiliares ====================
        private List<Boid> GetFlockmates()
        {
            var result = new List<Boid>();
            for (int i = 0; i < All.Count; i++)
            {
                Boid other = All[i];
                if (other == this || other.groupId != groupId) continue;
                if (Vector3.Distance(Position, other.Position) <= neighborRadius)
                    result.Add(other);
            }
            return result;
        }

        private Hunter FindNearestHunter()
        {
            Hunter nearest = null;
            float nearestSqrDist = hunterVisionRange * hunterVisionRange;

            for (int i = 0; i < Hunter.All.Count; i++)
            {
                Hunter hunter = Hunter.All[i];
                float sqrDist = (hunter.Position - Position).sqrMagnitude;
                if (sqrDist <= nearestSqrDist)
                {
                    nearestSqrDist = sqrDist;
                    nearest = hunter;
                }
            }
            return nearest;
        }

        private void ApplyGlobalSeparation()
        {
            Vector3 steer = Vector3.zero;
            int count = 0;

            for (int i = 0; i < All.Count; i++)
            {
                Boid other = All[i];
                if (other == this) continue;
                Vector3 offset = Position - other.Position;
                float distance = offset.magnitude;
                if (distance > 0.0001f && distance < separationRadius)
                {
                    steer += offset.normalized / distance;
                    count++;
                }
            }

            if (count > 0)
                ApplySteer(steer / count * separationWeight);
        }

        private void ApplyContainment()
        {
            if (ArenaBounds.Instance == null) return;
            Vector3 containment = ArenaBounds.Instance.GetContainmentForce(Position, Velocity, maxSpeed);
            if (containment != Vector3.zero)
                ApplySteer(containment);
        }

        private void ApplySteer(Vector3 steer)
        {
            steer.y = 0f;
            Velocity = SteeringBehaviors.Integrate(Velocity, steer, maxForce, maxSpeed, Time.deltaTime);
            Velocity = new Vector3(Velocity.x, 0f, Velocity.z);
        }

        private void Integrate()
        {
            Vector3 newPosition = transform.position + Velocity * Time.deltaTime;
            newPosition.y = transform.position.y;
            if (ArenaBounds.Instance != null)
                newPosition = ArenaBounds.Instance.ClampPosition(newPosition);

            transform.position = newPosition;
            SteeringBehaviors.FaceDirection(transform, Velocity, turnSpeed, Time.deltaTime);
        }

        private void SetColor(Color c)
        {
            if (bodyRenderer != null)
                bodyRenderer.material.color = c;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, foodDetectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, hunterVisionRange);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, neighborRadius);
        }
    }
}
