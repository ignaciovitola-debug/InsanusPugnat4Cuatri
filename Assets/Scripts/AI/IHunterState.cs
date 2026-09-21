namespace GladiusAI
{
    
    /// Cada estado decide, dentro
    /// de Execute, cuándo pedirle a la FSM que cambie de estado — la transición
    /// la dispara el estado mismo.
   
    public interface IHunterState
    {
        void Enter(Hunter hunter);
        void Execute(Hunter hunter, float deltaTime);
        void Exit(Hunter hunter);
    }
}
