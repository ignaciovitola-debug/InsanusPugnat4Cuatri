using System;
using UnityEngine;

namespace GladiusAI
{
    // Energia para entrar a los combates: vuelve de a un punto con el tiempo, aunque el juego este cerrado.
    // Originador del Memento: arma y restaura su propia foto.
    public static class Energy
    {
        private static int current;
        private static long stampTicks;

        private static EconomyConfig Config => EconomyConfig.Instance;
        private static long IntervalTicks => TimeSpan.FromMinutes(Mathf.Max(0.01f, Config.minutesPerEnergy)).Ticks;

        public static int Max => Config.maxEnergy;

        public static int Current
        {
            get
            {
                Regenerate();
                return current;
            }
        }

        public static float SecondsToNext
        {
            get
            {
                Regenerate();
                if (current >= Max) return 0f;

                long remaining = IntervalTicks - (DateTime.UtcNow.Ticks - stampTicks);
                return Mathf.Max(0f, (float)remaining / TimeSpan.TicksPerSecond);
            }
        }

        public static string TimeToNextText()
        {
            var time = TimeSpan.FromSeconds(Mathf.CeilToInt(SecondsToNext));
            return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
        }

        public static bool TrySpend(int amount)
        {
            Regenerate();
            if (amount <= 0) return true;
            if (current < amount) return false;

            // Al bajar del maximo empieza a correr la cuenta del proximo punto.
            if (current >= Max)
                stampTicks = DateTime.UtcNow.Ticks;

            current -= amount;
            Notify();
            return true;
        }

        public static void Refill()
        {
            current = Max;
            stampTicks = DateTime.UtcNow.Ticks;
            Notify();
        }

        public static EnergyMemento CreateMemento() => new EnergyMemento { energy = current, stampTicks = stampTicks };

        public static void RestoreMemento(EnergyMemento memento)
        {
            current = Mathf.Clamp(memento.energy, 0, Max);
            stampTicks = memento.stampTicks;
        }

        public static void ResetToNewGame()
        {
            current = Max;
            stampTicks = DateTime.UtcNow.Ticks;
        }

        // Suma los puntos que se ganaron con el tiempo que paso.
        private static void Regenerate()
        {
            long now = DateTime.UtcNow.Ticks;

            if (current >= Max)
            {
                current = Max;
                stampTicks = now;
                return;
            }

            long elapsed = now - stampTicks;
            if (elapsed < 0)
            {
                // El reloj se atraso: se reinicia la cuenta en vez de dar tiempo negativo.
                stampTicks = now;
                return;
            }

            long gained = elapsed / IntervalTicks;
            if (gained <= 0) return;

            current = (int)Math.Min(Max, current + gained);
            stampTicks = current >= Max ? now : stampTicks + gained * IntervalTicks;
        }

        private static void Notify()
        {
            EventManager.Raise(new EnergyChangedEvent(current));
            EventManager.Raise(new SaveRequestedEvent());
        }
    }
}
