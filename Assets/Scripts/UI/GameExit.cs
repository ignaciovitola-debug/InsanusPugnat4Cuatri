using UnityEngine;

namespace GladiusAI
{
    // Salir del juego segun la plataforma.
    // En Android no se cierra la app: Application.Quit deja la app rota en "recientes" y al volver a abrirla
    // queda la pantalla negra. Se la manda al fondo, igual que el boton de inicio del celular.
    public static class GameExit
    {
        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_ANDROID
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                activity.Call<bool>("moveTaskToBack", true);
            }
#else
            Application.Quit();
#endif
        }
    }
}
