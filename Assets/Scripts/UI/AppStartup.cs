using UnityEngine;
using UnityEngine.Rendering;

namespace GladiusAI
{
    /// Configuración global del juego que se aplica sola al arrancar, antes de cargar la primera escena.
    /// Al ser una clase estática con [RuntimeInitializeOnLoadMethod], no hace falta ponerla en ninguna escena:
    /// Unity la ejecuta automáticamente.
    public static class AppStartup
    {
        // 60 FPS: suficiente para que el movimiento se vea fluido sin gastar batería de más.
        private const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            // En Android/iOS Unity limita el juego a 30 FPS por defecto, y eso se siente "a tirones".
            // Le pedimos 60 explícitamente. (En PC no tiene efecto si el VSync está activado.)
            Application.targetFrameRate = TargetFrameRate;

            // URP trae un panel de debug ("Display Stats") que se abre con un doble toque de tres dedos
            // en el celular, o con Ctrl+Backspace en el editor. Un jugador lo podía abrir sin querer,
            // así que lo desactivamos.
            DebugManager.instance.enableRuntimeUI = false;
        }
    }
}
