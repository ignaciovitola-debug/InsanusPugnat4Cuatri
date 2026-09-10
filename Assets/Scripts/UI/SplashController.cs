using UnityEngine;
using UnityEngine.SceneManagement;

namespace GladiusAI
{
    /// <summary>Splash simple: muestra el título/nombres del equipo unos segundos y pasa solo a la siguiente escena.</summary>
    public class SplashController : MonoBehaviour
    {
        [SerializeField] private string nextSceneName = "Menu";
        [SerializeField] private float displaySeconds = 2.5f;

        private float timer;

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= displaySeconds)
                SceneManager.LoadScene(nextSceneName);
        }
    }
}
