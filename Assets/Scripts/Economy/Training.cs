using System;
using UnityEngine;

namespace GladiusAI
{
    // Entrenamiento de gladiadores. Guarda la hora de fin y el reloj hace el resto, aunque el juego este cerrado.
    // Entrena uno solo a la vez. Originador del Memento: arma y restaura su propia foto.
    public static class Training
    {
        private static int slot = -1;
        private static long endTicks;
        private static int tier;

        private static EconomyConfig Config => EconomyConfig.Instance;

        public static bool IsActive => slot >= 0;
        public static int Slot => slot;

        public static float SecondsRemaining
        {
            get
            {
                if (!IsActive) return 0f;

                float remaining = (float)(endTicks - DateTime.UtcNow.Ticks) / TimeSpan.TicksPerSecond;
                // Si el reloj se atraso, nunca puede faltar mas que la duracion elegida.
                float max = CurrentTier.minutes * 60f;
                return Mathf.Clamp(remaining, 0f, max);
            }
        }

        public static string TimeText()
        {
            var time = TimeSpan.FromSeconds(Mathf.CeilToInt(SecondsRemaining));
            return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
        }

        public static int RushCost => Mathf.CeilToInt(SecondsRemaining / 60f) * Config.rushCostPerMinute;

        private static EconomyConfig.TrainingTier CurrentTier
            => Config.trainingTiers[Mathf.Clamp(tier, 0, Config.trainingTiers.Length - 1)];

        public static bool TryStart(int slotIndex, int tierIndex, out string reason)
        {
            reason = null;
            if (IsActive) reason = "Ya hay un gladiador entrenando";
            else if (!GladiatorRoster.IsUnlocked(slotIndex)) reason = "Ese gladiador todavía no es tuyo";
            else if (tierIndex < 0 || tierIndex >= Config.trainingTiers.Length) reason = "Duración inválida";
            if (reason != null) return false;

            slot = slotIndex;
            tier = tierIndex;
            endTicks = DateTime.UtcNow.AddMinutes(Config.trainingTiers[tierIndex].minutes).Ticks;
            Notify();
            return true;
        }

        // Si ya paso el tiempo, aplica la mejora. Devuelve true solo en el momento en que termina.
        public static bool TryComplete()
        {
            if (!IsActive || SecondsRemaining > 0f) return false;

            var finishedTier = CurrentTier;
            int finishedSlot = slot;
            slot = -1;

            GladiatorRoster.AddTrainingBonus(finishedSlot, finishedTier.bonusHP, finishedTier.bonusDamage);
            Notify();
            return true;
        }

        public static bool TryRush(out string reason)
        {
            reason = null;
            if (!IsActive) { reason = "No hay ningún entrenamiento en curso"; return false; }
            if (!Denarii.TrySpend(RushCost)) { reason = $"Te faltan {RushCost - Denarii.Balance} denarios"; return false; }

            endTicks = DateTime.UtcNow.Ticks;
            return TryComplete();
        }

        public static TrainingMemento CreateMemento()
            => new TrainingMemento { slot = slot, endTicks = endTicks, tier = tier };

        public static void RestoreMemento(TrainingMemento memento)
        {
            slot = memento.slot;
            endTicks = memento.endTicks;
            tier = memento.tier;
        }

        public static void ResetToNewGame()
        {
            slot = -1;
            endTicks = 0;
            tier = 0;
        }

        private static void Notify()
        {
            EventManager.Raise(new RosterChangedEvent());
            EventManager.Raise(new SaveRequestedEvent());
        }
    }
}
