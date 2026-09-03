using UnityEngine;

namespace GladiusAI
{
    /// <summary>
    /// Descanso: el cazador no se mueve y recupera energía. Cuando termina el
    /// tiempo de descanso Y la energía volvió a estar completa, pasa a Patrol.
    /// </summary>
    public class HunterIdleState : IHunterState
    {
        private float restTimer;

        public void Enter(Hunter hunter)
        {
            restTimer = hunter.RestDuration;
            hunter.SetColor(Color.gray);
            hunter.Stop();
        }

        public void Execute(Hunter hunter, float deltaTime)
        {
            restTimer -= deltaTime;
            hunter.RegenerateEnergy(deltaTime);

            if (restTimer <= 0f && hunter.EnergyRatio >= 1f)
                hunter.FSM.ChangeState(hunter, hunter.PatrolState);
        }

        public void Exit(Hunter hunter) { }
    }

    /// <summary>Recorre los waypoints (ida y vuelta). Si detecta un boid, pasa a Hunting.</summary>
    public class HunterPatrolState : IHunterState
    {
        public void Enter(Hunter hunter) => hunter.SetColor(Color.green);

        public void Execute(Hunter hunter, float deltaTime)
        {
            if (hunter.TryDrainEnergy(hunter.EnergyDrainPatrol * deltaTime))
            {
                hunter.FSM.ChangeState(hunter, hunter.IdleState);
                return;
            }

            hunter.MoveAlongPatrol(deltaTime);

            Boid visible = hunter.FindVisibleBoid();
            if (visible != null)
            {
                hunter.CurrentTarget = visible;
                hunter.FSM.ChangeState(hunter, hunter.HuntingState);
            }
        }

        public void Exit(Hunter hunter) { }
    }

    /// <summary>Persigue (Pursuit) al boid detectado prediciendo su posición futura.</summary>
    public class HunterHuntingState : IHunterState
    {
        public void Enter(Hunter hunter) => hunter.SetColor(Color.red);

        public void Execute(Hunter hunter, float deltaTime)
        {
            if (hunter.TryDrainEnergy(hunter.EnergyDrainHunting * deltaTime))
            {
                hunter.FSM.ChangeState(hunter, hunter.IdleState);
                return;
            }

            Boid target = hunter.CurrentTarget;
            if (target == null)
            {
                hunter.FSM.ChangeState(hunter, hunter.PatrolState);
                return;
            }

            if (Vector3.Distance(hunter.Position, target.Position) <= hunter.CatchDistance)
            {
                target.GetCaught();
                hunter.FSM.ChangeState(hunter, hunter.PatrolState);
                return;
            }

            if (!hunter.CanSee(target))
            {
                hunter.FSM.ChangeState(hunter, hunter.PatrolState);
                return;
            }

            hunter.Pursue(target, deltaTime);
        }

        public void Exit(Hunter hunter) => hunter.CurrentTarget = null;
    }
}
