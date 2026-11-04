using UnityEngine;
using UnityEngine.UI;

namespace GladiusAI
{
    // Capa negra a pantalla completa cuyo alfa baja el brillo. Se crea sola al arrancar cada partida
    // (sobrevive a los cambios de escena), asi no hay que ponerla en ninguna escena.
    public static class BrightnessOverlay
    {
        private const float MaxDarkness = 0.75f;

        private static Image overlay;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var go = new GameObject("BrightnessOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(Image));
            Object.DontDestroyOnLoad(go);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000;

            overlay = go.GetComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0f);
            overlay.raycastTarget = false; // que no robe los toques de la interfaz

            Apply();
        }

        public static void Apply()
        {
            if (overlay == null) return;

            float darkness = (1f - GameSettings.Brightness) / (1f - GameSettings.MinBrightness) * MaxDarkness;
            overlay.color = new Color(0f, 0f, 0f, darkness);
            overlay.enabled = darkness > 0.001f;
        }
    }
}
