using UnityEngine;

namespace GladiusAI
{
    /// <summary>
    /// Fondo sonoro de la escena: al cargarla, el SoundManager pasa (con fundido) a estas capas en loop.
    /// Si la escena siguiente tiene el mismo fondo (Menu -> Ludus), sigue sonando sin cortarse.
    /// </summary>
    public class SceneAmbience : MonoBehaviour
    {
        [Tooltip("Capas que suenan juntas en loop. Vacío = silencio.")]
        [SerializeField] private SoundId[] layers = new SoundId[0];
        [SerializeField] private float fadeSeconds = 1.5f;

        private void Start() => SoundManager.PlayAmbience(fadeSeconds, layers);
    }
}
