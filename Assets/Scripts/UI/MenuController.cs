using UnityEngine;
using UnityEngine.SceneManagement;

namespace GladiusAI
{
    /// <summary>Controlador del menú principal: Jugar / Opciones / Créditos / Salir.</summary>
    public class MenuController : MonoBehaviour
    {
        [SerializeField] private string levelSceneName = "ArenaDeCombate1";
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject creditsPanel;

        /// <summary>Usado por la herramienta de Editor al armar la escena — asigna las referencias una sola vez.</summary>
        public void Configure(GameObject options, GameObject credits, string levelScene)
        {
            optionsPanel = options;
            creditsPanel = credits;
            levelSceneName = levelScene;
        }

        public void PlayLevel() => SceneManager.LoadScene(levelSceneName);

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
