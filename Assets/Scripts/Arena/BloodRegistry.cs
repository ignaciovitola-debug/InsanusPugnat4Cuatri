using System.Collections.Generic;

namespace GladiusAI
{
    // Las manchas de sangre de cada arena. Se acumulan combate tras combate (y entre partidas: van en el
    // archivo de guardado), así cada arena se ve más sangrienta cuanto más se pelea en ella.
    // Estática para sobrevivir al cambio de escena. Originador del Memento, como GladiatorRoster.
    public static class BloodRegistry
    {
        // Tope por arena: al pasarlo se borra la mancha más vieja. Así el piso se ve lleno de sangre
        // pero la cantidad de dibujos no crece para siempre (en el celular cada uno cuesta).
        public const int MaxPerScene = 200;

        private static List<BloodStain> stains = new List<BloodStain>();

        public static IEnumerable<BloodStain> InScene(string scene)
        {
            foreach (var s in stains)
                if (s.scene == scene) yield return s;
        }

        // Agrega una mancha. Devuelve la que se borró por pasar el tope (o null si no se borró ninguna).
        // No guarda el archivo en cada golpe: se guarda al terminar el combate (ver CombatStarter).
        public static BloodStain? Add(BloodStain stain)
        {
            stains.Add(stain);

            int count = 0;
            foreach (var s in stains)
                if (s.scene == stain.scene) count++;
            if (count <= MaxPerScene) return null;

            int oldest = stains.FindIndex(s => s.scene == stain.scene);
            var removed = stains[oldest];
            stains.RemoveAt(oldest);
            return removed;
        }

        public static BloodMemento CreateMemento()
            => new BloodMemento { stains = new List<BloodStain>(stains) };

        public static void RestoreMemento(BloodMemento memento)
            => stains = memento.stains != null ? new List<BloodStain>(memento.stains) : new List<BloodStain>();

        public static void ResetToNewGame() => stains = new List<BloodStain>();
    }
}
