namespace GladiusAI
{
    /// <summary>
    /// Traduce las consignas del jugador en chequeos de dado listos para
    /// usar en el árbol. No sabe nada de movimiento ni combate.
    /// </summary>
    public class GladiatorIntentHandler
    {
        private readonly PlayerIntentController controller;

        public GladiatorIntentHandler(PlayerIntentController controller)
        {
            this.controller = controller;
        }

        public bool HasController => controller != null;

        public bool TryConsume(PlayerIntent intent)
        {
            if (controller == null) return false;
            if (!controller.RollFor(intent)) return false;

            controller.ClearIntent();
            return true;
        }
    }
}