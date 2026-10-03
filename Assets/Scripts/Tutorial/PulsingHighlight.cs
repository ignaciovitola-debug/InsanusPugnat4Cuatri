using UnityEngine;
using UnityEngine.UI;

namespace GladiusAI
{
    /// Resalta un elemento de UI (un botón) con un borde dorado y un leve "latido" de tamaño.
    /// Solo cambia cómo se ve: no afecta si el botón se puede tocar o no.
    /// Uso: PulsingHighlight.Add(boton, color) para prender, PulsingHighlight.Remove(boton) para apagar.
    [DisallowMultipleComponent]
    public class PulsingHighlight : MonoBehaviour
    {
        private const float PulseAmount = 0.06f; // crece y achica un 6%
        private const float PulseSpeed = 6f;     // velocidad del latido (radianes por segundo)

        private Outline outline;

        public static void Add(RectTransform target, Color color)
        {
            var highlight = target.GetComponent<PulsingHighlight>();
            if (highlight == null)
                highlight = target.gameObject.AddComponent<PulsingHighlight>();

            // El borde lo dibuja el componente Outline de Unity UI, sobre el Image del botón.
            if (highlight.outline == null)
                highlight.outline = target.gameObject.AddComponent<Outline>();
            highlight.outline.effectColor = color;
            highlight.outline.effectDistance = new Vector2(6f, -6f);
        }

        public static void Remove(RectTransform target)
        {
            var highlight = target.GetComponent<PulsingHighlight>();
            if (highlight == null) return;

            // DestroyImmediate en vez de Destroy: Destroy recién borra al final del frame. Si en ese mismo
            // frame se vuelve a resaltar el botón (termina el diálogo y empieza la espera del combate),
            // Add encontraría este componente "a punto de morir" y lo reutilizaría, y el resaltado
            // desaparecería un instante después.
            if (highlight.outline != null)
                DestroyImmediate(highlight.outline);
            target.localScale = Vector3.one;
            DestroyImmediate(highlight);
        }

        private void Update()
        {
            // Time.unscaledTime: late aunque el juego esté en pausa (por ejemplo, con el diálogo abierto).
            float scale = 1f + PulseAmount * Mathf.Sin(Time.unscaledTime * PulseSpeed);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
