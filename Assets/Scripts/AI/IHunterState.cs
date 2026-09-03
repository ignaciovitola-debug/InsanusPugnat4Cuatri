namespace GladiusAI
{
    /// <summary>
    /// Contrato de un estado de la FSM del Cazador. Cada estado decide, dentro
    /// de Execute, cuándo pedirle a la FSM que cambie de estado — la transición
    /// la dispara el estado mismo, no un controlador externo (tal como pide la consigna).
    /// </summary>
    public interface IHunterState
    {
        void Enter(Hunter hunter);
        void Execute(Hunter hunter, float deltaTime);
        void Exit(Hunter hunter);
    }
}
