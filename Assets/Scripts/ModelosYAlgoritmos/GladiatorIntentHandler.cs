namespace GladiusAI
{
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