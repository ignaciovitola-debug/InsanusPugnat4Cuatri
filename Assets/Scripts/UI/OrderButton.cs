using UnityEngine;
using UnityEngine.UI;

namespace GladiusAI
{
    /// <summary>
    /// Botón de consigna (Atacá / Defiéndete): mientras corre la espera entre órdenes queda
    /// apagado y una franja oscura muestra cuánto falta, así el jugador sabe por qué no responde.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class OrderButton : MonoBehaviour
    {
        [SerializeField] private PlayerIntentController intentController;
        [SerializeField] private Color cooldownOverlayColor = new Color(0f, 0f, 0f, 0.55f);

        private Button button;
        private RectTransform cooldownOverlay;
        private float shownRemaining = -1f;

        private void Awake()
        {
            button = GetComponent<Button>();
            cooldownOverlay = CreateOverlay();
        }

        private void Update()
        {
            float remaining = intentController != null ? intentController.OrderCooldownRatio : 0f;

            // Tocar el RectTransform obliga a reconstruir el Canvas: solo se hace si algo cambio.
            if (Mathf.Approximately(remaining, shownRemaining)) return;
            shownRemaining = remaining;

            button.interactable = remaining <= 0f;
            cooldownOverlay.gameObject.SetActive(remaining > 0f);
            // La franja se achica de derecha a izquierda a medida que pasa la espera.
            cooldownOverlay.anchorMax = new Vector2(remaining, 1f);
        }

        /// <summary>Franja oscura encima del fondo del botón y debajo del texto (primer hijo).</summary>
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
            image.raycastTarget = false;

            go.SetActive(false);
            return rt;
        }
    }
}
