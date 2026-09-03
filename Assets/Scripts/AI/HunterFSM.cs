namespace GladiusAI
{
    /// <summary>Máquina de Estados Finita mínima: guarda el estado actual y gestiona Enter/Exit al cambiar.</summary>
    public class HunterFSM
    {
        public IHunterState CurrentState { get; private set; }

        public void ChangeState(Hunter hunter, IHunterState newState)
        {
            CurrentState?.Exit(hunter);
            CurrentState = newState;
            CurrentState?.Enter(hunter);
        }

        public void Tick(Hunter hunter, float deltaTime) => CurrentState?.Execute(hunter, deltaTime);
    }
}
