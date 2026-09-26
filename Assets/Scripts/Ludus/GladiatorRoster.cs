using System.Collections.Generic;
using UnityEngine;

namespace GladiusAI
{
    /// <summary>
    /// Roster de hasta 8 gladiadores reclutables y cual se eligio para el proximo combate.
    /// Estatica para sobrevivir el cambio de escena Ludus -> ArenaDeCombate2, igual que EventManager.
    /// </summary>
    public static class GladiatorRoster
    {
        public const int SlotCount = 8;

        private const float MinHP = 70f;
        private const float MaxHP = 160f;
        private const float MinDamageLow = 8f;
        private const float MinDamageHigh = 16f;
        private const float MaxDamageBonus = 4f;
        private const float MaxDamageHigh = 30f;
        private const float MinCooldown = 0.8f;
        private const float MaxCooldown = 1.5f;

        private static List<GladiatorRosterEntry> slots;

        public static List<GladiatorRosterEntry> Slots
        {
            get
            {
                if (slots == null)
                    slots = BuildDefaultRoster();
                return slots;
            }
        }

        public static int SelectedIndex { get; set; } = 0;

        public static GladiatorRosterEntry GetSelected()
        {
            int index = SelectedIndex >= 0 && SelectedIndex < Slots.Count ? SelectedIndex : 0;
            return Slots[index];
        }

        /// <summary>Tu gladiador cayo en combate: ese slot vuelve a stats basicas.</summary>
        public static void ResetSlotToBasic(int index)
        {
            if (index < 0 || index >= Slots.Count) return;
            var basic = BasicPreset();
            basic.label = Slots[index].label;
            Slots[index] = basic;
        }

        public static bool IsUnlocked(int index)
            => index >= 0 && index < Slots.Count && Slots[index].unlocked;

        /// <summary>Se gano el combate que otorga reclutas: desbloquea el proximo gladiador bloqueado y le sortea stats.</summary>
        public static void UnlockNext()
        {
            for (int i = 1; i < Slots.Count; i++)
            {
                if (Slots[i].unlocked) continue;

                var entry = Slots[i];
                entry.unlocked = true;
                entry.maxHP = Random.Range(MinHP, MaxHP);
                entry.minDamage = Random.Range(MinDamageLow, MinDamageHigh);
                entry.maxDamage = Random.Range(entry.minDamage + MaxDamageBonus, MaxDamageHigh);
                entry.attackCooldown = Random.Range(MinCooldown, MaxCooldown);
                Slots[i] = entry;
                return;
            }
        }

        private static GladiatorRosterEntry BasicPreset()
            => new GladiatorRosterEntry("Tu Gladiador", 100f, 10f, 20f, 1.2f);

        private static List<GladiatorRosterEntry> BuildDefaultRoster()
        {
            return new List<GladiatorRosterEntry>
            {
                BasicPreset(),
                new GladiatorRosterEntry("Novato", 0f, 0f, 0f, 0f, unlocked: false),
                new GladiatorRosterEntry("Veterano", 0f, 0f, 0f, 0f, unlocked: false),
                new GladiatorRosterEntry("Agresivo", 0f, 0f, 0f, 0f, unlocked: false),
                new GladiatorRosterEntry("Defensivo", 0f, 0f, 0f, 0f, unlocked: false),
                new GladiatorRosterEntry("Berserker", 0f, 0f, 0f, 0f, unlocked: false),
                new GladiatorRosterEntry("Resistente", 0f, 0f, 0f, 0f, unlocked: false),
                new GladiatorRosterEntry("Equilibrado", 0f, 0f, 0f, 0f, unlocked: false),
            };
        }
    }
}
