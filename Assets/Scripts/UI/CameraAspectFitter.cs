using UnityEngine;

namespace GladiusAI
{
    /// <summary>
    /// Mantiene visible el mismo ANCHO de arena en cualquier pantalla (celular 16:9, 20:9, tablet 4:3...).
    /// La escena se armo para la resolucion de referencia; si la pantalla es mas angosta que eso,
    /// agranda el orthographicSize para no recortar los costados (donde aparecen los gladiadores).
    /// Si es mas ancha, deja el tamaño original y simplemente se ve un poco mas a los costados.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraAspectFitter : MonoBehaviour
    {
        [SerializeField] private Vector2 referenceResolution = new Vector2(2280f, 1080f);

        private Camera cam;
        private float baseOrthoSize;
        private float lastAspect;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            baseOrthoSize = cam.orthographicSize;
        }

        // En Update y no solo en Start: el celular puede rotar o cambiar de tamaño (pantalla dividida).
        private void Update()
        {
            if (Mathf.Approximately(cam.aspect, lastAspect)) return;
            lastAspect = cam.aspect;

            float referenceAspect = referenceResolution.x / referenceResolution.y;
            cam.orthographicSize = cam.aspect < referenceAspect
                ? baseOrthoSize * referenceAspect / cam.aspect
                : baseOrthoSize;
        }
    }
}
