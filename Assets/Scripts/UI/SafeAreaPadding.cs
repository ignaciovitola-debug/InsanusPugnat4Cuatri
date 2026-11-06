using UnityEngine;

namespace GladiusAI
{
    // Corre un elemento de UI pegado a un borde de la pantalla para que no lo tape el notch, la cámara
    // o las esquinas redondeadas del celular. Unity informa la zona libre en Screen.safeArea; acá se
    // mide cuánto le falta a cada borde y se suma a la posición original del elemento.
    // Solo afecta a los bordes donde el elemento está anclado (por ejemplo, arriba a la izquierda).
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaPadding : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Canvas rootCanvas;
        private Vector2 basePosition;   // posición puesta en la escena, sin el margen del notch
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
            basePosition = rectTransform.anchoredPosition;
        }

        private void OnEnable() => Apply();

        // La zona segura cambia si el celular rota; se revisa cada frame pero solo se recalcula si cambió.
        private void Update()
        {
            if (Screen.safeArea != lastSafeArea || Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
                Apply();
        }

        private void Apply()
        {
            if (rectTransform == null || rootCanvas == null) return;

            Rect safe = Screen.safeArea;
            lastSafeArea = safe;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            // Píxeles de pantalla tapados en cada borde, pasados a unidades del Canvas.
            // Se limitan a valores razonables: algunos simuladores informan una zona segura que no
            // coincide con el tamaño de la pantalla, y eso daría márgenes negativos o enormes.
            float scale = rootCanvas.scaleFactor;
            float left = ClampInset(safe.xMin, Screen.width) / scale;
            float right = ClampInset(Screen.width - safe.xMax, Screen.width) / scale;
            float bottom = ClampInset(safe.yMin, Screen.height) / scale;
            float top = ClampInset(Screen.height - safe.yMax, Screen.height) / scale;

            Vector2 offset = Vector2.zero;
            if (rectTransform.anchorMin.x == 0f && rectTransform.anchorMax.x == 0f) offset.x += left;
            if (rectTransform.anchorMin.x == 1f && rectTransform.anchorMax.x == 1f) offset.x -= right;
            if (rectTransform.anchorMin.y == 0f && rectTransform.anchorMax.y == 0f) offset.y += bottom;
            if (rectTransform.anchorMin.y == 1f && rectTransform.anchorMax.y == 1f) offset.y -= top;

            rectTransform.anchoredPosition = basePosition + offset;
        }

        // Un notch nunca ocupa menos de 0 ni más de un cuarto de la pantalla.
        private static float ClampInset(float pixels, float screenSize) => Mathf.Clamp(pixels, 0f, screenSize * 0.25f);
    }
}
