using UnityEngine;
using UnityEngine.UI;

namespace GladiusAI
{
    // Resalta un botón con borde dorado y un leve latido de tamaño.
    // Uso: PulsingHighlight.Add(boton, color) para prender y Remove(boton) para apagar.
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

            // DestroyImmediate porque Destroy borra recién al final del frame.
            // Si no, Add reutilizaría este componente y el resaltado desaparecería.
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
