using System.Collections.Generic;
using UnityEngine;

namespace GladiusAI
{
    /// Roster de hasta 8 gladiadores reclutables y cual se eligio para el proximo combate.
    /// Estatica para sobrevivir el cambio de escena Ludus -> ArenaDeCombate2, igual que EventManager.
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

        // La lista vive dentro del SaveData: lo que se cambia aca se guarda con SaveService.Save().
        public static List<GladiatorRosterEntry> Slots
        {
            get
            {
                if (slots == null)
                {
                    slots = SaveService.Data.roster;
                    if (slots.Count != SlotCount)
                    {
                        slots.Clear();
                        slots.AddRange(BuildDefaultRoster());
                        SaveService.Save();
                    }
                    AssignSkinsOnce();
                    GiftStarterOnce();
                }
                return slots;
            }
        }

        public static int SelectedIndex { get; set; } = 0;

        // Version del tutorial de la Ludus que ya se vio (queda guardada). Quien lo vio antes de que existieran las
        // versiones tiene la 1, asi cuando se amplia solo ve lo nuevo.
        public static int LudusTutorialVersion
        {
            get
            {
                var data = SaveService.Data;
                return Mathf.Max(data.ludusTutorialVersion, data.ludusTutorialSeen ? 1 : 0);
            }
            set
            {
                SaveService.Data.ludusTutorialVersion = value;
                SaveService.Save();
            }
        }

        public static bool HasLockedSlot
        {
            get
            {
                for (int i = 1; i < Slots.Count; i++)
                    if (!Slots[i].unlocked) return true;
                return false;
            }
        }

        // Despues de borrar los datos guardados: se vuelve a leer todo desde cero.
        public static void Reset()
        {
            slots = null;
            SelectedIndex = 0;
        }

        public static GladiatorRosterEntry GetSelected()
        {
            int index = SelectedIndex >= 0 && SelectedIndex < Slots.Count ? SelectedIndex : 0;
            return Slots[index];
        }

        /// Tu gladiador cayo en combate: ese slot vuelve a stats basicas.
        public static void ResetSlotToBasic(int index)
        {
            if (index < 0 || index >= Slots.Count) return;
            var basic = BasicPreset();
            basic.label = Slots[index].label;
            basic.skin = Slots[index].skin;
            Slots[index] = basic;
            SaveService.Save();
            EventManager.Raise(new RosterChangedEvent());
        }

        public static bool IsUnlocked(int index)
            => index >= 0 && index < Slots.Count && Slots[index].unlocked;

        /// Se gano el combate que otorga reclutas: desbloquea el proximo gladiador bloqueado y le sortea stats.
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
                SaveService.Save();
                EventManager.Raise(new RosterChangedEvent());
                return;
            }
        }

        // El Novato (puesto 1) se regala una sola vez: asi, aunque el gladiador principal este entrenando,
        // siempre hay alguien disponible para pelear.
        private static void GiftStarterOnce()
        {
            var data = SaveService.Data;
            if (data.starterGift >= 1) return;

            var novato = slots[1];
            if (!novato.unlocked)
            {
                novato.unlocked = true;
                novato.maxHP = 90f;
                novato.minDamage = 9f;
                novato.maxDamage = 17f;
                novato.attackCooldown = 1.3f;
                slots[1] = novato;
            }
            data.starterGift = 1;
            SaveService.Save();
        }

        // Cada puesto del roster tiene su aspecto (Gladiator 1-3, Amazon 1-3) y lo conserva siempre.
        // Se reparten en ronda una sola vez; las partidas guardadas antes de que existieran los aspectos
        // (todos con el 0) tambien pasan por aca.
        private static void AssignSkinsOnce()
        {
            var data = SaveService.Data;
            int skinCount = GladiatorSkins.Count;
            if (data.skinsVersion >= 1 || skinCount == 0) return;

            for (int i = 0; i < slots.Count; i++)
            {
                var entry = slots[i];
                entry.skin = i % skinCount;
                slots[i] = entry;
            }
            data.skinsVersion = 1;
            SaveService.Save();
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
