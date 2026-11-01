namespace GladiusAI
{
    // La moneda del juego: el denario romano. Cada cambio se guarda y se avisa por EventManager.
    public static class Denarii
    {
        public static int Balance => SaveService.Data.denarii;

        public static void Add(int amount)
        {
            if (amount <= 0) return;
            Change(amount);
        }

        public static bool TrySpend(int cost)
        {
            if (cost < 0 || Balance < cost) return false;
            Change(-cost);
            return true;
        }

        private static void Change(int delta)
        {
            SaveService.Data.denarii += delta;
            SaveService.Save();
            EventManager.Raise(new DenariiChangedEvent(Balance));
        }
    }
}
