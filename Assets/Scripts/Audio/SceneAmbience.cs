using UnityEngine;

namespace GladiusAI
{
    /// Define el fondo sonoro de una escena. Se coloca en un objeto de la escena y, al cargarla, le pide
    /// al SoundManager que pase (con fundido) a estas capas en loop.
    /// Como el SoundManager no reinicia un fondo que ya está sonando, pasar de una escena a otra con el
    /// mismo fondo (Menu → Ludus) no lo corta.
    public class SceneAmbience : MonoBehaviour
    {
        [Tooltip("Capas que suenan juntas en loop (por ejemplo lluvia + estadio de lejos). Vacío = silencio.")]
        [SerializeField] private SoundId[] layers = new SoundId[0];
        [SerializeField] private float fadeSeconds = 1.5f;

        private void Start() => SoundManager.PlayAmbience(fadeSeconds, layers);
    }
}
