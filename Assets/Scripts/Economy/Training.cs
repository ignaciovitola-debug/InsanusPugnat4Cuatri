using System;
using UnityEngine;

namespace GladiusAI
{
    // Entrenamiento de gladiadores. Mismo patron que Energy: se guarda la hora de fin y el reloj hace el resto,
    // asi sigue corriendo aunque el juego este cerrado. Un solo gladiador entrena a la vez y mientras tanto no puede pelear.
    public static class Training
    {
        private static SaveData Data => SaveService.Data;
        private static EconomyConfig Config => EconomyConfig.Instance;

        public static bool IsActive => Data.trainingSlot >= 0;
        public static int Slot => Data.trainingSlot;

        public static float SecondsRemaining
        {
            get
            {
                if (!IsActive) return 0f;

                float remaining = (float)(Data.trainingEndTicks - DateTime.UtcNow.Ticks) / TimeSpan.TicksPerSecond;
                // Si el reloj se atraso, nunca puede faltar mas que la duracion elegida.
                float max = Config.trainingTiers[Mathf.Clamp(Data.trainingTier, 0, Config.trainingTiers.Length - 1)].minutes * 60f;
                return Mathf.Clamp(remaining, 0f, max);
            }
        }

        public static string TimeText()
        {
            var time = TimeSpan.FromSeconds(Mathf.CeilToInt(SecondsRemaining));
            return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
        }

        public static int RushCost => Mathf.CeilToInt(SecondsRemaining / 60f) * Config.rushCostPerMinute;

        public static bool TryStart(int slot, int tierIndex, out string reason)
        {
            reason = null;
            if (IsActive) reason = "Ya hay un gladiador entrenando";
            else if (!GladiatorRoster.IsUnlocked(slot)) reason = "Ese gladiador todavía no es tuyo";
            else if (tierIndex < 0 || tierIndex >= Config.trainingTiers.Length) reason = "Duración inválida";
            if (reason != null) return false;

            Data.trainingSlot = slot;
            Data.trainingTier = tierIndex;
            Data.trainingEndTicks = DateTime.UtcNow.AddMinutes(Config.trainingTiers[tierIndex].minutes).Ticks;
            SaveService.Save();
            EventManager.Raise(new RosterChangedEvent());
            return true;
        }

        // Si ya paso el tiempo, aplica la mejora. Devuelve true solo en el momento en que termina.
        public static bool TryComplete()
        {
            if (!IsActive || SecondsRemaining > 0f) return false;

            var tier = Config.trainingTiers[Mathf.Clamp(Data.trainingTier, 0, Config.trainingTiers.Length - 1)];
            int slot = Data.trainingSlot;
            Data.trainingSlot = -1;

            var entry = GladiatorRoster.Slots[slot];
            entry.maxHP += tier.bonusHP;
            entry.minDamage += tier.bonusDamage;
            entry.maxDamage += tier.bonusDamage;
            entry.level++;
            GladiatorRoster.Slots[slot] = entry;

            SaveService.Save();
            EventManager.Raise(new RosterChangedEvent());
            return true;
        }

        public static bool TryRush(out string reason)
        {
            reason = null;
            if (!IsActive) { reason = "No hay ningún entrenamiento en curso"; return false; }
            if (!Denarii.TrySpend(RushCost)) { reason = $"Te faltan {RushCost - Denarii.Balance} denarios"; return false; }

            Data.trainingEndTicks = DateTime.UtcNow.Ticks;
            return TryComplete();
        }
    }
}
