using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GladiusAI
{
    /// <summary>
    /// Fundido a negro entre escenas. Vive en Splash (la primera escena que carga
    /// siempre) y sobrevive el resto de la partida via DontDestroyOnLoad, asi
    /// cualquier escena puede llamar SceneFader.Instance.FadeToScene(nombre) en vez
    /// de SceneManager.LoadScene directo.
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        public static SceneFader Instance { get; private set; }

        [SerializeField] private Image fadeImage;
        [SerializeField] private float fadeDuration = 0.6f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() => StartCoroutine(Fade(1f, 0f));

        public void FadeToScene(string sceneName) => StartCoroutine(FadeOutThenLoad(sceneName));

        private IEnumerator FadeOutThenLoad(string sceneName)
        {
            yield return Fade(0f, 1f);
            SceneManager.LoadScene(sceneName);
            yield return Fade(1f, 0f);
        }

        private IEnumerator Fade(float from, float to)
        {
            if (fadeImage == null) yield break;

            fadeImage.gameObject.SetActive(true);
            SetAlpha(from);

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                SetAlpha(Mathf.Lerp(from, to, t / fadeDuration));
                yield return null;
            }

            SetAlpha(to);
            if (to <= 0f)
                fadeImage.gameObject.SetActive(false);
        }

        private void SetAlpha(float alpha)
        {
            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;
        }
    }
}
