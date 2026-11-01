using System;
using UnityEngine;

namespace GladiusAI
{
    // Energia para entrar a los combates. Vuelve de a un punto cada cierto tiempo, aunque el juego este cerrado:
    // no se guarda "cuanto falta", se guarda cuanta energia habia y desde cuando corre la cuenta, y al
    // preguntar se calcula cuantos puntos se ganaron con la hora actual.
    public static class Energy
    {
        private static EconomyConfig Config => EconomyConfig.Instance;
        private static long IntervalTicks => TimeSpan.FromMinutes(Mathf.Max(0.01f, Config.minutesPerEnergy)).Ticks;

        public static int Max => Config.maxEnergy;

        public static int Current
        {
            get
            {
                Regenerate();
                return SaveService.Data.energy;
            }
        }

        public static float SecondsToNext
        {
            get
            {
                Regenerate();
                var d = SaveService.Data;
                if (d.energy >= Max) return 0f;

                long remaining = IntervalTicks - (DateTime.UtcNow.Ticks - d.energyStampTicks);
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
            var d = SaveService.Data;
            if (amount <= 0) return true;
            if (d.energy < amount) return false;

            // Al salir del tope empieza a correr la cuenta para el proximo punto.
            if (d.energy >= Max)
                d.energyStampTicks = DateTime.UtcNow.Ticks;

            d.energy -= amount;
            SaveAndNotify();
            return true;
        }

        public static void Refill()
        {
            var d = SaveService.Data;
            d.energy = Max;
            d.energyStampTicks = DateTime.UtcNow.Ticks;
            SaveAndNotify();
        }

        private static void Regenerate()
        {
            var d = SaveService.Data;
            long now = DateTime.UtcNow.Ticks;

            if (d.energy >= Max)
            {
                d.energy = Max;
                d.energyStampTicks = now;
                return;
            }

            long elapsed = now - d.energyStampTicks;
            if (elapsed < 0)
            {
                // El reloj del celular se atraso: se vuelve a empezar la cuenta en vez de dar tiempo negativo.
                d.energyStampTicks = now;
                return;
            }

            long gained = elapsed / IntervalTicks;
            if (gained <= 0) return;

            d.energy = (int)Math.Min(Max, d.energy + gained);
            d.energyStampTicks = d.energy >= Max ? now : d.energyStampTicks + gained * IntervalTicks;
        }

        private static void SaveAndNotify()
        {
            SaveService.Save();
            EventManager.Raise(new EnergyChangedEvent(SaveService.Data.energy));
        }
    }
}
