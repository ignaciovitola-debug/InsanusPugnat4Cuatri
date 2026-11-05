namespace GladiusAI
{
    // La moneda del juego (el denario romano). Originador del Memento: arma y restaura su propia foto.
    public static class Denarii
    {
        private static int balance;

        public static int Balance => balance;

        public static void Add(int amount)
        {
            if (amount <= 0) return;
            Change(amount);
        }

        public static bool TrySpend(int cost)
        {
            if (cost < 0 || balance < cost) return false;
            Change(-cost);
            return true;
        }

        public static WalletMemento CreateMemento() => new WalletMemento { denarii = balance };

        public static void RestoreMemento(WalletMemento memento) => balance = memento.denarii;

        public static void ResetToNewGame() => balance = 0;

        // Avisa a la pantalla y pide guardar.
        private static void Change(int delta)
        {
            balance += delta;
            EventManager.Raise(new DenariiChangedEvent(balance));
            EventManager.Raise(new SaveRequestedEvent());
        }
    }
}
