using UnityEngine;
using UnityEngine.SceneManagement;

namespace GladiusAI
{
    /// Controlador del menú principal: Jugar / Opciones / Créditos / Salir.
    public class MenuController : MonoBehaviour
    {
        [SerializeField] private string levelSceneName = "ArenaDeCombate1";
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject creditsPanel;

        /// Usado por la herramienta de Editor al armar la escena — asigna las referencias una sola vez.
        public void Configure(GameObject options, GameObject credits, string levelScene)
        {
            optionsPanel = options;
            creditsPanel = credits;
            levelSceneName = levelScene;
        }

        public void PlayLevel()
        {
            if (SceneFader.Instance != null)
            {
                SceneFader.Instance.FadeToScene(levelSceneName);
            }
            else
            {
                Debug.LogWarning("No hay SceneFader activo (¿arrancaste el juego desde Splash?) -- cargando sin fundido.");
                SceneManager.LoadScene(levelSceneName);
            }
        }

        public void ShowOptions() => optionsPanel.SetActive(true);
        public void HideOptions() => optionsPanel.SetActive(false);

        public void ShowCredits() => creditsPanel.SetActive(true);
        public void HideCredits() => creditsPanel.SetActive(false);

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
