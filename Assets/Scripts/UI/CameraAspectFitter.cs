using UnityEngine;

namespace GladiusAI
{
    // Hace que la cámara ortográfica muestre siempre el mismo ancho de arena en cualquier pantalla.
    // Si la pantalla es más angosta, agranda el orthographicSize para que entre todo el ancho.
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

        // Se revisa cada frame porque la pantalla puede cambiar (rotación, pantalla dividida).
        // Si la proporción no cambió, sale enseguida.
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
