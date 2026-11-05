using UnityEngine;
using UnityEngine.Rendering;

namespace GladiusAI
{
    // Configuración global que se aplica sola al arrancar, antes de cargar la primera escena.
    // No hace falta ponerla en ninguna escena ([RuntimeInitializeOnLoadMethod]).
    public static class AppStartup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            // Volumen general, calidad y cuadros por segundo (60 por defecto, 30 si el jugador lo eligio para
            // ahorrar bateria) salen de los ajustes guardados.
            GameSettings.ApplyAll();

            // Se desactiva el panel de debug de URP (doble toque de tres dedos): se abría sin querer.
            DebugManager.instance.enableRuntimeUI = false;
        }
    }
}
