using System;
using System.Collections.Generic;

namespace GladiusAI
{
    // Un cuerpo que quedó tirado en una arena: en qué escena, dónde, con qué aspecto y hacia dónde mira.
    [Serializable]
    public struct CorpseRecord
    {
        public string scene;
        public float x;
        public float y;
        public float z;
        public int skin;
        public bool flipX;
        public float puddleRotation;   // giro del charco de sangre de abajo (así no son todos iguales)
    }

    // Cuántos combates se jugaron en una arena (para saber cuándo limpiarla).
    [Serializable]
    public struct ArenaCombatCount
    {
        public string scene;
        public int count;
    }

    // Foto de los cuerpos de todas las arenas (la crea y la usa CorpseRegistry).
    [Serializable]
    public struct CorpseMemento
    {
        public List<CorpseRecord> corpses;
        public List<ArenaCombatCount> combats;
    }
}
