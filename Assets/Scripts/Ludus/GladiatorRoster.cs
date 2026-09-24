using System.Collections.Generic;

namespace GladiusAI
{
    /// <summary>
    /// Roster de hasta 8 gladiadores reclutables y cual se eligio para el proximo combate.
    /// Estatica para sobrevivir el cambio de escena Ludus -> ArenaDeCombate2, igual que EventManager.
    /// </summary>
    public static class GladiatorRoster
    {
        public const int SlotCount = 8;

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

        private static GladiatorRosterEntry BasicPreset()
            => new GladiatorRosterEntry("Tu Gladiador", 100f, 10f, 20f, 1.2f);

        private static List<GladiatorRosterEntry> BuildDefaultRoster()
        {
            return new List<GladiatorRosterEntry>
            {
                BasicPreset(),
                new GladiatorRosterEntry("Novato", 80f, 8f, 14f, 1.4f),
                new GladiatorRosterEntry("Veterano", 130f, 12f, 22f, 1.1f),
                new GladiatorRosterEntry("Agresivo", 90f, 15f, 28f, 0.9f),
                new GladiatorRosterEntry("Defensivo", 150f, 8f, 14f, 1.5f),
                new GladiatorRosterEntry("Berserker", 70f, 18f, 30f, 0.8f),
                new GladiatorRosterEntry("Resistente", 160f, 9f, 16f, 1.3f),
                new GladiatorRosterEntry("Equilibrado", 110f, 11f, 19f, 1.15f),
            };
        }
    }
}
