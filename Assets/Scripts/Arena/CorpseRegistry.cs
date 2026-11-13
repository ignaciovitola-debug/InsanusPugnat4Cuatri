using System.Collections.Generic;
using UnityEngine;

namespace GladiusAI
{
    // Los cuerpos que quedan en cada arena entre un combate y otro (y entre partidas: se guarda en el archivo).
    // Cada arena cuenta sus combates: los cuerpos se acumulan durante 3 combates y al arrancar el siguiente
    // la arena se limpia (los esclavos se los llevan). Estática para sobrevivir al cambio de escena.
    // Originador del Memento, como GladiatorRoster.
    public static class CorpseRegistry
    {
        // Cada cuántos combates se limpia la arena.
        public const int CombatsBeforeCleanup = 3;

        // Tope por arena: al pasarlo se va el cuerpo más viejo, para que la arena no se llene.
        public const int MaxPerScene = 20;

        // Distancia mínima entre dos cuerpos para que no se encimen.
        public const float MinSpacing = 1.3f;

        private static List<CorpseRecord> corpses = new List<CorpseRecord>();
        private static List<ArenaCombatCount> combats = new List<ArenaCombatCount>();

        public static IEnumerable<CorpseRecord> InScene(string scene)
        {
            foreach (var c in corpses)
                if (c.scene == scene) yield return c;
        }

        // Arranca un combate en esta arena. Si ya se jugaron 3 desde la última limpieza, se sacan los cuerpos.
        public static void RegisterCombat(string scene)
        {
            int index = combats.FindIndex(c => c.scene == scene);
            var entry = index >= 0 ? combats[index] : new ArenaCombatCount { scene = scene, count = 0 };

            if (entry.count >= CombatsBeforeCleanup)
            {
                corpses.RemoveAll(c => c.scene == scene);
                entry.count = 0;
            }

            entry.count++;
            if (index >= 0) combats[index] = entry; else combats.Add(entry);
            EventManager.Raise(new SaveRequestedEvent());
        }

        // Agrega un cuerpo. Devuelve el que se borró por pasar el tope (o null si no se borró ninguno).
        public static CorpseRecord? Add(CorpseRecord record)
        {
            corpses.Add(record);

            CorpseRecord? removed = null;
            int count = 0;
            foreach (var c in corpses)
                if (c.scene == record.scene) count++;

            if (count > MaxPerScene)
            {
                int oldest = corpses.FindIndex(c => c.scene == record.scene);
                removed = corpses[oldest];
                corpses.RemoveAt(oldest);
            }

            EventManager.Raise(new SaveRequestedEvent());
            return removed;
        }

        // Busca un lugar libre lo más cerca posible de donde cayó: si ya hay un cuerpo ahí, prueba
        // alrededor en círculos cada vez más grandes. Siempre dentro de la arena.
        public static Vector3 FindFreeSpot(string scene, Vector3 desired)
        {
            const int Directions = 8;
            const int Rings = 5;

            if (IsFree(scene, desired)) return desired;

            for (int ring = 1; ring <= Rings; ring++)
            {
                float radius = MinSpacing * ring;
                float startAngle = Random.Range(0f, 360f);   // para que no siempre se corran para el mismo lado
                for (int i = 0; i < Directions; i++)
                {
                    float angle = (startAngle + i * 360f / Directions) * Mathf.Deg2Rad;
                    var candidate = desired + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    if (ArenaBounds.Instance != null)
                        candidate = ArenaBounds.Instance.ClampPosition(candidate, 1f);
                    candidate.y = desired.y;
                    if (IsFree(scene, candidate)) return candidate;
                }
            }
            return desired;   // arena llena: se acepta que se encime
        }

        private static bool IsFree(string scene, Vector3 position)
        {
            float minSqr = MinSpacing * MinSpacing;
            foreach (var c in corpses)
            {
                if (c.scene != scene) continue;
                float dx = c.x - position.x, dz = c.z - position.z;
                if (dx * dx + dz * dz < minSqr) return false;
            }
            return true;
        }

        public static CorpseMemento CreateMemento()
            => new CorpseMemento { corpses = new List<CorpseRecord>(corpses), combats = new List<ArenaCombatCount>(combats) };

        public static void RestoreMemento(CorpseMemento memento)
        {
            corpses = memento.corpses != null ? new List<CorpseRecord>(memento.corpses) : new List<CorpseRecord>();
            combats = memento.combats != null ? new List<ArenaCombatCount>(memento.combats) : new List<ArenaCombatCount>();
        }

        public static void ResetToNewGame()
        {
            corpses = new List<CorpseRecord>();
            combats = new List<ArenaCombatCount>();
        }
    }
}
