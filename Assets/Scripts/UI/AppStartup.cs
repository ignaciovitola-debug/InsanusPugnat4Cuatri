using UnityEngine;
using UnityEngine.Rendering;

namespace GladiusAI
{
    /// <summary>
    /// Configuracion global que se aplica sola al arrancar el juego (antes de cargar la primera escena).
    /// No hace falta ponerlo en ninguna escena.
    /// </summary>
    public static class AppStartup
    {
        private const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            // En Android/iOS Unity corre por defecto a 30 FPS, lo que se siente "a tirones".
            Application.targetFrameRate = TargetFrameRate;

            // El Rendering Debugger de URP (el panel "Display Stats") se abre con un doble toque
            // de tres dedos en el celular, o Ctrl+Backspace en el editor: el jugador lo abria sin querer.
            DebugManager.instance.enableRuntimeUI = false;
        }
    }
}
