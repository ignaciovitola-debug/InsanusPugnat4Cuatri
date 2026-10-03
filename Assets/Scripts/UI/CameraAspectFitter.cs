using UnityEngine;

namespace GladiusAI
{
    /// Hace que la cámara ortográfica muestre siempre el mismo ANCHO de arena, sin importar la pantalla
    /// (celular 16:9, 20:9, tablet 4:3...).
    ///
    /// Problema que resuelve: una cámara ortográfica mantiene fija la ALTURA visible (orthographicSize),
    /// así que en una pantalla más angosta que la de referencia se recortan los costados, justo donde
    /// aparecen los gladiadores. Si la pantalla es más angosta, agrandamos el orthographicSize lo
    /// necesario para que vuelva a entrar todo el ancho. Si es más ancha, no hace falta tocar nada:
    /// simplemente se ve un poco más de los costados.
    [RequireComponent(typeof(Camera))]
    public class CameraAspectFitter : MonoBehaviour
    {
        [Tooltip("Resolución para la que se armó la escena (la misma que la del Canvas Scaler).")]
        [SerializeField] private Vector2 referenceResolution = new Vector2(2280f, 1080f);

        private Camera cam;
        private float baseOrthoSize;   // el tamaño que tenía la cámara en la escena
        private float referenceAspect; // ancho / alto de la referencia, calculado una sola vez
        private float lastAspect;      // última proporción de pantalla que ajustamos

        private void Awake()
        {
            cam = GetComponent<Camera>();
            baseOrthoSize = cam.orthographicSize;
            referenceAspect = referenceResolution.x / referenceResolution.y;
        }

        // Se revisa en cada frame (y no solo al empezar) porque en el celular la pantalla puede cambiar
        // de tamaño: rotación, pantalla dividida, etc. Si la proporción no cambió, salimos enseguida,
        // así que el costo es una sola comparación por frame.
        private void Update()
        {
            float aspect = cam.aspect;
            if (Mathf.Approximately(aspect, lastAspect)) return;
            lastAspect = aspect;

            // Regla de tres: si la pantalla es X veces más angosta, la altura visible tiene que ser X veces mayor.
            cam.orthographicSize = aspect < referenceAspect
                ? baseOrthoSize * referenceAspect / aspect
                : baseOrthoSize;
        }
    }
}
