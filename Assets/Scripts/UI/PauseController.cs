using UnityEngine;
using UnityEngine.SceneManagement;

namespace GladiusAI
{
    // Menu de pausa de los niveles: Continuar / Opciones / Salir. Pausa con Time.timeScale = 0.
    public class PauseController : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject optionsPanel;
        [Tooltip("Escena a la que se vuelve al salir del combate.")]
        [SerializeField] private string exitScene = "Ludus";

        private float previousTimeScale = 1f;

        public void Pause()
        {
            if (pausePanel.activeSelf) return;

            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            pausePanel.SetActive(true);
        }

        public void Resume()
        {
            optionsPanel.SetActive(false);
            pausePanel.SetActive(false);
            Time.timeScale = previousTimeScale;
        }

        public void ShowOptions() => optionsPanel.SetActive(true);
        public void HideOptions() => optionsPanel.SetActive(false);

        // Salir a mitad de combate: se pierde el combate (la energia ya se gasto) y se vuelve a la Ludus.
        public void Exit()
        {
            Time.timeScale = 1f;
            CombatSetup.Current = null;

            if (SceneFader.Instance != null)
                SceneFader.Instance.FadeToScene(exitScene);
            else
                SceneManager.LoadScene(exitScene);
        }

        private void OnDestroy()
        {
            // Si la escena se cambia estando en pausa, el juego no puede quedar congelado.
            Time.timeScale = 1f;
        }
    }
}
