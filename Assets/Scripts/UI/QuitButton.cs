using UnityEngine;

namespace GladiusAI
{
    /// <summary>Botón para salir del juego (útil en demos/pruebas, ej. para que el profesor cierre al terminar de evaluar).</summary>
    public class QuitButton : MonoBehaviour
    {
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
