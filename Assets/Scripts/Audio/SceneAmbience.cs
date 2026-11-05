using UnityEngine;

namespace GladiusAI
{
    // Define el fondo sonoro de la escena: al cargarla le pide al SoundManager esas capas en loop.
    // Si el fondo ya está sonando (Menu a Ludus), no se corta.
    public class SceneAmbience : MonoBehaviour
    {
        [Tooltip("Capas que suenan juntas en loop. Vacío = silencio.")]
        [SerializeField] private SoundId[] layers = new SoundId[0];
        [SerializeField] private float fadeSeconds = 1.5f;

        private void Start() => SoundManager.PlayAmbience(fadeSeconds, layers);
    }
}
