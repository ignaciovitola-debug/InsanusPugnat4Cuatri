using UnityEngine;
using UnityEngine.Rendering;

namespace GladiusAI
{
    /// Configuración global del juego que se aplica sola al arrancar, antes de cargar la primera escena.
    /// Al ser una clase estática con [RuntimeInitializeOnLoadMethod], no hace falta ponerla en ninguna escena:
    /// Unity la ejecuta automáticamente.
    public static class AppStartup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            // Volumen general, calidad y cuadros por segundo (60 por defecto, 30 si el jugador lo eligio para
            // ahorrar bateria) salen de los ajustes guardados.
            GameSettings.ApplyAll();

            // URP trae un panel de debug ("Display Stats") que se abre con un doble toque de tres dedos
            // en el celular, o con Ctrl+Backspace en el editor. Un jugador lo podía abrir sin querer,
            // así que lo desactivamos.
            DebugManager.instance.enableRuntimeUI = false;
        }
    }
}
