using UnityEngine;
using UnityEngine.UI;

namespace GladiusAI
{
    /// Botón de orden (Atacá / Defiéndete) que muestra la espera entre órdenes.
    /// Después de dar una orden hay unos segundos en los que el gladiador no acepta otra: durante ese
    /// tiempo el botón queda deshabilitado y una franja oscura se va achicando para mostrar cuánto falta.
    /// Así el jugador entiende por qué el botón "no responde".
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

            // Optimización: modificar un RectTransform obliga a Unity a reconstruir el Canvas, que es
            // costoso en mobile. Por eso solo tocamos la UI cuando el valor realmente cambió; cuando no
            // hay espera (casi todo el tiempo), este Update sale acá mismo.
            if (Mathf.Approximately(remaining, shownRemaining)) return;
            shownRemaining = remaining;

            button.interactable = remaining <= 0f;
            cooldownOverlay.gameObject.SetActive(remaining > 0f);
            // La franja ocupa del borde izquierdo hasta "remaining": se achica de derecha a izquierda.
            cooldownOverlay.anchorMax = new Vector2(remaining, 1f);
        }

        /// Crea la franja oscura por código, así no hay que armarla a mano en cada escena.
        /// Va como primer hijo del botón: queda encima del fondo pero debajo del texto.
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
