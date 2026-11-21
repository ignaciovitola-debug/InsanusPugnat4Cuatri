using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GladiusAI
{
    // Lista de renderers donde está el contorno (se asigna una vez desde el Editor).
    public class RendererRefs : ScriptableObject
    {
        public ScriptableRendererData[] renderers = new ScriptableRendererData[0];
    }
}
