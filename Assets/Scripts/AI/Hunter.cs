using System.Collections.Generic;
using UnityEngine;

namespace GladiusAI
{
    /// <summary>
    /// El león: NPC cazador controlado por una FSM (Idle/Rest, Patrol, Hunting).
    /// Puede haber varias instancias en la escena (varios leones) — cada una
    /// con su propia FSM y su propia energía. Movimiento 100% cinemático
    /// (Transform), sin Rigidbody, tal como exige la consigna.
    /// </summary>
    public class Hunter : MonoBehaviour
    {
        public static readonly List<Hunter> All = new List<Hunter>();

        [Header("Identidad")]
        [SerializeField] private Renderer bodyRenderer;

        [Header("Movimiento")]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float maxForce = 12f;
        [SerializeField] private float turnSpeed = 360f;

        [Header("Patrulla (waypoints en orden, ida y vuelta)")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float waypointArriveDistance = 0.5f;

        [Header("Visión (cono, como un depredador acechando)")]
        [SerializeField] private float visionRange = 7f;
        [SerializeField] private float visionAngle = 100f;

        [Header("Caza")]
        [SerializeField] private float catchDistance = 0.6f;

        [Header("Energía")]
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float energyDrainPatrol = 4f;   // por segundo
        [SerializeField] private float energyDrainHunting = 12f; // por segundo
        [SerializeField] private float energyRegenPerSecond = 25f;
        [SerializeField] private float restDuration = 4f;

        public Vector3 Position => transform.position;
        public Vector3 Velocity { get; private set; }
        public HunterFSM FSM { get; private set; }
        public Boid CurrentTarget { get; set; }

        public float RestDuration => restDuration;
        public float EnergyDrainPatrol => energyDrainPatrol;
        public float EnergyDrainHunting => energyDrainHunting;
        public float EnergyRatio => currentEnergy / maxEnergy;
        public float CatchDistance => catchDistance;
        public string StateName => FSM.CurrentState?.GetType().Name ?? "-";

        public IHunterState IdleState { get; private set; }
        public IHunterState PatrolState { get; private set; }
        public IHunterState HuntingState { get; private set; }

        private float currentEnergy;
        private int waypointIndex;
        private int waypointDirection = 1;

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        private void Awake()
        {
            currentEnergy = maxEnergy;

            IdleState = new HunterIdleState();
            PatrolState = new HunterPatrolState();
            HuntingState = new HunterHuntingState();

            FSM = new HunterFSM();
            FSM.ChangeState(this, PatrolState);

            if (bodyRenderer == null)
                bodyRenderer = GetComponentInChildren<Renderer>();

            DirectionIndicator.Attach(transform, Color.black, heightOffset: 1f, scale: 1.4f);
        }

        private void Update()
        {
            FSM.Tick(this, Time.deltaTime);

            Vector3 newPosition = transform.position + Velocity * Time.deltaTime;
            if (ArenaBounds.Instance != null)
                newPosition = ArenaBounds.Instance.ClampPosition(newPosition);

            transform.position = newPosition;
            SteeringBehaviors.FaceDirection(transform, Velocity, turnSpeed, Time.deltaTime);
        }

        // ==================== Usado por los estados de la FSM ====================
        public void RegenerateEnergy(float deltaTime) =>
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + energyRegenPerSecond * deltaTime);

        /// <summary>Descuenta energía; devuelve true si se agotó (el estado que llama debe pasar a Idle).</summary>
        public bool TryDrainEnergy(float amount)
        {
            currentEnergy = Mathf.Max(0f, currentEnergy - amount);
            return currentEnergy <= 0f;
        }

        public void Stop() => Velocity = Vector3.zero;

        public void MoveAlongPatrol(float deltaTime)
        {
            if (waypoints == null || waypoints.Length == 0) { Stop(); return; }

            Transform wp = waypoints[waypointIndex];
            Vector3 steer = SteeringBehaviors.Arrive(Position, Velocity, wp.position, moveSpeed, 1.5f);
            Velocity = SteeringBehaviors.Integrate(Velocity, steer, maxForce, moveSpeed, deltaTime);

            if (Vector3.Distance(Position, wp.position) <= waypointArriveDistance)
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

        public void Pursue(Boid target, float deltaTime)
        {
            Vector3 steer = SteeringBehaviors.Pursuit(Position, Velocity, target.Position, target.Velocity, moveSpeed);
            Velocity = SteeringBehaviors.Integrate(Velocity, steer, maxForce, moveSpeed, deltaTime);
        }

        public Boid FindVisibleBoid()
        {
            for (int i = 0; i < Boid.All.Count; i++)
            {
                if (CanSee(Boid.All[i])) return Boid.All[i];
            }
            return null;
        }

        public bool CanSee(Boid boid)
        {
            Vector3 toBoid = boid.Position - Position;
            toBoid.y = 0f;
            if (toBoid.magnitude > visionRange) return false;

            float angle = Vector3.Angle(transform.forward, toBoid);
            return angle <= visionAngle * 0.5f;
        }

        public void SetColor(Color c)
        {
            if (bodyRenderer != null)
                bodyRenderer.material.color = c;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, visionRange);

            if (waypoints == null) return;
            Gizmos.color = Color.blue;
            foreach (var wp in waypoints)
                if (wp != null) Gizmos.DrawSphere(wp.position, 0.2f);
        }
    }
}
