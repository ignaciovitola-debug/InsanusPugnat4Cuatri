using UnityEngine;
using UnityEngine.UI;

namespace GladiusAI
{
    // Botón de orden que muestra la espera entre órdenes.
    // Mientras espera queda deshabilitado y una franja oscura se achica para mostrar cuánto falta.
    [RequireComponent(typeof(Button))]
    public class OrderButton : MonoBehaviour
    {
        [Tooltip("De acá se lee cuánto falta para poder dar otra orden.")]
        [SerializeField] private PlayerIntentController intentController;
        [SerializeField] private Color cooldownOverlayColor = new Color(0f, 0f, 0f, 0.55f);

        private Button button;
        private RectTransform cooldownOverlay;
        private float shownRemaining = -1f; // último valor dibujado (-1 = todavía no se dibujó nada)

        private void Awake()
        {
            button = GetComponent<Button>();
            cooldownOverlay = CreateOverlay();
        }

        private void Update()
        {
            // remaining va de 1 (recién se dio la orden) a 0 (ya se puede dar otra).
            float remaining = intentController != null ? intentController.OrderCooldownRatio : 0f;

            // Solo se toca la UI cuando el valor cambió (modificar un RectTransform es costoso en mobile).
            if (Mathf.Approximately(remaining, shownRemaining)) return;
            shownRemaining = remaining;

            button.interactable = remaining <= 0f;
            cooldownOverlay.gameObject.SetActive(remaining > 0f);
            // La franja ocupa del borde izquierdo hasta "remaining": se achica de derecha a izquierda.
            cooldownOverlay.anchorMax = new Vector2(remaining, 1f);
        }

        // Crea la franja oscura por código, así no hay que armarla a mano en cada escena.
        // Va como primer hijo del botón: queda encima del fondo pero debajo del texto.
        private RectTransform CreateOverlay()
        {
            var go = new GameObject("CooldownOverlay", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.SetAsFirstSibling();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = cooldownOverlayColor;
            image.raycastTarget = false; // que no "robe" los toques del botón

            go.SetActive(false);
            return rt;
        }
    }
}
