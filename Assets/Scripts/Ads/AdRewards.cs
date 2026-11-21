namespace GladiusAI
{
    // Que anuncio se mostro y como termino.
    public enum AdSlot { Energy, Coins }
    public enum AdOutcome { Completed, Skipped, Failed }

    // Un anuncio termino (la tienda y la pantalla de victoria muestran el mensaje).
    public readonly struct AdFinishedEvent
    {
        public readonly AdSlot slot;
        public readonly AdOutcome outcome;
        public readonly string message;

        public AdFinishedEvent(AdSlot slot, AdOutcome outcome, string message)
        {
            this.slot = slot;
            this.outcome = outcome;
            this.message = message;
        }
    }

    // El premio cambia segun se vea el anuncio entero o se lo cierre antes. Devuelve el mensaje para el jugador.
    public static class AdRewards
    {
        public static string Grant(AdSlot slot, AdOutcome outcome, AdsConfig config)
        {
            if (outcome == AdOutcome.Failed)
                return "No se pudo mostrar el anuncio. Probá de nuevo en un rato.";

            bool completed = outcome == AdOutcome.Completed;
            switch (slot)
            {
                case AdSlot.Energy:
                    if (completed)
                    {
                        Energy.Refill();
                        return "¡Viste el anuncio entero! Energía llena.";
                    }
                    Energy.Add(config.energyIfSkipped);
                    return $"Cerraste el anuncio antes de tiempo: solo +{config.energyIfSkipped} de energía.";

                default:
                    int coins = completed ? config.coinsIfCompleted : config.coinsIfSkipped;
                    Denarii.Add(coins);
                    return completed
                        ? $"¡Viste el anuncio entero! {DenariiText.Amount(coins, showPlus: true)}"
                        : $"Cerraste el anuncio antes de tiempo: {DenariiText.Amount(coins, showPlus: true)}";
            }
        }
    }
}
