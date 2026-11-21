using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GladiusAI
{
    // Prende y apaga el contorno negro (la feature "Line" de los renderers) sin tener que rearmar el juego.
    public static class OutlineSwitch
    {
        private const string FeatureName = "Line";
        private const string ResourcePath = "RendererRefs";

        public static void Apply(bool enabled)
        {
            var refs = Resources.Load<RendererRefs>(ResourcePath);
            if (refs == null) return;

            foreach (var renderer in refs.renderers)
            {
                if (renderer == null) continue;

                foreach (var feature in renderer.rendererFeatures)
                {
                    if (feature != null && feature.name == FeatureName)
                        feature.SetActive(enabled);
                }
            }
        }

#if UNITY_EDITOR
        // En el Editor los renderers son assets: se deja todo prendido al salir del Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RestoreOnQuit()
        {
            Application.quitting -= RestoreAll;
            Application.quitting += RestoreAll;
        }

        private static void RestoreAll() => Apply(true);
#endif
    }
}
