using System.Collections.Generic;
using UnityEngine;

namespace GladiusAI
{
    // Los 8 gladiadores reclutables y cual se eligio para el proximo combate.
    // Es estatica para sobrevivir al cambio de escena. Originador del Memento: arma y restaura su propia foto.
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

        private static List<GladiatorRosterEntry> slots = BuildDefaultRoster();
        private static int tutorialVersion;
        private static bool arenaTutorialDone;

        // Solo lectura: los cambios pasan por los metodos de abajo, que guardan solos.
        public static IReadOnlyList<GladiatorRosterEntry> Slots => slots;

        public static int SelectedIndex { get; set; } = 0;

        // Version del tutorial de la Ludus que ya se vio. Si se amplia, solo se muestra lo nuevo.
        public static int LudusTutorialVersion
        {
            get => tutorialVersion;
            set
            {
                tutorialVersion = value;
                EventManager.Raise(new SaveRequestedEvent());
            }
        }

        // True cuando ya se completo el tutorial de la Arena 1: "Jugar" pasa a ir directo a la Ludus.
        public static bool ArenaTutorialDone
        {
            get => arenaTutorialDone;
            set
            {
                arenaTutorialDone = value;
                EventManager.Raise(new SaveRequestedEvent());
            }
        }

        public static bool HasLockedSlot
        {
            get
            {
                for (int i = 1; i < slots.Count; i++)
                    if (!slots[i].unlocked) return true;
                return false;
            }
        }

        public static GladiatorRosterEntry GetSelected()
        {
            int index = SelectedIndex >= 0 && SelectedIndex < slots.Count ? SelectedIndex : 0;
            return slots[index];
        }

        public static bool IsUnlocked(int index)
            => index >= 0 && index < slots.Count && slots[index].unlocked;

        // Tu gladiador cayo en combate: ese puesto vuelve a stats basicas.
        public static void ResetSlotToBasic(int index)
        {
            if (index < 0 || index >= slots.Count) return;

            var basic = BasicPreset();
            basic.label = slots[index].label;
            basic.skin = slots[index].skin;
            slots[index] = basic;
            Notify();
        }

        // Se gano el combate que da reclutas: desbloquea el proximo gladiador y le sortea stats.
        public static void UnlockNext()
        {
            for (int i = 1; i < slots.Count; i++)
            {
                if (slots[i].unlocked) continue;

                var entry = slots[i];
                entry.unlocked = true;
                entry.maxHP = Random.Range(MinHP, MaxHP);
                entry.minDamage = Random.Range(MinDamageLow, MinDamageHigh);
                entry.maxDamage = Random.Range(entry.minDamage + MaxDamageBonus, MaxDamageHigh);
                entry.attackCooldown = Random.Range(MinCooldown, MaxCooldown);
                slots[i] = entry;
                Notify();
                return;
            }
        }

        // Termino un entrenamiento: el gladiador sube de nivel. Cuanto mas nivel, menos mejora (tabla).
        public static void AddTrainingBonus(int index, float bonusHP, float bonusDamage)
        {
            if (index < 0 || index >= slots.Count) return;

            var entry = slots[index];
            float scale = ProgressionTables.TrainingBonusForLevel(entry.level);
            entry.maxHP += bonusHP * scale;
            entry.minDamage += bonusDamage * scale;
            entry.maxDamage += bonusDamage * scale;
            entry.level++;
            slots[index] = entry;
            Notify();
        }

        public static RosterMemento CreateMemento()
        {
            return new RosterMemento
            {
                slots = new List<GladiatorRosterEntry>(slots),
                tutorialVersion = tutorialVersion,
                arenaTutorialDone = arenaTutorialDone,
            };
        }

        public static void RestoreMemento(RosterMemento memento)
        {
            bool valid = memento.slots != null && memento.slots.Count == SlotCount;
            slots = valid ? new List<GladiatorRosterEntry>(memento.slots) : BuildDefaultRoster();
            tutorialVersion = memento.tutorialVersion;
            arenaTutorialDone = memento.arenaTutorialDone;
            SelectedIndex = 0;
        }

        public static void ResetToNewGame()
        {
            slots = BuildDefaultRoster();
            tutorialVersion = 0;
            arenaTutorialDone = false;
            SelectedIndex = 0;
        }

        private static void Notify()
        {
            EventManager.Raise(new RosterChangedEvent());
            EventManager.Raise(new SaveRequestedEvent());
        }

        private static GladiatorRosterEntry BasicPreset()
            => new GladiatorRosterEntry("Tu Gladiador", 100f, 10f, 20f, 1.2f);

        // Partida nueva: cada puesto con su aspecto, y el Novato ya regalado para tener con quien pelear.
        private static List<GladiatorRosterEntry> BuildDefaultRoster()
        {
            var list = new List<GladiatorRosterEntry>
            {
                BasicPreset(),
                new GladiatorRosterEntry("Novato", 90f, 9f, 17f, 1.3f),
                new GladiatorRosterEntry("Veterano", 0f, 0f, 0f, 0f, unlocked: false),
                new GladiatorRosterEntry("Agresivo", 0f, 0f, 0f, 0f, unlocked: false),
                new GladiatorRosterEntry("Defensivo", 0f, 0f, 0f, 0f, unlocked: false),
                new GladiatorRosterEntry("Berserker", 0f, 0f, 0f, 0f, unlocked: false),
                new GladiatorRosterEntry("Resistente", 0f, 0f, 0f, 0f, unlocked: false),
                new GladiatorRosterEntry("Equilibrado", 0f, 0f, 0f, 0f, unlocked: false),
            };

            int skinCount = GladiatorSkins.Count;
            if (skinCount > 0)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var entry = list[i];
                    entry.skin = i % skinCount;
                    list[i] = entry;
                }
            }
            return list;
        }
    }
}
