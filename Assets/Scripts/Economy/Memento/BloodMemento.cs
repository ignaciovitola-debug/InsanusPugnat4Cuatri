using System;
using System.Collections.Generic;

namespace GladiusAI
{
    // Una mancha de sangre en el piso de una arena: en qué escena, dónde, qué dibujo, girada y de qué tamaño.
    [Serializable]
    public struct BloodStain
    {
        public string scene;
        public float x;
        public float y;
        public float z;
        public int sprite;
        public float rotation;
        public float scale;
    }

    // Foto de las manchas de todas las arenas (la crea y la usa BloodRegistry).
    [Serializable]
    public struct BloodMemento
    {
        public List<BloodStain> stains;
    }
}
